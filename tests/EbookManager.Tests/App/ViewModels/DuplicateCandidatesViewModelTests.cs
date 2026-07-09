using EbookManager.Application.Books;
using EbookManager.Domain.Books;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class DuplicateCandidatesViewModelTests
{
    [Fact]
    public void Rows_flatten_groups_for_grid_display()
    {
        var first = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            series: "Midden-aarde",
            language: "nl",
            description: "Een reis door Midden-aarde.",
            coverRelativePath: "books/cover.jpg");
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: "Middle-earth", language: "eng");
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup("de hobbit:0", "De Hobbit", "J.R.R. Tolkien", [first, second])
        ]);

        var viewModel = new DuplicateCandidatesViewModel(result, "C:/Library");

        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows[0].GroupTitle.Should().Be("De Hobbit - J.R.R. Tolkien");
        viewModel.Rows[0].Title.Should().Be("De Hobbit");
        viewModel.Rows[0].Authors.Should().Be("J.R.R. Tolkien");
        viewModel.Rows[0].Series.Should().Be("Midden-aarde");
        viewModel.Rows[0].Language.Should().Be("nl");
        viewModel.Rows[0].FormatText.Should().Be("EPUB, PDF");
        viewModel.Rows[0].MatchKind.Should().Be(DuplicateCandidateMatchKind.AuthorOverlap);
        viewModel.Rows[0].Description.Should().Be("Een reis door Midden-aarde.");
        viewModel.Rows[0].CoverPath.Should().Be(Path.Combine("C:/Library", "books/cover.jpg"));
    }

    [Fact]
    public void Rows_expose_title_only_match_kind_for_display()
    {
        var first = CreateBook("De Chocoladevilla", ["Maria Nikolai"], series: null, language: "nl");
        var second = CreateBook("De chocoladevilla", ["Unknown"], series: null, language: null);
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup(
                "de chocoladevilla:title",
                "De Chocoladevilla",
                "Maria Nikolai, Unknown",
                [first, second],
                DuplicateCandidateMatchKind.TitleOnly)
        ]);

        var viewModel = new DuplicateCandidatesViewModel(result, "C:/Library");
        viewModel.ExactMatchesOnly = false;

        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows.Should().OnlyContain(row => row.MatchKind == DuplicateCandidateMatchKind.TitleOnly);
    }

    [Fact]
    public void ExactMatchesOnly_defaults_to_author_overlap_matches()
    {
        var authorMatchFirst = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var authorMatchSecond = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var titleOnlyFirst = CreateBook("De Chocoladevilla", ["Maria Nikolai"], series: null, language: null);
        var titleOnlySecond = CreateBook("De chocoladevilla", ["Unknown"], series: null, language: null);
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup(
                "de hobbit:0",
                "De Hobbit",
                "J.R.R. Tolkien",
                [authorMatchFirst, authorMatchSecond],
                DuplicateCandidateMatchKind.AuthorOverlap),
            new DuplicateCandidateGroup(
                "de chocoladevilla:title",
                "De Chocoladevilla",
                "Maria Nikolai, Unknown",
                [titleOnlyFirst, titleOnlySecond],
                DuplicateCandidateMatchKind.TitleOnly)
        ]);

        var viewModel = new DuplicateCandidatesViewModel(result, "C:/Library");

        viewModel.ExactMatchesOnly.Should().BeTrue();
        viewModel.GroupCount.Should().Be(1);
        viewModel.Rows.Select(row => row.Title).Should().BeEquivalentTo("De Hobbit", "de hobbit");
    }

    [Fact]
    public void ExactMatchesOnly_can_be_disabled_to_show_title_only_matches()
    {
        var first = CreateBook("De Chocoladevilla", ["Maria Nikolai"], series: null, language: null);
        var second = CreateBook("De chocoladevilla", ["Unknown"], series: null, language: null);
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup(
                "de chocoladevilla:title",
                "De Chocoladevilla",
                "Maria Nikolai, Unknown",
                [first, second],
                DuplicateCandidateMatchKind.TitleOnly)
        ]);
        var viewModel = new DuplicateCandidatesViewModel(result, "C:/Library");

        viewModel.Rows.Should().BeEmpty();

        viewModel.ExactMatchesOnly = false;

        viewModel.GroupCount.Should().Be(1);
        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows.Should().OnlyContain(row => row.MatchKind == DuplicateCandidateMatchKind.TitleOnly);
    }

    [Fact]
    public async Task DeleteCandidate_removes_book_and_recomputes_duplicate_groups()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var third = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var result = new DuplicateCandidateService().FindCandidates([first, second, third]);
        var deletedIds = new List<Guid>();
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            deleteCandidateAsync: (row, _) =>
            {
                deletedIds.Add(row.Id);
                return Task.FromResult(true);
            });

        await viewModel.DeleteCandidateAsync(viewModel.Rows[0], CancellationToken.None);

        deletedIds.Should().Equal(first.Id);
        viewModel.HasChanges.Should().BeTrue();
        viewModel.GroupCount.Should().Be(1);
        viewModel.BookCount.Should().Be(2);
        viewModel.Rows.Select(row => row.Id).Should().BeEquivalentTo([second.Id, third.Id]);
    }

    [Fact]
    public async Task DeleteCandidate_closes_duplicate_list_when_no_duplicate_groups_remain()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var result = new DuplicateCandidateService().FindCandidates([first, second]);
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            deleteCandidateAsync: (_, _) => Task.FromResult(true));

        await viewModel.DeleteCandidateAsync(viewModel.Rows[0], CancellationToken.None);

        viewModel.HasChanges.Should().BeTrue();
        viewModel.HasGroups.Should().BeFalse();
        viewModel.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSelectedCandidates_deletes_selected_rows_and_recomputes_duplicate_groups()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var second = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var third = CreateBook("de hobbit", ["J.R.R. Tolkien"], series: null, language: null);
        var result = new DuplicateCandidateService().FindCandidates([first, second, third]);
        var deletedIds = new List<Guid>();
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            deleteCandidateAsync: (row, _) =>
            {
                deletedIds.Add(row.Id);
                return Task.FromResult(true);
            });
        var selectedIds = viewModel.Rows.Take(2).Select(row => row.Id).ToList();
        viewModel.Rows[0].IsSelected = true;
        viewModel.Rows[1].IsSelected = true;

        await viewModel.DeleteSelectedCandidatesCommand.ExecuteAsync(null);

        deletedIds.Should().Equal(selectedIds);
        viewModel.HasChanges.Should().BeTrue();
        viewModel.HasGroups.Should().BeFalse();
        viewModel.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task MergeCandidate_merges_row_into_best_metadata_target_in_the_same_group()
    {
        var source = CreateBook("De Hobbit", ["Unknown"], series: null, language: null);
        var target = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            series: "Midden-aarde",
            language: "nl",
            description: "Een rijker gevuld basisboek.",
            coverRelativePath: "books/cover.jpg");
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup(
                "de hobbit:title",
                "De Hobbit",
                "J.R.R. Tolkien, Unknown",
                [source, target],
                DuplicateCandidateMatchKind.TitleOnly)
        ]);
        (Guid SourceBookId, Guid TargetBookId)? merged = null;
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            "C:/Library",
            mergeCandidateAsync: (sourceRow, targetRow, _, _) =>
            {
                merged = (sourceRow.Id, targetRow.Id);
                return Task.FromResult(true);
            })
        {
            ExactMatchesOnly = false
        };

        await viewModel.MergeCandidateAsync(viewModel.Rows.Single(row => row.Id == source.Id), CancellationToken.None);

        merged.Should().Be((source.Id, target.Id));
        viewModel.HasChanges.Should().BeTrue();
        viewModel.HasGroups.Should().BeFalse();
        viewModel.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task MergeCandidate_keeps_best_metadata_book_as_target_when_clicked()
    {
        var source = CreateBook("De Hobbit", ["Unknown"], series: null, language: null);
        var target = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            series: "Midden-aarde",
            language: "nl",
            description: "Een rijker gevuld basisboek.",
            coverRelativePath: "books/cover.jpg");
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup(
                "de hobbit:title",
                "De Hobbit",
                "J.R.R. Tolkien, Unknown",
                [source, target],
                DuplicateCandidateMatchKind.TitleOnly)
        ]);
        (Guid SourceBookId, Guid TargetBookId)? merged = null;
        var viewModel = new DuplicateCandidatesViewModel(
            result,
            "C:/Library",
            mergeCandidateAsync: (sourceRow, targetRow, _, _) =>
            {
                merged = (sourceRow.Id, targetRow.Id);
                return Task.FromResult(true);
            })
        {
            ExactMatchesOnly = false
        };

        await viewModel.MergeCandidateAsync(viewModel.Rows.Single(row => row.Id == target.Id), CancellationToken.None);

        merged.Should().Be((source.Id, target.Id));
    }

    [Fact]
    public void CreateMergePreview_shows_source_and_best_metadata_target()
    {
        var source = CreateBook("De Hobbit", ["Unknown"], series: null, language: null);
        var target = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            series: "Midden-aarde",
            language: "nl",
            description: "Een rijker gevuld basisboek.",
            coverRelativePath: "books/cover.jpg");
        var result = new DuplicateCandidateResult(
        [
            new DuplicateCandidateGroup(
                "de hobbit:title",
                "De Hobbit",
                "J.R.R. Tolkien, Unknown",
                [source, target],
                DuplicateCandidateMatchKind.TitleOnly)
        ]);
        var viewModel = new DuplicateCandidatesViewModel(result, "C:/Library")
        {
            ExactMatchesOnly = false
        };

        var preview = viewModel.CreateMergePreview(viewModel.Rows.Single(row => row.Id == source.Id));

        preview.Should().NotBeNull();
        preview!.Source.Id.Should().Be(source.Id);
        preview.Target.Id.Should().Be(target.Id);
    }

    [Fact]
    public void MergePreview_can_swap_source_and_target_books()
    {
        var source = CreateRow(CreateBook("Bron", ["Auteur"], null, null, coverRelativePath: "books/source/cover.jpg", formats: [EbookFormat.Pdf]));
        var target = CreateRow(CreateBook("Doel", ["Auteur"], null, null, coverRelativePath: "books/target/cover.jpg", formats: [EbookFormat.Epub]));
        var preview = new DuplicateMergePreviewViewModel(source, target);

        preview.SwapDirectionCommand.Execute(null);

        preview.Source.Id.Should().Be(target.Id);
        preview.Target.Id.Should().Be(source.Id);
        preview.Rows.Single(row => row.Label == "Title").SourceValue.Should().Be("Doel");
        preview.Rows.Single(row => row.Label == "Title").TargetValue.Should().Be("Bron");
        preview.Rows.Single(row => row.Label == "Cover").SourceImagePath.Should().Be(target.CoverPath);
        preview.Rows.Single(row => row.Label == "Cover").TargetImagePath.Should().Be(source.CoverPath);
    }

    [Fact]
    public void MergePreview_field_action_cycles_through_no_action_copy_and_merge()
    {
        var row = new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Title, "Title", "Doel", "Bron");

        row.Action.Should().Be(DuplicateMergeFieldAction.NoAction);

        row.CycleActionCommand.Execute(null);
        row.Action.Should().Be(DuplicateMergeFieldAction.Copy);

        row.CycleActionCommand.Execute(null);
        row.Action.Should().Be(DuplicateMergeFieldAction.Merge);

        row.CycleActionCommand.Execute(null);
        row.Action.Should().Be(DuplicateMergeFieldAction.NoAction);
    }

    [Fact]
    public void MergePreview_cover_action_cycles_through_no_action_and_copy_only()
    {
        var row = DuplicateMergeFieldRowViewModel.CreateCover("C:/target/cover.jpg", "C:/source/cover.jpg");

        row.Action.Should().Be(DuplicateMergeFieldAction.NoAction);

        row.CycleActionCommand.Execute(null);
        row.Action.Should().Be(DuplicateMergeFieldAction.Copy);

        row.CycleActionCommand.Execute(null);
        row.Action.Should().Be(DuplicateMergeFieldAction.NoAction);
    }

    [Fact]
    public void MergePreview_disables_action_when_target_and_source_values_are_equal()
    {
        var row = new DuplicateMergeFieldRowViewModel(DuplicateMergeMetadataField.Title, "Title", "De Hobbit", "De Hobbit");

        row.IsActionEnabled.Should().BeFalse();

        row.CycleActionCommand.Execute(null);

        row.Action.Should().Be(DuplicateMergeFieldAction.NoAction);
    }

    [Fact]
    public void MergePreview_includes_cover_and_formats_as_merge_fields()
    {
        var source = CreateRow(CreateBook("Pro Git", ["Unknown"], null, null, coverRelativePath: "books/source/cover.jpg", formats: [EbookFormat.Pdf]));
        var target = CreateRow(CreateBook("Pro Git", ["Scott Chacon"], null, null, coverRelativePath: "books/target/cover.jpg", formats: [EbookFormat.Epub]));

        var preview = new DuplicateMergePreviewViewModel(source, target);

        preview.Rows.Select(row => row.Label).Should().ContainInOrder("Cover", "Title", "Authors", "Formats");
        preview.Rows.Single(row => row.Label == "Cover").IsCover.Should().BeTrue();
        preview.Rows.Single(row => row.Label == "Cover").IsActionEnabled.Should().BeTrue();
        preview.Rows.Single(row => row.Label == "Formats").TargetValue.Should().Be("EPUB");
        preview.Rows.Single(row => row.Label == "Formats").SourceValue.Should().Be("PDF");
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        string? series,
        string? language,
        string? description = null,
        string? coverRelativePath = null,
        IReadOnlyList<EbookFormat>? formats = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(title, authors, Description: description, Language: language, Series: series),
            ReadingStatus.Unread,
            coverRelativePath,
            now,
            now)
        {
            Formats = formats ?? [EbookFormat.Epub, EbookFormat.Pdf]
        };
    }

    private static DuplicateCandidateRowViewModel CreateRow(Book book) =>
        new("group", "group", DuplicateCandidateMatchKind.TitleOnly, book, "C:/Library");
}
