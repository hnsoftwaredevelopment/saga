using EbookManager.Domain.Importing;
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
}
