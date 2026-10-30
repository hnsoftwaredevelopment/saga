using EbookManager.Domain.Authors;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class AuthorManagementViewModel
{
    private bool merging;
    private readonly HashSet<Guid> mergeAuthorIds = [];
    public IReadOnlyCollection<Guid> MergeAuthorIds => mergeAuthorIds.ToArray();
    public int MergeSelectionCount => mergeAuthorIds.Count;
    public string MergeSelectionSummary => string.Format(CultureInfo.CurrentCulture, localize("AuthorMergeSelected"), MergeSelectionCount);
    public bool CanMerge => CanChooseAuthors && !HasChanges && mergeService is not null && MergeSelectionCount >= 2;

    public void SetMergeAuthors(IEnumerable<AuthorSummary> selected)
    {
        var loaded = Authors.Select(author => author.Id).ToHashSet();
        var ids = selected.Select(author => author.Id).Where(loaded.Contains).ToHashSet();
        if (mergeAuthorIds.SetEquals(ids)) return;
        mergeAuthorIds.Clear();
        mergeAuthorIds.UnionWith(ids);
        OnPropertyChanged(nameof(MergeSelectionCount));
        OnPropertyChanged(nameof(MergeSelectionSummary));
        OnPropertyChanged(nameof(CanMerge));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanRename));
    }

    public Task<byte[]?> ReadMergePhotoAsync(Guid id, string path) => photos.ReadAsync(id, path, lifetime.Token);

    public async Task<AuthorMergePreview?> PreviewMergeAsync()
    {
        if (!CanMerge) return null;
        var ids = mergeAuthorIds.ToArray();
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => mergeService!.PreviewAsync(ids, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview is null || !ids.ToHashSet().SetEquals(preview.Authors.Select(author => author.Profile.Id)) || !mergeAuthorIds.SetEquals(ids))
            { SetStatus("AuthorMergeConflict", true); return null; }
            return preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("AuthorProfileLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }

    public Task<bool> MergeAsync(AuthorMergePreview preview, AuthorMergeSelection selection) =>
        CanMerge && mergeAuthorIds.SetEquals(preview.Authors.Select(author => author.Profile.Id))
            ? MergeCoreAsync(preview, selection) : Task.FromResult(false);

    private async Task<bool> MergeCoreAsync(AuthorMergePreview preview, AuthorMergeSelection selection)
    {
        merging = true;
        IsBusy = true;
        ++selectionVersion;
        ++searchVersion;
        IsSearching = false;
        RenameWarnings = [];
        SetStatus("AuthorMergeWorking", false);
        var committed = false;
        try
        {
            var completion = await Task.Run(() => mergeService!.MergeAsync(preview, selection, lifetime.Token), lifetime.Token);
            var change = completion.Change;
            committed = change.Status == AuthorMergeStatus.Merged;
            if (committed) changedBookIds.UnionWith(change.Books.Select(book => book.Id));
            if (disposed) return committed;
            RenameWarnings = completion.Warnings;
            if (!committed)
            {
                SetStatus(change.Status == AuthorMergeStatus.DuplicateBook ? "AuthorRenameDuplicateBook" : "AuthorMergeConflict", true);
                if (completion.CleanupWarning) StatusMessage += "\n" + localize("AuthorMergeCleanupWarning");
                return false;
            }
            original = change.Profile;
            SetMergeAuthors([]);
            Books = [];
            TotalBooks = 0;
            ApplyProfile(null);
            if (!await SearchCoreAsync(change.Profile!.Name)) throw new InvalidOperationException("Merged author index reload failed.");
            if (disposed) return true;
            var summary = Authors.FirstOrDefault(author => author.Id == selection.TargetId);
            if (summary is null) throw new InvalidOperationException("Merged author index is unavailable.");
            Books = await Task.Run(() => repository.ListBooksAsync(selection.TargetId, 0, 200, lifetime.Token), lifetime.Token);
            TotalBooks = summary.BookCount;
            var photo = original!.PhotoRelativePath is null ? null : await photos.ReadAsync(original.Id, original.PhotoRelativePath, lifetime.Token);
            if (disposed) return true;
            ApplyProfile(photo);
            SetMergeAuthors([summary]);
            StatusMessage = string.Format(CultureInfo.CurrentCulture, localize(completion.Warnings.Count == 0
                ? "AuthorMergeSaved" : "AuthorMergeSavedWithWarnings"), change.Books.Count, completion.Warnings.Count);
            if (completion.CleanupWarning) StatusMessage += "\n" + localize("AuthorMergeCleanupWarning");
            IsError = completion.Warnings.Count > 0 || completion.CleanupWarning;
            return true;
        }
        catch (OperationCanceledException) { return committed; }
        catch (Exception) { if (!disposed) SetStatus(committed ? "AuthorRenameRefreshFailed" : "AuthorMergeFailed", true); return committed; }
        finally { merging = false; if (!disposed) IsBusy = false; }
    }
}
