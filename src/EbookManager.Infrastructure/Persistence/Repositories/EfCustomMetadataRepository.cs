using System.Globalization;
using System.Text;
using System.Text.Json;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed class EfCustomMetadataRepository(
    LibraryDbContextFactory contextFactory,
    string libraryPath) : ICustomMetadataRepository
{
    private const int SqliteParameterChunkSize = 500;

    public async Task<IReadOnlyList<CustomMetadataFieldDefinition>> ListDefinitionsAsync(
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var fields = await context.CustomMetadataFields
            .AsNoTracking()
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Name)
            .ThenBy(field => field.Id)
            .ToListAsync(cancellationToken);
        return fields.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task<CustomMetadataFieldDefinition> AddDefinitionAsync(
        string name,
        CustomMetadataFieldType type,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported custom metadata field type.");
        }

        var trimmedName = NormalizeDisplayName(name);
        var now = DateTimeOffset.UtcNow;
        await using var context = contextFactory.Create(libraryPath);
        var normalizedName = NormalizeName(trimmedName);
        if (await context.CustomMetadataFields.AnyAsync(field => field.NormalizedName == normalizedName, cancellationToken))
        {
            throw new InvalidOperationException($"A custom metadata field named '{trimmedName}' already exists.");
        }

        var sortOrder = await context.CustomMetadataFields
            .Select(field => (int?)field.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;
        var entity = new CustomMetadataFieldEntity
        {
            Id = Guid.NewGuid(),
            Key = await CreateUniqueKeyAsync(context, trimmedName, cancellationToken),
            Name = trimmedName,
            NormalizedName = normalizedName,
            Type = type,
            SortOrder = sortOrder + 1,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        context.CustomMetadataFields.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return ToDomain(entity);
    }

    public async Task RenameDefinitionAsync(
        Guid fieldId,
        string name,
        CancellationToken cancellationToken)
    {
        var trimmedName = NormalizeDisplayName(name);
        var normalizedName = NormalizeName(trimmedName);
        await using var context = contextFactory.Create(libraryPath);
        var duplicateExists = await context.CustomMetadataFields
            .AnyAsync(field => field.Id != fieldId && field.NormalizedName == normalizedName, cancellationToken);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"A custom metadata field named '{trimmedName}' already exists.");
        }

        var entity = await context.CustomMetadataFields
            .SingleOrDefaultAsync(field => field.Id == fieldId, cancellationToken)
            ?? throw new KeyNotFoundException($"Custom metadata field '{fieldId}' does not exist.");
        entity.Name = trimmedName;
        entity.NormalizedName = normalizedName;
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDefinitionOptionsAsync(
        Guid fieldId,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var entity = await context.CustomMetadataFields
            .SingleOrDefaultAsync(field => field.Id == fieldId, cancellationToken)
            ?? throw new KeyNotFoundException($"Custom metadata field '{fieldId}' does not exist.");
        if (entity.Type is not (CustomMetadataFieldType.SingleSelect or CustomMetadataFieldType.MultiSelect))
        {
            throw new InvalidOperationException("Options can only be configured for single-select and multi-select metadata fields.");
        }

        var normalizedOptions = NormalizeOptions(options);
        entity.OptionsJson = normalizedOptions.Count == 0
            ? null
            : JsonSerializer.Serialize(normalizedOptions);
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDefinitionAsync(Guid fieldId, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var entity = await context.CustomMetadataFields
            .SingleOrDefaultAsync(field => field.Id == fieldId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        context.CustomMetadataFields.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomMetadataValue>> GetValuesAsync(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var values = await context.CustomMetadataValues
            .AsNoTracking()
            .Where(value => value.BookId == bookId)
            .OrderBy(value => value.Field.SortOrder)
            .ThenBy(value => value.Field.Name)
            .ToListAsync(cancellationToken);
        return values.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<CustomMetadataValue>> GetValuesForBooksAsync(
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken)
    {
        if (bookIds.Count == 0)
        {
            return [];
        }

        await using var context = contextFactory.Create(libraryPath);
        var values = new List<CustomMetadataValueEntity>();
        foreach (var chunk in bookIds.Distinct().Chunk(SqliteParameterChunkSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkValues = await context.CustomMetadataValues
                .AsNoTracking()
                .Where(value => chunk.Contains(value.BookId))
                .OrderBy(value => value.BookId)
                .ThenBy(value => value.Field.SortOrder)
                .ThenBy(value => value.Field.Name)
                .ToListAsync(cancellationToken);
            values.AddRange(chunkValues);
        }

        return values.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task SetValueAsync(
        CustomMetadataValue value,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var field = await context.CustomMetadataFields
            .AsNoTracking()
            .SingleOrDefaultAsync(field => field.Id == value.FieldId, cancellationToken)
            ?? throw new KeyNotFoundException($"Custom metadata field '{value.FieldId}' does not exist.");
        ValidateValue(field.Type, value);

        var entity = await context.CustomMetadataValues
            .SingleOrDefaultAsync(
                existing => existing.BookId == value.BookId && existing.FieldId == value.FieldId,
                cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (entity is null)
        {
            entity = new CustomMetadataValueEntity
            {
                BookId = value.BookId,
                FieldId = value.FieldId
            };
            context.CustomMetadataValues.Add(entity);
        }

        entity.TextValue = NormalizeBlank(value.TextValue);
        entity.NumberValue = value.NumberValue;
        entity.DateValue = value.DateValue;
        entity.BooleanValue = value.BooleanValue;
        entity.UpdatedUtc = value.UpdatedUtc ?? now;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteValueAsync(
        Guid bookId,
        Guid fieldId,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var entity = await context.CustomMetadataValues
            .SingleOrDefaultAsync(
                value => value.BookId == bookId && value.FieldId == fieldId,
                cancellationToken);
        if (entity is null)
        {
            return;
        }

        context.CustomMetadataValues.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static CustomMetadataFieldDefinition ToDomain(CustomMetadataFieldEntity entity) =>
        new(
            entity.Id,
            entity.Key,
            entity.Name,
            entity.Type,
            DeserializeOptions(entity.OptionsJson),
            entity.SortOrder,
            entity.CreatedUtc,
            entity.UpdatedUtc);

    private static CustomMetadataValue ToDomain(CustomMetadataValueEntity entity) =>
        new(
            entity.BookId,
            entity.FieldId,
            entity.TextValue,
            entity.NumberValue,
            entity.DateValue,
            entity.BooleanValue,
            entity.UpdatedUtc);

    private static void ValidateValue(
        CustomMetadataFieldType type,
        CustomMetadataValue value)
    {
        var populatedCount =
            Count(value.TextValue) +
            Count(value.NumberValue) +
            Count(value.DateValue) +
            Count(value.BooleanValue);
        if (populatedCount > 1)
        {
            throw new InvalidOperationException("A custom metadata value can only store one typed value.");
        }

        var isValid = type switch
        {
            CustomMetadataFieldType.Text or CustomMetadataFieldType.SingleSelect or CustomMetadataFieldType.MultiSelect =>
                value.NumberValue is null && value.DateValue is null && value.BooleanValue is null,
            CustomMetadataFieldType.Number =>
                value.TextValue is null && value.DateValue is null && value.BooleanValue is null,
            CustomMetadataFieldType.Date =>
                value.TextValue is null && value.NumberValue is null && value.BooleanValue is null,
            CustomMetadataFieldType.Boolean =>
                value.TextValue is null && value.NumberValue is null && value.DateValue is null,
            _ => false
        };
        if (!isValid)
        {
            throw new InvalidOperationException($"Custom metadata value does not match field type '{type}'.");
        }

        static int Count<T>(T? value) => value is null ? 0 : 1;
    }

    private static string NormalizeDisplayName(string name)
    {
        var trimmed = name.Trim();
        return trimmed.Length == 0
            ? throw new ArgumentException("Custom metadata field name cannot be empty.", nameof(name))
            : trimmed;
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();

    private static string? NormalizeBlank(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static IReadOnlyList<string> NormalizeOptions(IEnumerable<string> options)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options.Select(NormalizeBlank).OfType<string>())
        {
            if (seen.Add(option))
            {
                result.Add(option);
            }
        }

        return result.AsReadOnly();
    }

    private static IReadOnlyList<string> DeserializeOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(optionsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static async Task<string> CreateUniqueKeyAsync(
        LibraryDbContext context,
        string name,
        CancellationToken cancellationToken)
    {
        var baseKey = CreateKey(name);
        var key = baseKey;
        var suffix = 2;
        while (await context.CustomMetadataFields.AnyAsync(field => field.Key == key, cancellationToken))
        {
            key = $"{baseKey}-{suffix}";
            suffix++;
        }

        return key;
    }

    private static string CreateKey(string name)
    {
        var builder = new StringBuilder();
        foreach (var character in name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var key = builder.ToString().Trim('-');
        return key.Length == 0 ? "field" : key;
    }
}
