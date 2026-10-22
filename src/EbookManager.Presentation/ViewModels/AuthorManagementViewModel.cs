using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Authors;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class AuthorManagementViewModel(
    IAuthorProfileRepository repository, IAuthorPhotoStore photos, Func<string, string> localize) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly AuthorProfileService service = new(repository, photos);
    private AuthorProfile? original;
    private byte[]? newPhoto;
    private bool removePhoto;
    private int selectionVersion;
    private int searchVersion;
    private bool disposed;
    private string currentQuery = "";

    [ObservableProperty] private IReadOnlyList<AuthorSummary> authors = [];
    [ObservableProperty] private IReadOnlyList<AuthorBook> books = [];
    [ObservableProperty] private int totalAuthors;
    [ObservableProperty] private int totalBooks;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string biography = "";
    [ObservableProperty] private string website = "";
    [ObservableProperty] private byte[]? photoBytes;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isError;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool isBusy;

    public bool HasProfile => original is not null;
    public bool CanEdit => HasProfile && !IsBusy && !disposed;
    public bool CanSave => CanEdit && HasChanges;
    public bool HasChanges => original is not null &&
        (AuthorProfileValidation.NormalizeOptional(Biography) != original.Biography ||
         AuthorProfileValidation.NormalizeOptional(Website) != original.WebsiteUrl || newPhoto is not null || removePhoto);
    public bool HasMoreAuthors => Authors.Count < TotalAuthors;
    public bool HasMoreBooks => Books.Count < TotalBooks;

    partial void OnBiographyChanged(string value) => DraftChanged();
    partial void OnWebsiteChanged(string value) => DraftChanged();
    private void DraftChanged()
    {
        OnPropertyChanged(nameof(HasChanges));
        OnPropertyChanged(nameof(CanSave));
    }

    public async Task SearchAsync(string query, bool append = false)
    {
        if (disposed) return;
        var version = ++searchVersion;
        currentQuery = query;
        try
        {
            var page = await Task.Run(() => repository.SearchAsync(query, append ? Authors.Count : 0, 200, lifetime.Token), lifetime.Token);
            if (disposed || version != searchVersion) return;
            Authors = append ? Authors.Concat(page.Authors).ToArray() : page.Authors;
            TotalAuthors = page.TotalCount;
            OnPropertyChanged(nameof(HasMoreAuthors));
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed && version == searchVersion) SetStatus("AuthorProfileLoadFailed", true); }
    }

    public async Task SelectAsync(AuthorSummary author)
    {
        if (disposed || HasChanges) return;
        var version = ++selectionVersion;
        IsBusy = true;
        SetStatus("", false);
        try
        {
            var profile = await Task.Run(() => repository.GetAsync(author.Id, lifetime.Token), lifetime.Token);
            var authorBooks = await Task.Run(() => repository.ListBooksAsync(author.Id, 0, 200, lifetime.Token), lifetime.Token);
            byte[]? photo = null;
            var photoFailed = false;
            if (profile?.PhotoRelativePath is not null)
            {
                try { photo = await photos.ReadAsync(author.Id, profile.PhotoRelativePath, lifetime.Token); }
                catch (OperationCanceledException) { throw; }
                catch (Exception) { photoFailed = true; }
            }
            if (disposed || version != selectionVersion) return;
            original = profile;
            Books = authorBooks;
            TotalBooks = author.BookCount;
            ApplyProfile(photo);
            if (profile is null) SetStatus("AuthorProfileConflict", true);
            else if (photoFailed) SetStatus("AuthorPhotoLoadFailed", true);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed && version == selectionVersion) SetStatus("AuthorProfileLoadFailed", true); }
        finally { if (!disposed && version == selectionVersion) IsBusy = false; }
    }

    public async Task LoadMoreBooksAsync()
    {
        if (!CanEdit || !HasMoreBooks) return;
        IsBusy = true;
        try
        {
            var page = await Task.Run(() => repository.ListBooksAsync(original!.Id, Books.Count, 200, lifetime.Token), lifetime.Token);
            if (!disposed) { Books = Books.Concat(page).ToArray(); OnPropertyChanged(nameof(HasMoreBooks)); }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed) SetStatus("AuthorProfileLoadFailed", true); }
        finally { if (!disposed) IsBusy = false; }
    }

    public void SetPhoto(byte[] jpeg)
    {
        if (!CanEdit) return;
        newPhoto = jpeg.ToArray();
        removePhoto = false;
        PhotoBytes = newPhoto;
        DraftChanged();
    }

    public void RemovePhoto()
    {
        if (!CanEdit) return;
        newPhoto = null;
        removePhoto = original?.PhotoRelativePath is not null;
        PhotoBytes = null;
        DraftChanged();
    }

    public void DiscardChanges() => ApplyProfile(null, preservePhoto: true);

    private void ApplyProfile(byte[]? photo, bool preservePhoto = false)
    {
        newPhoto = null;
        removePhoto = false;
        Name = original?.Name ?? "";
        Biography = original?.Biography ?? "";
        Website = original?.WebsiteUrl ?? "";
        if (!preservePhoto) savedPhoto = photo;
        PhotoBytes = savedPhoto;
        OnPropertyChanged(nameof(HasProfile));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(HasMoreBooks));
        DraftChanged();
    }
    private byte[]? savedPhoto;

    public async Task<bool> SaveAsync()
    {
        if (!CanSave) return !HasChanges;
        if (!AuthorProfileValidation.IsValidWebsite(Website) || Biography.Length > 10000)
        {
            SetStatus("AuthorProfileInvalid", true);
            return false;
        }
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => service.SaveAsync(original!, Biography, Website, newPhoto, removePhoto, lifetime.Token), lifetime.Token);
            if (disposed) return false;
            if (result.Status == AuthorProfileSaveStatus.Conflict)
            {
                SetStatus("AuthorProfileConflict", true);
                return false;
            }
            original = result.Profile;
            ApplyProfile(PhotoBytes);
            SetStatus(result.CleanupWarning ? "AuthorProfileCleanupWarning" : "AuthorProfileSaved", result.CleanupWarning);
            await SearchAsync(currentQuery);
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception) { if (!disposed) SetStatus("AuthorProfileSaveFailed", true); return false; }
        finally { if (!disposed) IsBusy = false; }
    }

    public void SetStatus(string key, bool error)
    {
        StatusMessage = key.Length == 0 ? "" : localize(key);
        IsError = error;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
