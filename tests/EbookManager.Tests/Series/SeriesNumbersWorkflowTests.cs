using EbookManager.Application.Series;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Series;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.Series;

public sealed class SeriesNumbersWorkflowTests
{
    [Fact]
    public async Task Loaded_selection_survives_paging_and_save_reorders_only_selected_books()
    {
        var store = new Store(600); using var vm = new SeriesManagementViewModel(store, store, key => key, numbersService: store);
        await vm.SelectAsync(new("Series", 600)); vm.SetNumberSelection(vm.Books.Take(2)); await vm.LoadMoreBooksAsync(); await vm.LoadMoreBooksAsync();
        vm.SelectedNumberBooks.Should().HaveCount(2); vm.SetNumberSelection([vm.Books.First(), vm.Books.Last()]);
        var ids = vm.SelectedNumberBooks.Select(row => row.Id).ToArray(); var preview = (await vm.PreviewNumbersAsync())!;
        (await vm.AssignNumbersAsync(preview, [new(ids[0], -1), new(ids[1], -2)])).Should().BeTrue();
        vm.Books.Should().HaveCount(600); vm.Books.First().Id.Should().Be(ids[1]); vm.SelectedNumberBooks.Select(row => row.Id).Should().BeEquivalentTo(ids);
        vm.ChangedBookIds.Should().BeEquivalentTo(ids); vm.Name.Should().Be("Series"); vm.TotalBooks.Should().Be(600); vm.IsError.Should().BeFalse();
        (await vm.AssignNumbersAsync(preview, [new(ids[0], 1), new(ids[1], 2)])).Should().BeFalse(); store.Saves.Should().Be(1);
    }
    [Fact]
    public async Task Changed_selection_invalidates_preview_and_changing_series_clears_selection()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, numbersService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SetNumberSelection(vm.Books.Take(2)); var preview = (await vm.PreviewNumbersAsync())!;
        vm.SetNumberSelection(vm.Books.Skip(1).Take(2));
        (await vm.AssignNumbersAsync(preview, [])).Should().BeFalse(); store.Saves.Should().Be(0);
        await vm.SelectAsync(new("Series", 205)); vm.SelectedNumberBooks.Should().BeEmpty(); vm.CanAssignNumbers.Should().BeFalse();
    }
    [Fact]
    public async Task Conflict_preserves_selection_and_reports_no_changed_ids()
    {
        var store = new Store { Conflict = true }; using var vm = new SeriesManagementViewModel(store, store, key => key, numbersService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SetNumberSelection(vm.Books.Take(2)); var preview = (await vm.PreviewNumbersAsync())!;
        (await vm.AssignNumbersAsync(preview, preview.Books.Select((row,i) => new SeriesNumberAssignment(row.Book.Id, i)).ToArray())).Should().BeFalse();
        vm.ChangedBookIds.Should().BeEmpty(); vm.SelectedNumberBooks.Should().HaveCount(2); vm.StatusMessage.Should().Be("SeriesNumbersConflict");
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Stored_numbers_keep_changed_ids_and_file_warnings_when_refresh_fails(bool refreshFails)
    {
        var store = new Store { FailRefresh = refreshFails, Warning = true }; using var vm = new SeriesManagementViewModel(store, store, key => key, numbersService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SetNumberSelection(vm.Books.Take(2)); var preview = (await vm.PreviewNumbersAsync())!;
        (await vm.AssignNumbersAsync(preview, preview.Books.Select((row,i) => new SeriesNumberAssignment(row.Book.Id, i)).ToArray())).Should().BeTrue();
        vm.ChangedBookIds.Should().HaveCount(2); vm.Warnings.Should().HaveCount(1); vm.IsError.Should().BeTrue();
        vm.StatusMessage.Should().Contain(refreshFails ? "SeriesNumbersRefreshFailed" : "SeriesNumbersWarnings");
    }
    [Fact]
    public async Task Numbers_outside_loaded_prefix_clear_hidden_selection_and_explain_load_more()
    {
        var store = new Store(); using var vm = new SeriesManagementViewModel(store, store, key => key, numbersService: store);
        await vm.SelectAsync(new("Series", 205)); vm.SetNumberSelection(vm.Books.Take(2)); var preview = (await vm.PreviewNumbersAsync())!;
        (await vm.AssignNumbersAsync(preview, preview.Books.Select((row,i) => new SeriesNumberAssignment(row.Book.Id, 999 + i)).ToArray())).Should().BeTrue();
        vm.Books.Should().HaveCount(200); vm.SelectedNumberBooks.Should().BeEmpty(); vm.CanAssignNumbers.Should().BeFalse(); vm.HasMoreBooks.Should().BeTrue();
        vm.StatusMessage.Should().Contain("SeriesNumbersOutside"); vm.IsError.Should().BeFalse();
    }
    private sealed class Store(int count = 205) : ISeriesRepository, ISeriesRenameService, ISeriesNumbersService
    {
        private readonly List<SeriesBook> rows = Enumerable.Range(1,count).Select(i => new SeriesBook(Guid.NewGuid(), $"Book {i:000}", "Author", i)).ToList();
        public bool Conflict, Warning, FailRefresh; public int Saves;
        public Task<SeriesPage> SearchAsync(string query, int skip, int take, CancellationToken token) => Task.FromResult(new SeriesPage([new("Series", rows.Count)], 1));
        public Task<SeriesBookPage> ListBooksAsync(string name, int skip, int take, CancellationToken token) => FailRefresh && Saves > 0 ? throw new IOException() : Task.FromResult(new SeriesBookPage(rows.OrderBy(row => row.Number).ThenBy(row => row.Title).Skip(skip).Take(take).ToArray(), rows.Count));
        public Task<SeriesNumbersPreview?> PreviewAsync(string name, IReadOnlyList<Guid> ids, CancellationToken token) => Task.FromResult<SeriesNumbersPreview?>(new(name, ids.Select(id => new SeriesNumberSource(name, rows.Single(row => row.Id == id))).ToArray(), []));
        public Task<SeriesNumbersCompletion> ChangeAsync(SeriesNumbersPreview preview, IReadOnlyList<SeriesNumberAssignment> assignments, CancellationToken token)
        {
            Saves++;
            if (!Conflict) foreach (var item in assignments) { var index = rows.FindIndex(row => row.Id == item.Id); rows[index] = rows[index] with { Number = item.Number }; }
            var changed = preview.Books.Select(row => new SeriesSourceBook(row.Book.Id, row.StoredName, row.Book.Title)).ToArray();
            return Task.FromResult(new SeriesNumbersCompletion(new(Conflict ? SeriesNumberStatus.Conflict : SeriesNumberStatus.Changed, Conflict ? [] : changed, 0), Warning ? changed.Take(1).ToArray() : []));
        }
        public Task<SeriesRenamePreview> PreviewAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenamePreview> PreviewRenameAsync(string name, CancellationToken token) => throw new NotSupportedException();
        public Task<SeriesRenameResult> RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
        Task<SeriesRenameCompletion> ISeriesRenameService.RenameAsync(SeriesRenamePreview preview, string name, CancellationToken token) => throw new NotSupportedException();
    }
}
