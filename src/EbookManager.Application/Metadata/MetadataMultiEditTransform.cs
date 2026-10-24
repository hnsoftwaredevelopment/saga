using EbookManager.Domain.Books;

namespace EbookManager.Application.Metadata;

public static class MetadataMultiEditTransform
{
    public static Book Apply(Book book, MetadataMultiEditRequest edit)
    {
        var metadata = book.Metadata;
        var title = edit.SwapTitleAndAuthors ? string.Join(", ", metadata.Authors).Trim() : metadata.Title;
        IReadOnlyList<string> authors = edit.SwapTitleAndAuthors ? [metadata.Title.Trim()]
            : edit.UpdateAuthors ? Split(edit.AuthorsText) : metadata.Authors;
        var tags = metadata.Tags;
        if (edit.UpdateTags)
        {
            var values = Split(edit.TagsText);
            tags = edit.TagAction switch
            {
                MultiEditTagAction.Add => (tags ?? []).Concat(values.Where(value => !(tags ?? []).Contains(value, StringComparer.OrdinalIgnoreCase))).ToArray(),
                MultiEditTagAction.Remove => tags?.Where(tag => !values.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToArray(),
                _ => values
            };
            if (tags?.Count == 0) tags = null;
        }
        var series = edit.UpdateSeries ? Blank(edit.SeriesText) : metadata.Series;
        var seriesNumber = edit.UpdateSeries && series is null ? null : metadata.SeriesNumber;
        var language = edit.UpdateLanguage ? Blank(edit.LanguageText) : metadata.Language;
        var status = edit.UpdateStatus ? edit.Status : book.ReadingStatus;
        var updated = new BookMetadata(title, authors, metadata.Description, language, metadata.Publisher,
            metadata.PublicationDate, tags, series, seriesNumber, metadata.Isbn, metadata.CoverBytes);
        return updated == metadata && status == book.ReadingStatus ? book
            : book with { Metadata = updated, ReadingStatus = status, UpdatedUtc = DateTimeOffset.UtcNow };
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IReadOnlyList<string> Split(string? value) => (value ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
