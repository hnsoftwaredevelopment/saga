using System.Globalization;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class LibraryViewModel
{
    private async Task ShowMetadataMultiEditAsync(CancellationToken cancellationToken)
    {
        var selectedIds = SelectedBooks.Select(row => row.Id).ToArray();
        if (selectedIds.Length == 0)
        {
            return;
        }

        var edit = new MetadataMultiEditViewModel(selectedIds.Length, customMetadataFieldDefinitions);
        var result = await userInteraction.ShowMetadataMultiEditAsync(edit, cancellationToken);
        if (result is null)
        {
            return;
        }

        await ApplyMetadataMultiEditAsync(selectedIds, result, cancellationToken);
    }

    private async Task ApplyMetadataMultiEditAsync(
        IReadOnlyCollection<Guid> selectedIds,
        MetadataMultiEditResult result,
        CancellationToken cancellationToken)
    {
        IsCleaningMetadata = true;
        MetadataCleanupStatusText = "Updating metadata...";
        await Task.Yield();
        try
        {
            var persistedBooks = new List<Book>();
            var customChanges = TryCreateCustomMetadataMultiEditChanges(result);
            if (customChanges is null)
            {
                await userInteraction.ShowMessageAsync(
                    localize("MetadataMultiEditTitle"),
                    MetadataCleanupStatusText,
                    cancellationToken);
                return;
            }

            var customMetadataChanged = false;
            var skippedBookCount = 0;
            foreach (var bookId in selectedIds)
            {
                var fullBook = await bookRepository.GetAsync(bookId, cancellationToken);
                if (fullBook is not null)
                {
                    var updatedBook = ApplyMetadataMultiEdit(fullBook, result);
                    if (updatedBook != fullBook)
                    {
                        try
                        {
                            await bookRepository.UpdateAsync(updatedBook, cancellationToken);
                            persistedBooks.Add(updatedBook);
                        }
                        catch (BookConflictException)
                        {
                            // Keep conflicting books unchanged; later slices can expose skipped books in the dialog.
                            skippedBookCount++;
                        }
                    }

                    if (customChanges.Count > 0 &&
                        customMetadataRepository is not null &&
                        await ApplyCustomMetadataMultiEditAsync(bookId, customChanges, cancellationToken))
                    {
                        customMetadataChanged = true;
                    }
                }
            }

            if (customMetadataChanged)
            {
                await RefreshCustomMetadataValuesForBooksAsync(selectedIds, cancellationToken);
                if (SelectedBook is { } selectedBook &&
                    selectedIds.Contains(selectedBook.Id))
                {
                    await Details.LoadCustomMetadataValuesAsync(selectedBook.Id, cancellationToken);
                }
            }

            if (persistedBooks.Count > 0)
            {
                ApplyPersistedMetadataChanges(persistedBooks, refreshDisplay: !customMetadataChanged);
            }

            if (customMetadataChanged)
            {
                RefreshFacetFilters();
                ApplyFilter();
            }

            if (skippedBookCount > 0)
            {
                await userInteraction.ShowMessageAsync(
                    localize("MetadataMultiEditTitle"),
                    string.Format(
                        CultureInfo.CurrentCulture,
                        localize("MetadataMultiEditSkippedConflicts"),
                        skippedBookCount),
                    cancellationToken);
            }
        }
        finally
        {
            IsCleaningMetadata = false;
        }
    }

    private static Book ApplyMetadataMultiEdit(Book book, MetadataMultiEditResult result)
    {
        var metadata = book.Metadata;
        var title = metadata.Title;
        var authors = result.UpdateAuthors ? SplitRequiredList(result.AuthorsText) : metadata.Authors;
        if (result.SwapTitleAndAuthors)
        {
            title = string.Join(", ", metadata.Authors).Trim();
            authors = [metadata.Title.Trim()];
        }

        var tags = result.UpdateTags ? ApplyTagAction(metadata.Tags, result.TagAction, result.TagsText) : metadata.Tags;
        var series = result.UpdateSeries ? NormalizeBlank(result.SeriesText) : metadata.Series;
        var seriesNumber = result.UpdateSeries && series is null ? null : metadata.SeriesNumber;
        var language = result.UpdateLanguage ? NormalizeBlank(result.LanguageText) : metadata.Language;
        var readingStatus = result.UpdateStatus ? result.Status : book.ReadingStatus;

        if (title == metadata.Title &&
            authors.SequenceEqual(metadata.Authors) &&
            NullableSequenceEqual(tags, metadata.Tags) &&
            series == metadata.Series &&
            seriesNumber == metadata.SeriesNumber &&
            language == metadata.Language &&
            readingStatus == book.ReadingStatus)
        {
            return book;
        }

        return book with
        {
            Metadata = CopyMetadata(metadata, title, authors, tags, series, language, seriesNumber),
            ReadingStatus = readingStatus,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private IReadOnlyList<CustomMetadataValueChange>? TryCreateCustomMetadataMultiEditChanges(
        MetadataMultiEditResult result)
    {
        try
        {
            return (result.CustomFields ?? [])
                .Select(field => string.IsNullOrWhiteSpace(field.ValueText)
                    ? new CustomMetadataValueChange(Guid.Empty, field.FieldId, null)
                    : new CustomMetadataValueChange(Guid.Empty, field.FieldId, CreateCustomMetadataValue(Guid.Empty, field)))
                .ToArray();
        }
        catch (FormatException exception)
        {
            MetadataCleanupStatusText = CustomMetadataValueParser.TryFormatValidationMessage(exception, localize, out var message)
                ? message
                : exception.Message;
            return null;
        }
        catch (InvalidOperationException exception)
        {
            MetadataCleanupStatusText = CustomMetadataValueParser.TryFormatValidationMessage(exception, localize, out var message)
                ? message
                : exception.Message;
            return null;
        }
    }

    private async Task<bool> ApplyCustomMetadataMultiEditAsync(
        Guid bookId,
        IReadOnlyList<CustomMetadataValueChange> changes,
        CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null || changes.Count == 0)
        {
            return false;
        }

        foreach (var change in changes)
        {
            if (change.Value is null)
            {
                await customMetadataRepository.DeleteValueAsync(bookId, change.FieldId, cancellationToken);
                continue;
            }

            await customMetadataRepository.SetValueAsync(
                change.Value with { BookId = bookId, UpdatedUtc = DateTimeOffset.UtcNow },
                cancellationToken);
        }

        return true;
    }

}
