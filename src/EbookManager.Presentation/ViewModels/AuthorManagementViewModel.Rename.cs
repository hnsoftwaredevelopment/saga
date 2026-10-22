using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Authors;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class AuthorManagementViewModel
{
    private bool renaming;
    private readonly HashSet<Guid> changedBookIds = [];
    [ObservableProperty] private IReadOnlyList<AuthorBook> renameWarnings = [];
    public IReadOnlyCollection<Guid> ChangedBookIds => changedBookIds.ToArray();
    public bool CanRename => CanEdit && renameService is not null;

    public async Task<AuthorRenamePreview?> PreviewRenameAsync()
    {
        if (!CanRename || HasChanges) return null;
        var profile = original!;
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => renameService!.PreviewAsync(profile.Id, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (original?.Id != profile.Id || preview?.Profile.Name != profile.Name)
            {
                SetStatus("AuthorRenameConflict", true);
                return null;
            }
            return preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("AuthorProfileLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<bool> RenameAsync(AuthorRenamePreview preview, string name)
    {
        if (!CanRename || HasChanges || preview.Profile.Id != original?.Id || preview.Profile.Name != original?.Name) return false;
        renaming = true;
        IsBusy = true;
        RenameWarnings = [];
        SetStatus("AuthorRenameWorking", false);
        try
        {
            var result = await Task.Run(() => renameService!.RenameAsync(preview, name, lifetime.Token), lifetime.Token);
            if (disposed) return false;
            if (result.Change.Status != AuthorRenameStatus.Renamed)
            {
                var key = result.Change.Status switch
                {
                    AuthorRenameStatus.NameExists => "AuthorRenameNameExists",
                    AuthorRenameStatus.InvalidName => "AuthorRenameInvalid",
                    AuthorRenameStatus.DuplicateBook => "AuthorRenameDuplicateBook",
                    AuthorRenameStatus.Unchanged => "AuthorRenameUnchanged",
                    _ => "AuthorRenameConflict"
                };
                SetStatus(key, result.Change.Status != AuthorRenameStatus.Unchanged);
                return false;
            }
            changedBookIds.UnionWith(result.Change.BookIds);
            original = result.Change.Profile;
            ApplyProfile(savedPhoto);
            // Titles and series did not change: keep the loaded pages in their existing title order.
            TotalBooks = result.Change.Books.Count;
            OnPropertyChanged(nameof(HasMoreBooks));
            RenameWarnings = result.Warnings;
            StatusMessage = string.Format(CultureInfo.CurrentCulture,
                localize(result.Warnings.Count == 0 ? "AuthorRenameSaved" : "AuthorRenameSavedWithWarnings"),
                result.Change.Books.Count, result.Warnings.Count);
            IsError = result.Warnings.Count > 0;
            await SearchAsync("");
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception) { if (!disposed) SetStatus("AuthorRenameFailed", true); return false; }
        finally
        {
            renaming = false;
            if (!disposed) IsBusy = false;
        }
    }
}
