namespace EbookManager.Presentation.ViewModels;

public static class LibraryGridRowProjector
{
    public static bool RequiresProjection(IEnumerable<string> groupColumnNames) =>
        groupColumnNames.Any(IsMultiValueGroupColumn);

    public static IReadOnlyList<string> GetActiveProjectionColumns(IEnumerable<string> groupColumnNames) =>
        groupColumnNames
            .Where(IsMultiValueGroupColumn)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<BookRowViewModel> Project(
        IEnumerable<BookRowViewModel> rows,
        IReadOnlyList<string> groupColumnNames)
    {
        var activeGroupColumns = GetActiveProjectionColumns(groupColumnNames);

        if (activeGroupColumns.Count == 0)
        {
            return rows.ToList();
        }

        return rows
            .SelectMany(row => ProjectRow(row, activeGroupColumns))
            .ToList();
    }

    private static IEnumerable<BookRowViewModel> ProjectRow(
        BookRowViewModel row,
        IReadOnlyList<string> activeGroupColumns)
    {
        IEnumerable<GroupKeySet> projectedKeys = [new GroupKeySet()];

        foreach (var columnName in activeGroupColumns)
        {
            projectedKeys = Expand(projectedKeys, columnName, GetGroupValues(row, columnName));
        }

        return projectedKeys.Select(keys => row.WithGridGroupKeys(
            keys.AuthorsGroupKey,
            keys.TagsGroupKey,
            keys.FormatsGroupKey));
    }

    private static IEnumerable<GroupKeySet> Expand(
        IEnumerable<GroupKeySet> existing,
        string columnName,
        IReadOnlyList<string> values)
    {
        var groupValues = values.Count == 0 ? [string.Empty] : values;
        foreach (var keySet in existing)
        {
            foreach (var value in groupValues)
            {
                yield return columnName switch
                {
                    nameof(BookRowViewModel.AuthorsGroupKey) => keySet with { AuthorsGroupKey = value },
                    nameof(BookRowViewModel.TagsGroupKey) => keySet with { TagsGroupKey = value },
                    nameof(BookRowViewModel.FormatsGroupKey) => keySet with { FormatsGroupKey = value },
                    _ => keySet
                };
            }
        }
    }

    private static IReadOnlyList<string> GetGroupValues(BookRowViewModel row, string columnName) =>
        columnName switch
        {
            nameof(BookRowViewModel.AuthorsGroupKey) => row.Book.Metadata.Authors
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            nameof(BookRowViewModel.TagsGroupKey) => (row.Book.Metadata.Tags ?? [])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            nameof(BookRowViewModel.FormatsGroupKey) => row.Book.Formats
                .Select(format => format.ToString().ToUpperInvariant())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            _ => []
        };

    private static bool IsMultiValueGroupColumn(string columnName) =>
        columnName is nameof(BookRowViewModel.AuthorsGroupKey)
            or nameof(BookRowViewModel.TagsGroupKey)
            or nameof(BookRowViewModel.FormatsGroupKey);

    private sealed record GroupKeySet(
        string? AuthorsGroupKey = null,
        string? TagsGroupKey = null,
        string? FormatsGroupKey = null);
}
