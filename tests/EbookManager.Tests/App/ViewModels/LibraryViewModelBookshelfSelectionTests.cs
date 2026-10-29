using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed partial class LibraryViewModelTests
{
    [Fact]
    public async Task Bookshelf_bulk_edit_updates_only_selected_books_and_clears_successful_tile_selection()
    {
        var books = new[] { CreateBook("One", ["A"]), CreateBook("Two", ["B"]), CreateBook("Three", ["C"]) };
        var repository = new StaticBookRepository(books);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new(false, "", true, "New Series", false, "", false, "", false, EbookManager.Domain.Books.ReadingStatus.Unread)
        };
        var vm = CreateViewModel(books, interaction, repository: repository);
        await vm.RefreshAsync(); vm.SelectedView = LibraryView.Bookshelf;
        vm.SelectBookshelfBook(vm.VisibleBooks.Single(row => row.Id == books[0].Id), false, false);
        vm.SelectBookshelfBook(vm.VisibleBooks.Single(row => row.Id == books[1].Id), true, false);
        await vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);
        repository.BooksSnapshot.Where(book => book.Id != books[2].Id).Should().OnlyContain(book => book.Metadata.Series == "New Series");
        repository.BooksSnapshot.Single(book => book.Id == books[2].Id).Metadata.Series.Should().BeNull();
        vm.SelectedBookCount.Should().Be(0);
        vm.VisibleBooks.Should().OnlyContain(row => !row.IsSelected);
    }

    [Fact]
    public async Task Bookshelf_groups_use_expanded_unique_order_and_shared_selection_flags()
    {
        var one = CreateBook("One", ["A"], ["First", "Second"]);
        var two = CreateBook("Two", ["A"], ["Second"]);
        var hidden = CreateBook("Hidden", ["A"], ["Third"]);
        var vm = CreateViewModel([one, two, hidden]);
        await vm.RefreshAsync();
        vm.SelectedView = LibraryView.Bookshelf;
        vm.SetGroupingOptions([LibraryGroupOption.Tag]);
        foreach (var group in vm.GroupedLibraryNodes) group.IsExpanded = group.Header != "Third";
        vm.GetBookshelfSelectionOrder().Select(row => row.Id).Should().Equal(one.Id, two.Id);
        var first = vm.VisibleBooks.Single(row => row.Id == one.Id);
        var second = vm.VisibleBooks.Single(row => row.Id == two.Id);
        vm.SelectBookshelfBook(first, false, false);
        vm.SelectBookshelfBook(second, false, true);
        vm.SelectedBooks.Select(row => row.Id).Should().BeEquivalentTo([one.Id, two.Id]);
        vm.GroupedLibraryNodes.SelectMany(group => group.Books).Where(row => row.Id == one.Id).Should().OnlyContain(row => row.IsSelected);
        vm.SelectBookshelfBook(first, true, false);
        vm.SelectedBooks.Single().Id.Should().Be(two.Id);
    }

    [Fact]
    public async Task Bookshelf_selection_survives_view_switch_and_cancelled_bulk_edit_then_clears_on_library_change()
    {
        var vm = CreateViewModel([CreateBook("One", ["A"]), CreateBook("Two", ["B"])]);
        await vm.RefreshAsync();
        vm.SelectedView = LibraryView.Bookshelf;
        vm.SelectBookshelfBook(vm.VisibleBooks[0], false, false);
        vm.SelectBookshelfBook(vm.VisibleBooks[1], true, false);
        vm.SelectedView = LibraryView.List;
        vm.SelectedBookCount.Should().Be(2);
        vm.SelectedView = LibraryView.Bookshelf;
        await vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);
        vm.SelectedBookCount.Should().Be(2);
        vm.CurrentLibraryPath = "different-library";
        vm.SelectedBooks.Should().BeEmpty();
        vm.VisibleBooks.Should().OnlyContain(row => !row.IsSelected);
    }

    [Fact]
    public async Task Selection_flags_follow_central_selection_and_filter_intersection()
    {
        var vm = CreateViewModel([CreateBook("One", ["A"]), CreateBook("Two", ["B"])]);
        await vm.RefreshAsync();
        var rows = vm.VisibleBooks.ToArray();
        vm.SetSelectedBooks(rows);
        rows.Should().OnlyContain(row => row.IsSelected);
        vm.SearchText = "One";
        vm.SelectedBooks.Should().ContainSingle().Which.Id.Should().Be(rows[0].Id);
        rows[1].IsSelected.Should().BeFalse();
        vm.SearchText = "";
        vm.SelectedBookCount.Should().Be(1);
        vm.SetSelectedBooks([]);
        rows.Should().OnlyContain(row => !row.IsSelected);
    }

    [Fact]
    public async Task Recreated_selected_row_replaces_old_reference_and_updates_its_visual_state()
    {
        var vm = CreateViewModel([CreateBook("One", ["A"])]);
        await vm.RefreshAsync();
        var original = vm.VisibleBooks.Single();
        vm.SetSelectedBooks([original]);
        var replacement = new BookRowViewModel(original.Book);
        vm.VisibleBooks[0] = replacement;
        vm.SetSelectedBooks([replacement]);
        vm.SelectedBooks.Single().Should().BeSameAs(replacement);
        original.IsSelected.Should().BeFalse();
        replacement.IsSelected.Should().BeTrue();
    }
}
