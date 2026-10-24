using System.Collections.ObjectModel;
using System.Globalization;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Presentation.ViewModels;

public sealed record MetadataMultiEditDetail(string Title, string Authors, string Reason);

public sealed partial class LibraryViewModel
{
    private bool multiEditActive;
    private long multiEditSelectionVersion;
    private string? multiEditMessage;
    private bool multiEditIsError;
    public string? MultiEditMessage { get => multiEditMessage; private set => SetProperty(ref multiEditMessage, value); }
    public bool MultiEditIsError { get => multiEditIsError; private set => SetProperty(ref multiEditIsError, value); }
    public ObservableCollection<MetadataMultiEditDetail> MultiEditDetails { get; } = [];
    public bool HasMultiEditDetails => MultiEditDetails.Count > 0;

    private async Task ShowMetadataMultiEditAsync(CancellationToken cancellationToken)
    {
        if (multiEditActive || IsCleaningMetadata) return;
        var selectedIds = SelectedBooks.Select(row => row.Id).Distinct().ToArray();
        if (selectedIds.Length == 0) return;
        var libraryPath = CurrentLibraryPath;
        var selection = multiEditSelectionVersion;
        var labels = SelectedBooks.ToDictionary(row => row.Id, row => (row.Title, row.Authors));
        multiEditActive = true;
        NotifyMultiEditAvailability();
        MultiEditDetails.Clear();
        OnPropertyChanged(nameof(HasMultiEditDetails));
        MultiEditMessage = null;
        try
        {
            var edit = new MetadataMultiEditViewModel(selectedIds.Length, customMetadataFieldDefinitions);
            var result = await userInteraction.ShowMetadataMultiEditAsync(edit, cancellationToken);
            if (result is null || libraryPath != CurrentLibraryPath) return;
            var custom = TryCreateCustomMetadataMultiEditChanges(result);
            if (custom is null)
            {
                MultiEditIsError = true;
                MultiEditMessage = MetadataCleanupStatusText;
                await userInteraction.ShowMessageAsync(localize("MetadataMultiEditTitle"), MetadataCleanupStatusText, cancellationToken);
                return;
            }
            IsCleaningMetadata = true;
            MetadataCleanupStatusText = localize("MultiEditWorking");
            var names = (result.CustomFields ?? []).ToDictionary(field => field.FieldId, field => field.Name);
            var request = new MetadataMultiEditRequest(result.UpdateAuthors, result.AuthorsText, result.UpdateSeries,
                result.SeriesText, result.UpdateTags, result.TagsText, result.UpdateLanguage, result.LanguageText,
                result.UpdateStatus, result.Status, result.SwapTitleAndAuthors,
                result.TagAction switch { MetadataMultiEditTagAction.Add => MultiEditTagAction.Add, MetadataMultiEditTagAction.Remove => MultiEditTagAction.Remove, _ => MultiEditTagAction.Replace },
                custom.Select(change => new MultiEditCustomChange(change.FieldId, names[change.FieldId], change.Value)).ToArray());
            var service = new MetadataMultiEditService(bookRepository, bookService, customMetadataRepository);
            var results = await Task.Run(() => service.ApplyAsync(selectedIds, request, cancellationToken), CancellationToken.None);
            if (libraryPath != CurrentLibraryPath) return;
            var keepSelection = selection == multiEditSelectionVersion;
            var persisted = results.Where(item => item.Book is not null).Select(item => item.Book!).ToArray();
            if (custom.Count > 0)
            {
                await RefreshCustomMetadataValuesForBooksAsync(selectedIds, CancellationToken.None);
                if (SelectedBook is { } selected && selectedIds.Contains(selected.Id))
                    await Details.LoadCustomMetadataValuesAsync(selected.Id, CancellationToken.None);
            }
            if (libraryPath != CurrentLibraryPath) return;
            ApplyPersistedMetadataChanges(persisted);
            if (keepSelection)
            {
                var failed = results.Where(item => item.Status is MultiEditStatus.Failed or MultiEditStatus.Partial or MultiEditStatus.NotProcessed)
                    .Select(item => item.BookId).ToHashSet();
                SetSelectedBooks(VisibleBooks.Where(row => failed.Contains(row.Id)));
            }
            foreach (var item in results.Where(item => item.Status is not (MultiEditStatus.Updated or MultiEditStatus.Unchanged)))
            {
                var label = labels.GetValueOrDefault(item.BookId);
                var key = item.Status switch
                {
                    MultiEditStatus.NotFound => "MultiEditUnavailable",
                    MultiEditStatus.NotProcessed => "MultiEditNotProcessed",
                    MultiEditStatus.FileWarning => "MultiEditFileWarning",
                    MultiEditStatus.Partial => "MultiEditPartial",
                    _ => item.Failure switch
                    {
                        MultiEditFailure.Duplicate => "MultiEditDuplicate",
                        MultiEditFailure.Read => "MultiEditReadFailed",
                        MultiEditFailure.Custom or MultiEditFailure.CustomUnavailable => "MultiEditCustomFailed",
                        _ => "MultiEditSaveFailed"
                    }
                };
                var reason = localize(item.Failure == MultiEditFailure.Uncertain ? "MultiEditUncertain" : key);
                if (item.CustomFields is { Count: > 0 }) reason += " " + string.Join(", ", item.CustomFields);
                if (item.HasFileWarning) reason += " " + localize("MultiEditFileWarning");
                MultiEditDetails.Add(new(label.Title ?? item.Book?.Metadata.Title ?? "", label.Authors ?? "", reason));
            }
            OnPropertyChanged(nameof(HasMultiEditDetails));
            MultiEditIsError = MultiEditDetails.Count > 0;
            var updated = results.Count(item => item.Status == MultiEditStatus.Updated);
            var unchanged = results.Count(item => item.Status == MultiEditStatus.Unchanged);
            MultiEditMessage = MultiEditIsError
                ? string.Format(CultureInfo.CurrentCulture, localize("MultiEditResult"), updated, unchanged, MultiEditDetails.Count)
                : string.Format(CultureInfo.CurrentCulture, localize(unchanged == 0 ? "MultiEditSaved" : "MultiEditSavedUnchanged"), updated, unchanged);
        }
        catch (OperationCanceledException) { MultiEditIsError = true; MultiEditMessage = localize("MultiEditNotProcessed"); }
        catch (Exception) { MultiEditIsError = true; MultiEditMessage = localize("MultiEditSaveFailed"); }
        finally
        {
            IsCleaningMetadata = false;
            multiEditActive = false;
            NotifyMultiEditAvailability();
        }
    }

    private void NotifyMultiEditAvailability()
    {
        OnPropertyChanged(nameof(CanMultiEditSelectedBooks));
        showMetadataMultiEditCommand?.NotifyCanExecuteChanged();
        createLibraryCommand?.NotifyCanExecuteChanged();
        openLibraryCommand?.NotifyCanExecuteChanged();
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

}
