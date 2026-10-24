using EbookManager.Domain.Books;
using EbookManager.Application.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed partial class LibraryViewModelTests
{
    [Fact]
    public async Task MultiEdit_cancellation_before_database_commit_marks_remaining_books_unprocessed()
    {
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        using var cancelled = new CancellationTokenSource();
        var repository = new MultiEditCancelledRepository([first, second], cancelled);
        var results = await new MetadataMultiEditService(repository, null).ApplyAsync([first.Id, second.Id],
            new(false, "", true, "Changed", false, "", false, "", false, ReadingStatus.Unread), cancelled.Token);

        results.Should().OnlyContain(item => item.Status == MultiEditStatus.NotProcessed);
        repository.BooksSnapshot.Should().OnlyContain(book => book.Metadata.Series == null);
    }

    private sealed class MultiEditCancelledRepository(IReadOnlyList<Book> initialBooks, CancellationTokenSource cancellation) : StaticBookRepository(initialBooks)
    {
        public override Task UpdateAsync(Book book, CancellationToken token)
        {
            cancellation.Cancel();
            return Task.FromCanceled(token);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultiEdit_handles_an_exception_after_database_commit_without_replaying_the_edit(bool failReadBack)
    {
        var book = CreateBook("Title", ["Author"]);
        var repository = new MultiEditAfterCommitRepository([book], failReadBack);
        var service = new MetadataMultiEditService(repository, null);
        var result = (await service.ApplyAsync([book.Id], new(false, "", false, "", false, "", false, "", false,
            ReadingStatus.Unread, SwapTitleAndAuthors: true), default)).Single();

        result.Status.Should().Be(failReadBack ? MultiEditStatus.Partial : MultiEditStatus.Updated);
        repository.UpdateCalls.Should().Be(1);
        repository.BooksSnapshot.Single().Metadata.Title.Should().Be("Author");
        repository.BooksSnapshot.Single().Metadata.Authors.Should().Equal("Title");
    }

    private sealed class MultiEditAfterCommitRepository(IReadOnlyList<Book> initialBooks, bool failReadBack) : StaticBookRepository(initialBooks)
    {
        private bool attempted;
        public override async Task UpdateAsync(Book book, CancellationToken token)
        {
            await base.UpdateAsync(book, token);
            attempted = true;
            throw new IOException("Failure after commit");
        }
        public override Task<Book?> GetAsync(Guid id, CancellationToken token) => attempted && failReadBack
            ? Task.FromException<Book?>(new IOException("Could not confirm storage")) : base.GetAsync(id, token);
    }

    [Fact]
    public async Task Metadata_multi_edit_keeps_a_newer_selection_and_blocks_second_commands_while_saving()
    {
        var first = CreateBook("First", ["Author"]);
        var other = CreateBook("Other", ["Author"]);
        var repository = new MultiEditGatedRepository([first, other]);
        var interaction = new ScriptedUserInteractionService { MetadataMultiEditResult = new(false, "", true, "New Series", false, "", false, "", false, ReadingStatus.Unread) };
        var vm = CreateViewModel([first, other], interaction, repository: repository);
        await vm.RefreshAsync();
        vm.SetSelectedBooks(vm.VisibleBooks.Where(row => row.Id == first.Id));

        var saving = vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);
        await repository.Started.Task;
        vm.CanMultiEditSelectedBooks.Should().BeFalse();
        vm.OpenLibraryCommand.CanExecute(null).Should().BeFalse();
        vm.SetSelectedBooks(vm.VisibleBooks.Where(row => row.Id == other.Id));
        repository.Continue.SetResult();
        await saving;

        vm.SelectedBooks.Should().ContainSingle().Which.Id.Should().Be(other.Id);
        vm.MultiEditDetails.Should().BeEmpty();
        vm.MultiEditIsError.Should().BeFalse();
        repository.UpdateCalls.Should().Be(1);
    }

    private sealed class MultiEditGatedRepository(IReadOnlyList<Book> initialBooks) : StaticBookRepository(initialBooks)
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task UpdateAsync(Book book, CancellationToken token)
        {
            Started.SetResult();
            await Continue.Task;
            await base.UpdateAsync(book, token);
        }
    }

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
