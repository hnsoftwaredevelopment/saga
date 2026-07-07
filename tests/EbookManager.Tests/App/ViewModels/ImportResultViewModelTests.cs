using EbookManager.Domain.Importing;
using EbookManager.Domain.Books;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class ImportResultViewModelTests
{
    [Fact]
    public void Summary_counts_added_skipped_failed_and_problem_state()
    {
        var viewModel = new ImportResultViewModel(new ImportBatchResult(
            Guid.NewGuid(),
            [
                new ImportItemResult("a.epub", ImportOutcome.Added, "added"),
                new ImportItemResult("b.epub", ImportOutcome.ExactDuplicate, "duplicate"),
                new ImportItemResult("c.epub", ImportOutcome.PossibleDuplicate, "possible"),
                new ImportItemResult("d.epub", ImportOutcome.Failed, "failed")
            ]));

        viewModel.TotalCount.Should().Be(4);
        viewModel.SkippedCount.Should().Be(2);
        viewModel.HasProblems.Should().BeTrue();
        viewModel.SummaryText.Should().Be("4 files processed: 1 added, 2 skipped, 1 failed.");
    }

    [Fact]
    public void Items_format_import_diagnostics_for_display()
    {
        var viewModel = new ImportResultViewModel(new ImportBatchResult(
            Guid.NewGuid(),
            [
                new ImportItemResult(
                    "comic.cbr",
                    ImportOutcome.Added,
                    "added",
                    Diagnostics: new ImportItemDiagnostics(
                        TimeSpan.FromMilliseconds(1234),
                        SizeBytes: 1_572_864,
                        Format: EbookFormat.Cbr))
            ]));

        var item = viewModel.Items.Should().ContainSingle().Which;
        item.FormatText.Should().Be("CBR");
        item.SizeText.Should().Be("1,5 MB");
        item.DurationText.Should().Be("1,2 s");
    }

    [Fact]
    public void Visible_items_can_be_filtered_by_search_text_and_outcome()
    {
        var viewModel = new ImportResultViewModel(new ImportBatchResult(
            Guid.NewGuid(),
            [
                new ImportItemResult(
                    "fast.epub",
                    ImportOutcome.Added,
                    "added",
                    Diagnostics: new ImportItemDiagnostics(TimeSpan.FromMilliseconds(25), 100, EbookFormat.Epub)),
                new ImportItemResult(
                    "slow-comic.cbr",
                    ImportOutcome.Failed,
                    "source unreadable",
                    Diagnostics: new ImportItemDiagnostics(TimeSpan.FromSeconds(2), 200, EbookFormat.Cbr))
            ]));

        viewModel.SearchText = "CBR";
        viewModel.VisibleItems.Should().ContainSingle()
            .Which.FileName.Should().Be("slow-comic.cbr");

        viewModel.SearchText = "2,0 s";
        viewModel.VisibleItems.Should().ContainSingle()
            .Which.FileName.Should().Be("slow-comic.cbr");

        viewModel.SearchText = string.Empty;
        viewModel.SelectedOutcomeFilter = ImportResultOutcomeFilter.Failed;
        viewModel.VisibleItems.Should().ContainSingle()
            .Which.FileName.Should().Be("slow-comic.cbr");
    }

    [Fact]
    public async Task RetryFailedCommand_retries_only_failed_items_with_existing_source_paths()
    {
        var retryablePath = Path.GetTempFileName();
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.epub");
        IReadOnlyList<string>? retriedPaths = null;

        try
        {
            var viewModel = new ImportResultViewModel(
                new ImportBatchResult(
                    Guid.NewGuid(),
                    [
                        new ImportItemResult(retryablePath, ImportOutcome.Failed, "source unreadable"),
                        new ImportItemResult(missingPath, ImportOutcome.Failed, "source unreadable"),
                        new ImportItemResult("display-only.epub", ImportOutcome.Failed, "source unreadable"),
                        new ImportItemResult(retryablePath, ImportOutcome.Added, "added")
                    ]),
                (paths, _) =>
                {
                    retriedPaths = paths;
                    return Task.CompletedTask;
                });

            viewModel.RetryFailedCount.Should().Be(1);
            viewModel.RetryFailedCommand.CanExecute(null).Should().BeTrue();

            await viewModel.RetryFailedCommand.ExecuteAsync(null);

            retriedPaths.Should().Equal(retryablePath);
        }
        finally
        {
            File.Delete(retryablePath);
        }
    }

    [Fact]
    public async Task LinkSuggestionCommand_links_added_item_to_suggested_book_once()
    {
        var importedBookId = Guid.NewGuid();
        var targetBookId = Guid.NewGuid();
        (Guid SourceBookId, Guid TargetBookId)? linked = null;
        var viewModel = new ImportResultViewModel(
            new ImportBatchResult(
                Guid.NewGuid(),
                [
                    new ImportItemResult(
                        "Pro Git.pdf",
                        ImportOutcome.Added,
                        "added; possible title match: Pro Git",
                        importedBookId,
                        Suggestion: new ImportItemSuggestion(
                            ImportItemSuggestionKind.TitleMatch,
                            targetBookId,
                            "Pro Git",
                            "Scott Chacon; Ben Straub"))
                ]),
            linkSuggestionAsync: (sourceBookId, suggestedBookId, _) =>
            {
                linked = (sourceBookId, suggestedBookId);
                return Task.CompletedTask;
            });
        var item = viewModel.Items.Should().ContainSingle().Which;

        item.SuggestionText.Should().Be("Pro Git - Scott Chacon; Ben Straub");
        item.LinkSuggestionLabel.Should().Be("Link");
        item.CanLinkSuggestion.Should().BeTrue();
        item.LinkSuggestionCommand.CanExecute(null).Should().BeTrue();

        await item.LinkSuggestionCommand.ExecuteAsync(null);

        linked.Should().Be((importedBookId, targetBookId));
        item.LinkSuggestionLabel.Should().Be("Linked");
        item.CanLinkSuggestion.Should().BeFalse();
        item.LinkSuggestionCommand.CanExecute(null).Should().BeFalse();
    }
}
