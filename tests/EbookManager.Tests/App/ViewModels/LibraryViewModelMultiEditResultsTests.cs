using EbookManager.Domain.Books;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed partial class LibraryViewModelTests
{
    [Fact]
    public async Task Metadata_multi_edit_general_error_keeps_failed_book_selected_and_updates_later_books()
    {
        var failed = CreateBook("First", ["Author"]);
        var success = CreateBook("Second", ["Author"]);
        var repository = new MultiEditFailingRepository([failed, success], failed.Id);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new(false, "", true, "New Series", false, "", false, "", false, ReadingStatus.Unread)
        };
        var vm = CreateViewModel([failed, success], interaction, repository: repository);
        await vm.RefreshAsync();
        vm.SetSelectedBooks(vm.VisibleBooks);

        await vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.BooksSnapshot.Single(book => book.Id == success.Id).Metadata.Series.Should().Be("New Series");
        vm.SelectedBooks.Should().ContainSingle().Which.Id.Should().Be(failed.Id);
        vm.IsCleaningMetadata.Should().BeFalse();
    }

    private sealed class MultiEditFailingRepository(IReadOnlyList<Book> initialBooks, Guid failingId) : StaticBookRepository(initialBooks)
    {
        public override Task UpdateAsync(Book book, CancellationToken token) => book.Id == failingId
            ? Task.FromException(new IOException("Do not display this technical failure")) : base.UpdateAsync(book, token);
    }
}
