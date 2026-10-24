using EbookManager.Domain.Books;
using EbookManager.Application.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed partial class LibraryViewModelTests
{
    [Fact]
    public async Task MultiEdit_unexpected_batch_exception_does_not_claim_that_no_changes_were_saved()
    {
        var first = CreateBook("First", ["Author"]);
        var broken = CreateBook("Second", ["Author"]);
        var repository = new MultiEditInvalidResultRepository([first, broken], broken.Id);
        var interaction = new ScriptedUserInteractionService { MetadataMultiEditResult = new(false, "", true, "Saved Series", false, "", false, "", false, ReadingStatus.Unread) };
        var vm = CreateViewModel([first, broken], interaction, repository: repository);
        await vm.RefreshAsync();
        vm.SetSelectedBooks(vm.VisibleBooks);

        await vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.BooksSnapshot.Single(book => book.Id == first.Id).Metadata.Series.Should().Be("Saved Series");
        repository.UpdateCalls.Should().Be(1);
        vm.MultiEditMessage.Should().Be("MultiEditUncertain");
        vm.MultiEditIsError.Should().BeTrue();
        vm.IsCleaningMetadata.Should().BeFalse();
    }

    private sealed class MultiEditInvalidResultRepository(IReadOnlyList<Book> initialBooks, Guid brokenId) : StaticBookRepository(initialBooks)
    {
        public override async Task<Book?> GetAsync(Guid id, CancellationToken token)
        {
            var book = await base.GetAsync(id, token);
            // Simulate a provider contract failure only after the first book has committed.
            return id == brokenId && UpdateCalls > 0 ? book! with { Metadata = null! } : book;
        }
    }

    [Fact]
    public async Task MultiEdit_availability_notifies_when_another_cleanup_starts_and_finishes()
    {
        var book = CreateBook("Book", ["Author"]);
        var vm = CreateViewModel([book]);
        await vm.RefreshAsync();
        vm.SetSelectedBooks(vm.VisibleBooks);
        var properties = new List<string?>();
        var commandChanges = 0;
        vm.PropertyChanged += (_, args) => properties.Add(args.PropertyName);
        vm.ShowMetadataMultiEditCommand.CanExecuteChanged += (_, _) => commandChanges++;

        vm.IsCleaningMetadata = true;
        vm.ShowMetadataMultiEditCommand.CanExecute(null).Should().BeFalse();
        properties.Should().Contain(nameof(vm.CanMultiEditSelectedBooks));
        commandChanges.Should().Be(1);
        properties.Clear();
        vm.IsCleaningMetadata = false;
        vm.ShowMetadataMultiEditCommand.CanExecute(null).Should().BeTrue();
        properties.Should().Contain(nameof(vm.CanMultiEditSelectedBooks));
        commandChanges.Should().Be(2);
    }

    [Fact]
    public async Task MultiEdit_cancelling_the_dialog_preserves_previous_feedback_and_book_values()
    {
        var book = CreateBook("Book", ["Author"]);
        var repository = new MultiEditFailingRepository([book], book.Id);
        var interaction = new ScriptedUserInteractionService { MetadataMultiEditResult = new(false, "", true, "New Series", false, "", false, "", false, ReadingStatus.Unread) };
        var vm = CreateViewModel([book], interaction, repository: repository);
        await vm.RefreshAsync();
        vm.SetSelectedBooks(vm.VisibleBooks);
        await vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);
        var previousMessage = vm.MultiEditMessage;
        var previousDetails = vm.MultiEditDetails.ToArray();
        previousDetails.Should().ContainSingle();
        interaction.MetadataMultiEditResult = null;

        await vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        vm.MultiEditMessage.Should().Be(previousMessage);
        vm.MultiEditDetails.Should().Equal(previousDetails);
        vm.MultiEditIsError.Should().BeTrue();
        repository.BooksSnapshot.Single().Metadata.Series.Should().BeNull();
    }

    [Fact]
    public async Task MultiEdit_reports_refresh_failure_after_storage_without_claiming_save_failure()
    {
        var book = CreateBook("Book", ["Author"]);
        var repository = new StaticBookRepository([book]);
        var interaction = new ScriptedUserInteractionService { MetadataMultiEditResult = new(false, "", true, "Saved Series", false, "", false, "", false, ReadingStatus.Unread) };
        var vm = CreateViewModel([book], interaction, repository: repository);
        await vm.RefreshAsync();
        vm.SetSelectedBooks(vm.VisibleBooks);
        var failRefresh = true;
        vm.VisibleBooks.CollectionChanged += (_, _) =>
        {
            if (!failRefresh) return;
            failRefresh = false;
            throw new InvalidOperationException("Technical UI refresh failure");
        };

        await vm.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.BooksSnapshot.Single().Metadata.Series.Should().Be("Saved Series");
        repository.UpdateCalls.Should().Be(1);
        vm.MultiEditMessage.Should().Be("MultiEditRefreshFailed");
        vm.MultiEditIsError.Should().BeTrue();
        vm.IsCleaningMetadata.Should().BeFalse();
    }

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
        try
        {
            await repository.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            vm.CanMultiEditSelectedBooks.Should().BeFalse();
            vm.OpenLibraryCommand.CanExecute(null).Should().BeFalse();
            vm.SetSelectedBooks(vm.VisibleBooks.Where(row => row.Id == other.Id));
        }
        finally { repository.Continue.TrySetResult(); }
        await saving.WaitAsync(TimeSpan.FromSeconds(5));

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
        vm.MultiEditDetails.Should().ContainSingle().Which.BookId.Should().Be(failed.Id);
        vm.MultiEditDetails.Single().Reason.Should().Be("MultiEditSaveFailed");
        vm.MultiEditDetails.Single().Reason.Should().NotContain("Do not display this technical failure");
        vm.MultiEditMessage.Should().NotBeNull().And.NotContain("Do not display this technical failure");
    }

    private sealed class MultiEditFailingRepository(IReadOnlyList<Book> initialBooks, Guid failingId) : StaticBookRepository(initialBooks)
    {
        public override Task UpdateAsync(Book book, CancellationToken token) => book.Id == failingId
            ? Task.FromException(new IOException("Do not display this technical failure")) : base.UpdateAsync(book, token);
    }
}
