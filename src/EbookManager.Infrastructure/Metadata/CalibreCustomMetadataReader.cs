using System.Globalization;
using System.Text.Json;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.CustomMetadata;
using Microsoft.Data.Sqlite;

namespace EbookManager.Infrastructure.Metadata;

public sealed class CalibreCustomMetadataReader : IExternalCustomMetadataReader
{
    private const string DatabaseFileName = "metadata.db";

    public async Task<IReadOnlyList<ExternalCustomMetadataValue>> ReadAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath));
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            return [];
        }

        var library = FindLibrary(sourceDirectory);
        if (library is null)
        {
            return [];
        }

        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = library.DatabasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString());
        await connection.OpenAsync(cancellationToken);

        var bookId = await FindBookIdAsync(connection, library.RelativeBookPath, cancellationToken);
        if (bookId is null)
        {
            return [];
        }

        var tableNames = await ReadTableNamesAsync(connection, cancellationToken);
        var columns = await ReadColumnsAsync(connection, cancellationToken);
        var values = new List<ExternalCustomMetadataValue>();
        foreach (var column in columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryMapField(column, out var field))
            {
                continue;
            }

            var rawValues = await ReadColumnValuesAsync(connection, tableNames, column.Id, bookId.Value, cancellationToken);
            var value = CreateValue(field, rawValues);
            if (value is not null)
            {
                values.Add(value);
            }
        }

        return values.AsReadOnly();
    }

    private static CalibreLibraryReference? FindLibrary(string sourceDirectory)
    {
        var current = new DirectoryInfo(sourceDirectory);
        while (current is not null)
        {
            var databasePath = Path.Combine(current.FullName, DatabaseFileName);
            if (File.Exists(databasePath))
            {
                var relativeBookPath = Path.GetRelativePath(current.FullName, sourceDirectory)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace(Path.AltDirectorySeparatorChar, '/');
                return relativeBookPath == "."
                    ? null
                    : new CalibreLibraryReference(databasePath, relativeBookPath);
            }

            current = current.Parent;
        }

        return null;
    }

    private static async Task<int?> FindBookIdAsync(
        SqliteConnection connection,
        string relativeBookPath,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM books WHERE path = $path LIMIT 1";
        command.Parameters.AddWithValue("$path", relativeBookPath);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is long id ? checked((int)id) : null;
    }

    private static async Task<HashSet<string>> ReadTableNamesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<IReadOnlyList<CalibreCustomColumn>> ReadColumnsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, label, name, datatype, is_multiple, display
            FROM custom_columns
            WHERE mark_for_delete = 0
            ORDER BY id
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new List<CalibreCustomColumn>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new CalibreCustomColumn(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetString(5)));
        }

        return columns.AsReadOnly();
    }

    private static bool TryMapField(
        CalibreCustomColumn column,
        out ExternalCustomMetadataField field)
    {
        field = default!;
        var type = column.Datatype.ToLowerInvariant() switch
        {
            "enumeration" => column.IsMultiple
                ? CustomMetadataFieldType.MultiSelect
                : CustomMetadataFieldType.SingleSelect,
            "text" or "comments" => column.IsMultiple
                ? CustomMetadataFieldType.MultiSelect
                : CustomMetadataFieldType.Text,
            "int" or "float" or "rating" => CustomMetadataFieldType.Number,
            "datetime" => CustomMetadataFieldType.Date,
            "bool" => CustomMetadataFieldType.Boolean,
            _ => (CustomMetadataFieldType?)null
        };
        if (type is null)
        {
            return false;
        }

        field = new ExternalCustomMetadataField(
            column.Label,
            column.Name,
            type.Value,
            type is CustomMetadataFieldType.SingleSelect or CustomMetadataFieldType.MultiSelect
                ? ReadEnumerationOptions(column.DisplayJson)
                : []);
        return true;
    }

    private static async Task<IReadOnlyList<string>> ReadColumnValuesAsync(
        SqliteConnection connection,
        HashSet<string> tableNames,
        int columnId,
        int bookId,
        CancellationToken cancellationToken)
    {
        var valueTable = $"custom_column_{columnId}";
        var linkTable = $"books_custom_column_{columnId}_link";
        if (!tableNames.Contains(valueTable))
        {
            return [];
        }

        await using var command = connection.CreateCommand();
        if (tableNames.Contains(linkTable))
        {
            command.CommandText = $"""
                SELECT c.value
                FROM {linkTable} l
                JOIN {valueTable} c ON c.id = l.value
                WHERE l.book = $book
                ORDER BY c.value
                """;
        }
        else
        {
            command.CommandText = $"SELECT value FROM {valueTable} WHERE book = $book";
        }

        command.Parameters.AddWithValue("$book", bookId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var values = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                var value = Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture)?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
        }

        return values.AsReadOnly();
    }

    private static ExternalCustomMetadataValue? CreateValue(
        ExternalCustomMetadataField field,
        IReadOnlyList<string> rawValues)
    {
        if (rawValues.Count == 0)
        {
            return null;
        }

        return field.Type switch
        {
            CustomMetadataFieldType.Text or CustomMetadataFieldType.SingleSelect =>
                new ExternalCustomMetadataValue(field, TextValue: rawValues[0]),
            CustomMetadataFieldType.MultiSelect =>
                new ExternalCustomMetadataValue(
                    field,
                    TextValue: string.Join("; ", rawValues.Distinct(StringComparer.OrdinalIgnoreCase))),
            CustomMetadataFieldType.Number when decimal.TryParse(
                rawValues[0],
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number) =>
                new ExternalCustomMetadataValue(field, NumberValue: number),
            CustomMetadataFieldType.Date when TryParseDate(rawValues[0], out var date) =>
                new ExternalCustomMetadataValue(field, DateValue: date),
            CustomMetadataFieldType.Boolean when TryParseBoolean(rawValues[0], out var boolean) =>
                new ExternalCustomMetadataValue(field, BooleanValue: boolean),
            _ => null
        };
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out date))
        {
            return true;
        }

        if (value.Length >= 10 &&
            DateOnly.TryParseExact(
                value[..10],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            return true;
        }

        date = default;
        return false;
    }

    private static bool TryParseBoolean(string value, out bool boolean)
    {
        if (bool.TryParse(value, out boolean))
        {
            return true;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
            number is 0 or 1)
        {
            boolean = number == 1;
            return true;
        }

        boolean = false;
        return false;
    }

    private static IReadOnlyList<string> ReadEnumerationOptions(string displayJson)
    {
        if (string.IsNullOrWhiteSpace(displayJson))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(displayJson);
            return document.RootElement.TryGetProperty("enum_values", out var values) &&
                values.ValueKind == JsonValueKind.Array
                    ? values
                        .EnumerateArray()
                        .Where(value => value.ValueKind == JsonValueKind.String)
                        .Select(value => value.GetString()?.Trim())
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                    : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record CalibreLibraryReference(string DatabasePath, string RelativeBookPath);

    private sealed record CalibreCustomColumn(
        int Id,
        string Label,
        string Name,
        string Datatype,
        bool IsMultiple,
        string DisplayJson);
}
