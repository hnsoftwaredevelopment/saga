using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class SeriesNumberWorkflowTests
{
    [Fact]
    public async Task Save_preserves_more_than_500_already_loaded_books_using_bounded_pages()
    {
        var store = new Store(600); using var vm = new SeriesManagementViewModel(store, store, key => key, numberService: store);
        await vm.SelectAsync(new("Series", 600)); await vm.LoadMoreBooksAsync(); await vm.LoadMoreBooksAsync();
        vm.Books.Should().HaveCount(600); vm.SelectedBook = vm.Books.Last();
        var preview = (await vm.PreviewNumberAsync())!;
        (await vm.ChangeNumberAsync(preview, 0.5m)).Should().BeTrue();
        vm.IsError.Should().BeFalse(); vm.Books.Should().HaveCount(600); vm.Books.First().Id.Should().Be(preview.Book.Id);
        vm.SelectedBook!.Id.Should().Be(preview.Book.Id); vm.HasMoreBooks.Should().BeFalse();
    }

    [Fact]
    public async Task Save_reorders_loaded_rows_keeps_selection_query_and_reports_only_changed_book()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, numberService: store);
        await vm.SearchAsync("Series"); await vm.SelectAsync(new("Series", 205));
        vm.CanEditNumber.Should().BeFalse();
        vm.SelectedBook = vm.Books[1]; var id = vm.SelectedBook.Id;
        var preview = (await vm.PreviewNumberAsync())!;
        (await vm.ChangeNumberAsync(preview, 0.5m)).Should().BeTrue();
        vm.SelectedBook!.Id.Should().Be(id); vm.Books.First().Id.Should().Be(id);
        vm.Books.Should().HaveCount(200); vm.TotalBooks.Should().Be(205); vm.HasMoreBooks.Should().BeTrue();
        vm.Query.Should().Be("Series"); vm.Name.Should().Be("Series");
        vm.ChangedBookIds.Should().Equal(id); vm.StatusMessage.Should().Be("SeriesNumberSaved"); vm.IsError.Should().BeFalse();
        (await vm.ChangeNumberAsync(preview, 4)).Should().BeFalse(); store.Saves.Should().Be(1);
    }

    [Fact]
    public async Task A_number_moving_outside_loaded_rows_does_not_load_the_entire_series_or_keep_hidden_selection()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, numberService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SelectedBook = vm.Books.First();
        var id = vm.SelectedBook.Id;
        var preview = (await vm.PreviewNumberAsync())!;
        (await vm.ChangeNumberAsync(preview, 999)).Should().BeTrue();
        vm.Books.Should().HaveCount(200); vm.SelectedBook.Should().BeNull(); vm.CanEditNumber.Should().BeFalse();
        vm.StatusMessage.Should().Contain("SeriesNumberOutsidePage"); vm.IsError.Should().BeFalse();
        await vm.LoadMoreBooksAsync();
        vm.Books.Should().HaveCount(205); vm.Books.Last().Id.Should().Be(id);
    }

    [Fact]
    public async Task Reload_failure_after_commit_preserves_warning_and_changed_id_and_says_saved()
    {
        var store = new Store { FailReload = true, Warning = true };
        using var vm = new SeriesManagementViewModel(store, store, key => key, numberService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SelectedBook = vm.Books.First();
        var preview = (await vm.PreviewNumberAsync())!;
        (await vm.ChangeNumberAsync(preview, null)).Should().BeTrue();
        vm.ChangedBookIds.Should().Equal(preview.Book.Id); vm.Warnings.Single().Id.Should().Be(preview.Book.Id);
        vm.StatusMessage.Should().Be("SeriesNumberRefreshFailed"); vm.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Only_the_current_explicit_preview_can_change_a_loaded_selected_book()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, numberService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SelectedBook = vm.Books.First();
        var preview = (await vm.PreviewNumberAsync())!;
        (await vm.ChangeNumberAsync(preview with { }, 3)).Should().BeFalse();
        vm.SelectedBook = vm.Books[1];
        (await vm.ChangeNumberAsync(preview, 3)).Should().BeFalse(); store.Saves.Should().Be(0);
        vm.SelectedBook = new(Guid.NewGuid(), "Hidden", "Author", 1); vm.CanEditNumber.Should().BeFalse();
    }

    [Fact]
    public async Task Disposal_after_commit_still_keeps_metadata_reconciliation_id()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, numberService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SelectedBook = vm.Books.First();
        var preview = (await vm.PreviewNumberAsync())!;
        store.OnSave = vm.Dispose;
        (await vm.ChangeNumberAsync(preview, 3)).Should().BeTrue(); vm.ChangedBookIds.Should().Equal(preview.Book.Id);
    }

    [Fact]
    public async Task Conflict_does_not_report_saved_books_or_discard_the_selection()
    {
        var store = new Store { Conflict = true }; using var vm = new SeriesManagementViewModel(store, store, key => key, numberService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SelectedBook = vm.Books.First();
        var preview = (await vm.PreviewNumberAsync())!;
        (await vm.ChangeNumberAsync(preview, 3)).Should().BeFalse();
        vm.ChangedBookIds.Should().BeEmpty(); vm.SelectedBook!.Id.Should().Be(preview.Book.Id);
        vm.StatusMessage.Should().Be("SeriesNumberConflict");
    }

    private sealed class Store(int count = 205) : ISeriesRepository, ISeriesRenameService, ISeriesNumberService
    {
        private readonly List<SeriesBook> rows = Enumerable.Range(1, count).Select(number => new SeriesBook(Guid.NewGuid(), $"Book {number}", "Author", number)).ToList();
        public int Saves;
        public bool FailReload, Warning, Conflict;
        public Action? OnSave;
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesPage([new("Series", rows.Count)], 1));
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token)
        {
            if (take > 500) throw new ArgumentOutOfRangeException(nameof(take));
            if (FailReload && Saves > 0) throw new IOException("Simulated reload failure");
            return Task.FromResult(new SeriesBookPage(rows.OrderBy(row => row.Number is null).ThenBy(row => row.Number).Skip(skip).Take(take).ToArray(), rows.Count));
        }
        public Task<SeriesNumberPreview?> PreviewAsync(Guid id, CancellationToken token) => Task.FromResult<SeriesNumberPreview?>(new("Series", rows.Single(row => row.Id == id), rows.Where(row => row.Id != id && row.Number.HasValue).Select(row => row.Number!.Value).ToArray()));
        public Task<SeriesNumberCompletion> ChangeAsync(SeriesNumberPreview preview, decimal? number, CancellationToken token)
        {
            Saves++;
            if (!Conflict) { var index = rows.FindIndex(row => row.Id == preview.Book.Id); rows[index] = rows[index] with { Number = number }; }
            OnSave?.Invoke();
            return Task.FromResult(new SeriesNumberCompletion(new(Conflict ? SeriesNumberStatus.Conflict : SeriesNumberStatus.Changed,
                number.HasValue ? rows.Count(row => row.Id != preview.Book.Id && row.Number == number) : 0), Warning));
        }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}
