using EbookManager.Domain.Tags;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class TagManagementViewModel
{
    private readonly HashSet<Guid> selectedBookIds = [];
    public IReadOnlyCollection<Guid> SelectedBookIds => selectedBookIds.ToArray();
    public int SelectedBookCount => selectedBookIds.Count;
    public bool CanChangeSelectedBooks => CanBrowse && assignments is not null && TagId is not null && SelectedBookCount > 0;
    public string SelectedBookSummary => string.Format(System.Globalization.CultureInfo.CurrentCulture, localize("TagSelectedBooks"), SelectedBookCount);

    public void SetSelectedBooks(IEnumerable<TagBook> selected)
    {
        var loaded = Books.Select(book => book.Id).ToHashSet();
        var ids = selected.Select(book => book.Id).Where(loaded.Contains).ToHashSet();
        if (ids.SetEquals(selectedBookIds)) return;
        selectedBookIds.Clear();
        selectedBookIds.UnionWith(ids);
        OnPropertyChanged(nameof(SelectedBookCount));
        OnPropertyChanged(nameof(SelectedBookSummary));
        OnPropertyChanged(nameof(CanChangeSelectedBooks));
    }

    public async Task<TagAssignmentPreview?> PreviewAssignmentAsync()
    {
        if (!CanChangeSelectedBooks || assignments is null) return null;
        var ids = selectedBookIds.ToArray();
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => assignments.PreviewAsync(TagId!.Value, ids, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview.TagId != TagId || preview.Name != Name || !ids.ToHashSet().SetEquals(preview.Books.Select(book => book.Id)) ||
                !selectedBookIds.SetEquals(ids))
            { SetStatus("TagAssignmentConflict", true); return null; }
            return preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("TagLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<bool> ApplyAssignmentAsync(TagAssignmentPreview preview, TagAssignmentRequest request)
    {
        if (!CanChangeSelectedBooks || assignments is null || preview.TagId != TagId || preview.Name != Name ||
            !selectedBookIds.SetEquals(preview.Books.Select(book => book.Id))) return false;
        var bookExtent = Math.Max(200, Books.Count);
        var tagExtent = Math.Max(200, Tags.Count);
        IsBusy = true;
        ++searchVersion;
        IsSearching = false;
        Warnings = [];
        SetStatus("TagAssignmentWorking", false);
        var committed = false;
        try
        {
            var result = await Task.Run(() => assignments.ApplyAsync(preview, request, lifetime.Token), lifetime.Token);
            committed = result.Change.Status == TagAssignmentStatus.Applied;
            if (committed) changedBookIds.UnionWith(result.Change.Books.Select(book => book.Id));
            if (disposed) return committed;
            if (!committed)
            {
                SetStatus(result.Change.Status switch
                {
                    TagAssignmentStatus.Unchanged => "TagAssignmentUnchanged",
                    TagAssignmentStatus.InvalidName => "TagRenameInvalid",
                    _ => "TagAssignmentConflict"
                }, result.Change.Status != TagAssignmentStatus.Unchanged);
                return false;
            }
            SetSelectedBooks([]);
            Warnings = result.Warnings;
            // Memberships changed, so reread both the tag counts and source pages instead of retaining old rows.
            if (!await SearchCoreAsync(Query)) throw new InvalidOperationException("Tag index refresh failed.");
            while (Tags.Count < tagExtent && Tags.Count < TotalTags)
            {
                var previous = Tags.Count;
                if (!await SearchCoreAsync(Query, append: true)) throw new InvalidOperationException("Tag index refresh failed.");
                if (Tags.Count == previous) break;
            }
            var loaded = new List<TagBook>();
            TagBookPage page;
            do
            {
                page = await Task.Run(() => repository.ListBooksAsync(preview.TagId, loaded.Count, 200, lifetime.Token), lifetime.Token);
                loaded.AddRange(page.Books);
            } while (page.Books.Count > 0 && loaded.Count < Math.Min(bookExtent, page.TotalCount));
            if (disposed) return true;
            TotalBooks = page.TotalCount;
            Books = loaded.DistinctBy(book => book.Id).ToArray();
            if (TotalBooks == 0) { TagId = null; Name = ""; }
            else Name = Tags.FirstOrDefault(tag => tag.Id == preview.TagId)?.Name ?? Name;
            NotifySelection();
            StatusMessage = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                localize(Warnings.Count == 0 ? "TagAssignmentSaved" : "TagAssignmentSavedWithWarnings"), result.Change.Books.Count, Warnings.Count);
            IsError = Warnings.Count > 0;
            return true;
        }
        catch (OperationCanceledException) { return committed; }
        catch (Exception)
        {
            if (!disposed) SetStatus(committed ? "TagAssignmentRefreshFailed" : "TagAssignmentFailed", true);
            return committed;
        }
        finally { if (!disposed) IsBusy = false; }
    }
}
