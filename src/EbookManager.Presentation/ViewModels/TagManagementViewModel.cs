using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Tags;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Tags;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class TagManagementViewModel(ITagRepository repository, ITagRenameService renameService,
    Func<string, string> localize) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly HashSet<Guid> changedBookIds = [];
    private int searchVersion;
    private bool disposed;
    [ObservableProperty] private Guid? tagId;
    [ObservableProperty] private string query = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private IReadOnlyList<TagSummary> tags = [];
    [ObservableProperty] private IReadOnlyList<TagBook> books = [];
    [ObservableProperty] private IReadOnlyList<TagSourceBook> warnings = [];
    [ObservableProperty] private int totalTags;
    [ObservableProperty] private int totalBooks;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isError;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBrowse))]
    [NotifyPropertyChangedFor(nameof(CanRename))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyPropertyChangedFor(nameof(HasMoreBooks))]
    [NotifyPropertyChangedFor(nameof(HasMoreTags))]
    private bool isBusy;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    [NotifyPropertyChangedFor(nameof(HasMoreTags))]
    private bool isSearching;

    public IReadOnlyCollection<Guid> ChangedBookIds => changedBookIds.ToArray();
    public bool CanBrowse => !disposed && !IsBusy;
    public bool CanRename => CanBrowse && TagId is not null && !string.IsNullOrEmpty(Name) && TotalBooks > 0;
    public bool IsWorking => IsBusy || IsSearching;
    public bool HasMoreTags => CanBrowse && !IsSearching && Tags.Count < TotalTags;
    public bool HasMoreBooks => CanBrowse && Books.Count < TotalBooks;

    public Task SearchAsync(string text, bool append = false) => CanBrowse ? SearchCoreAsync(text, append) : Task.CompletedTask;

    private async Task<bool> SearchCoreAsync(string text, bool append = false)
    {
        var version = ++searchVersion;
        Query = text;
        IsSearching = true;
        try
        {
            var page = await Task.Run(() => repository.SearchAsync(text, append ? Tags.Count : 0, 200, lifetime.Token), lifetime.Token);
            if (disposed || version != searchVersion) return false;
            Tags = append ? Tags.Concat(page.Tags).DistinctBy(tag => tag.Id).ToArray() : page.Tags;
            TotalTags = page.TotalCount;
            OnPropertyChanged(nameof(HasMoreTags));
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception) { if (!disposed && version == searchVersion) SetStatus("TagLoadFailed", true); return false; }
        finally { if (!disposed && version == searchVersion) IsSearching = false; }
    }

    public async Task SelectAsync(TagSummary selected)
    {
        if (!CanBrowse) return;
        IsBusy = true;
        TagId = selected.Id;
        Name = selected.Name;
        Books = [];
        TotalBooks = 0;
        Warnings = [];
        SetStatus("", false);
        try
        {
            var page = await Task.Run(() => repository.ListBooksAsync(selected.Id, 0, 200, lifetime.Token), lifetime.Token);
            if (disposed) return;
            Books = page.Books;
            TotalBooks = page.TotalCount;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed) SetStatus("TagLoadFailed", true); }
        finally { if (!disposed) { IsBusy = false; NotifySelection(); } }
    }

    public async Task LoadMoreBooksAsync()
    {
        if (!HasMoreBooks) return;
        IsBusy = true;
        try
        {
            var page = await Task.Run(() => repository.ListBooksAsync(TagId!.Value, Books.Count, 200, lifetime.Token), lifetime.Token);
            if (disposed) return;
            Books = Books.Concat(page.Books).DistinctBy(book => book.Id).ToArray();
            TotalBooks = page.TotalCount;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!disposed) SetStatus("TagLoadFailed", true); }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<TagRenamePreview?> PreviewRenameAsync()
    {
        if (!CanRename) return null;
        IsBusy = true;
        try
        {
            var preview = await Task.Run(() => renameService.PreviewAsync(TagId!.Value, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview.Books.Count == 0 || preview.TagId != TagId || preview.Name != Name)
            { SetStatus("TagRenameConflict", true); return null; }
            return preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("TagLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }

    public async Task<bool> RenameAsync(TagRenamePreview preview, string newName)
    {
        if (!CanRename || preview.TagId != TagId || preview.Name != Name) return false;
        IsBusy = true;
        ++searchVersion;
        IsSearching = false;
        Warnings = [];
        SetStatus("TagRenameWorking", false);
        var committed = false;
        try
        {
            var result = await Task.Run(() => renameService.RenameAsync(preview, newName, lifetime.Token), lifetime.Token);
            committed = result.Change.Status == TagRenameStatus.Renamed;
            if (committed) changedBookIds.UnionWith(result.Change.Books.Select(book => book.Id));
            if (disposed) return false;
            if (result.Change.Status != TagRenameStatus.Renamed)
            {
                SetStatus(result.Change.Status switch
                {
                    TagRenameStatus.NameExists => "TagRenameNameExists",
                    TagRenameStatus.InvalidName => "TagRenameInvalid",
                    TagRenameStatus.Unchanged => "TagRenameUnchanged",
                    _ => "TagRenameConflict"
                }, result.Change.Status != TagRenameStatus.Unchanged);
                return false;
            }
            Name = result.Change.Name;
            TotalBooks = result.Change.Books.Count;
            // Only the name changed; keep the already sorted book pages and their scroll position.
            Warnings = result.Warnings;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, localize(Warnings.Count == 0 ? "TagRenameSaved" : "TagRenameSavedWithWarnings"), TotalBooks, Warnings.Count);
            IsError = Warnings.Count > 0;
            if (!await SearchCoreAsync(Name) && !disposed) SetStatus("TagRenameRefreshFailed", true);
            NotifySelection();
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception)
        {
            if (!disposed) SetStatus(committed ? "TagRenameRefreshFailed" : "TagRenameFailed", true);
            return committed;
        }
        finally { if (!disposed) IsBusy = false; }
    }

    private void SetStatus(string key, bool error) { StatusMessage = key.Length == 0 ? "" : localize(key); IsError = error; }
    private void NotifySelection()
    {
        OnPropertyChanged(nameof(CanRename));
        OnPropertyChanged(nameof(HasMoreBooks));
        OnPropertyChanged(nameof(Name));
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ++searchVersion;
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
