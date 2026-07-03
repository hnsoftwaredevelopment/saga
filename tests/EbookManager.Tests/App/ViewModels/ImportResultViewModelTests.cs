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
}
