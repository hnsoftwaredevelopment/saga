using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Books;

public sealed class DuplicateMergeService(IBookRepository bookRepository)
{
    public async Task<DuplicateMergeResult> MergeFormatsOnlyAsync(
        Guid sourceBookId,
        Guid targetBookId,
        CancellationToken cancellationToken)
    {
        await bookRepository.AttachFilesToBookAsync(sourceBookId, targetBookId, cancellationToken);
        return new DuplicateMergeResult(
            sourceBookId,
            targetBookId,
            DuplicateMergeMetadataPolicy.PreserveTarget);
    }

    public async Task<DuplicateMergeResult> MergeAsync(
        Guid sourceBookId,
        Guid targetBookId,
        IReadOnlyList<DuplicateMergeFieldSelection> selections,
        CancellationToken cancellationToken)
    {
        if (sourceBookId == targetBookId)
        {
            return new DuplicateMergeResult(
                sourceBookId,
                targetBookId,
                DuplicateMergeMetadataPolicy.MergeSelectedFields);
        }

        var source = await bookRepository.GetAsync(sourceBookId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source book '{sourceBookId}' does not exist.");
        var target = await bookRepository.GetAsync(targetBookId, cancellationToken)
            ?? throw new KeyNotFoundException($"Target book '{targetBookId}' does not exist.");
        var mergedTarget = ApplySelectedMetadata(source, target, selections);

        await bookRepository.AttachFilesToBookAsync(sourceBookId, targetBookId, cancellationToken);
        await bookRepository.UpdateAsync(mergedTarget, cancellationToken);
        return new DuplicateMergeResult(
            sourceBookId,
            targetBookId,
            DuplicateMergeMetadataPolicy.MergeSelectedFields);
    }

    private static Book ApplySelectedMetadata(
        Book source,
        Book target,
        IReadOnlyList<DuplicateMergeFieldSelection> selections)
    {
        var metadata = target.Metadata;
        var coverRelativePath = target.CoverRelativePath;
        foreach (var selection in selections)
        {
            if (selection.Action == DuplicateMergeAction.NoAction ||
                selection.Field == DuplicateMergeMetadataField.Formats)
            {
                continue;
            }

            metadata = selection.Field switch
            {
                DuplicateMergeMetadataField.Cover => CopyCover(source, metadata, ref coverRelativePath),
                DuplicateMergeMetadataField.Title => CopyScalar(selection, metadata, source.Metadata.Title, value => CopyMetadata(metadata, title: value)),
                DuplicateMergeMetadataField.Authors => MergeList(selection, metadata, metadata.Authors, source.Metadata.Authors, values => CopyMetadata(metadata, authors: values)),
                DuplicateMergeMetadataField.Description => MergeText(selection, metadata, metadata.Description, source.Metadata.Description, value => CopyMetadata(metadata, description: value)),
                DuplicateMergeMetadataField.Tags => MergeNullableList(selection, metadata, metadata.Tags, source.Metadata.Tags, values => CopyMetadata(metadata, tags: values)),
                DuplicateMergeMetadataField.Publisher => CopyScalar(selection, metadata, source.Metadata.Publisher, value => CopyMetadata(metadata, publisher: value)),
                DuplicateMergeMetadataField.Language => CopyScalar(selection, metadata, source.Metadata.Language, value => CopyMetadata(metadata, language: value)),
                DuplicateMergeMetadataField.Series => CopyScalar(selection, metadata, source.Metadata.Series, value => CopyMetadata(metadata, series: value)),
                DuplicateMergeMetadataField.SeriesNumber => CopyValue(selection, metadata, source.Metadata.SeriesNumber, value => CopyMetadata(metadata, seriesNumber: value)),
                DuplicateMergeMetadataField.PublicationDate => CopyValue(selection, metadata, source.Metadata.PublicationDate, value => CopyMetadata(metadata, publicationDate: value)),
                DuplicateMergeMetadataField.Isbn => CopyScalar(selection, metadata, source.Metadata.Isbn, value => CopyMetadata(metadata, isbn: value)),
                _ => metadata
            };
        }

        return target with
        {
            Metadata = metadata,
            CoverRelativePath = coverRelativePath,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private static BookMetadata CopyCover(
        Book source,
        BookMetadata targetMetadata,
        ref string? coverRelativePath)
    {
        if (source.CoverRelativePath is null && source.Metadata.CoverBytes is null)
        {
            return targetMetadata;
        }

        coverRelativePath = source.CoverRelativePath;
        return CopyMetadata(targetMetadata, coverBytes: source.Metadata.CoverBytes);
    }

    private static BookMetadata CopyScalar(
        DuplicateMergeFieldSelection selection,
        BookMetadata metadata,
        string? sourceValue,
        Func<string?, BookMetadata> copy)
    {
        if (selection.Action != DuplicateMergeAction.Copy || string.IsNullOrWhiteSpace(sourceValue))
        {
            return metadata;
        }

        return copy(sourceValue.Trim());
    }

    private static BookMetadata CopyValue<T>(
        DuplicateMergeFieldSelection selection,
        BookMetadata metadata,
        T? sourceValue,
        Func<T?, BookMetadata> copy)
        where T : struct
    {
        if (selection.Action != DuplicateMergeAction.Copy || sourceValue is null)
        {
            return metadata;
        }

        return copy(sourceValue);
    }

    private static BookMetadata MergeList(
        DuplicateMergeFieldSelection selection,
        BookMetadata metadata,
        IReadOnlyList<string> targetValues,
        IReadOnlyList<string> sourceValues,
        Func<IReadOnlyList<string>, BookMetadata> copy)
    {
        if (selection.Action == DuplicateMergeAction.Copy)
        {
            return sourceValues.Count == 0 ? metadata : copy(CleanList(sourceValues));
        }

        return copy(AppendDistinct(targetValues, sourceValues));
    }

    private static BookMetadata MergeNullableList(
        DuplicateMergeFieldSelection selection,
        BookMetadata metadata,
        IReadOnlyList<string>? targetValues,
        IReadOnlyList<string>? sourceValues,
        Func<IReadOnlyList<string>?, BookMetadata> copy)
    {
        if (selection.Action == DuplicateMergeAction.Copy)
        {
            return sourceValues is null || sourceValues.Count == 0
                ? metadata
                : copy(CleanList(sourceValues));
        }

        var merged = AppendDistinct(targetValues ?? [], sourceValues ?? []);
        return merged.Count == 0 ? metadata : copy(merged);
    }

    private static BookMetadata MergeText(
        DuplicateMergeFieldSelection selection,
        BookMetadata metadata,
        string? targetValue,
        string? sourceValue,
        Func<string?, BookMetadata> copy)
    {
        if (selection.Action == DuplicateMergeAction.Copy)
        {
            return string.IsNullOrWhiteSpace(sourceValue)
                ? metadata
                : copy(sourceValue.Trim());
        }

        if (string.IsNullOrWhiteSpace(sourceValue))
        {
            return metadata;
        }

        if (string.IsNullOrWhiteSpace(targetValue))
        {
            return copy(sourceValue.Trim());
        }

        return string.Equals(targetValue.Trim(), sourceValue.Trim(), StringComparison.Ordinal)
            ? metadata
            : copy($"{targetValue.Trim()}\n\n{sourceValue.Trim()}");
    }

    private static IReadOnlyList<string> AppendDistinct(
        IReadOnlyList<string> targetValues,
        IReadOnlyList<string> sourceValues)
    {
        var values = CleanList(targetValues).ToList();
        foreach (var value in CleanList(sourceValues))
        {
            if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static IReadOnlyList<string> CleanList(IReadOnlyList<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static BookMetadata CopyMetadata(
        BookMetadata metadata,
        string? title = null,
        IReadOnlyList<string>? authors = null,
        string? description = null,
        string? language = null,
        string? publisher = null,
        DateOnly? publicationDate = null,
        IReadOnlyList<string>? tags = null,
        string? series = null,
        decimal? seriesNumber = null,
        string? isbn = null,
        byte[]? coverBytes = null) =>
        new(
            title ?? metadata.Title,
            authors ?? metadata.Authors,
            description ?? metadata.Description,
            language ?? metadata.Language,
            publisher ?? metadata.Publisher,
            publicationDate ?? metadata.PublicationDate,
            tags ?? metadata.Tags,
            series ?? metadata.Series,
            seriesNumber ?? metadata.SeriesNumber,
            isbn ?? metadata.Isbn,
            coverBytes ?? metadata.CoverBytes);
}

public enum DuplicateMergeMetadataPolicy
{
    PreserveTarget,
    MergeSelectedFields
}

public enum DuplicateMergeMetadataField
{
    Cover,
    Title,
    Authors,
    Formats,
    Series,
    SeriesNumber,
    Language,
    Publisher,
    PublicationDate,
    Isbn,
    Tags,
    Description
}

public enum DuplicateMergeAction
{
    NoAction,
    Copy,
    Merge
}

public sealed record DuplicateMergeFieldSelection(
    DuplicateMergeMetadataField Field,
    DuplicateMergeAction Action);

public sealed record DuplicateMergeResult(
    Guid SourceBookId,
    Guid TargetBookId,
    DuplicateMergeMetadataPolicy MetadataPolicy);
