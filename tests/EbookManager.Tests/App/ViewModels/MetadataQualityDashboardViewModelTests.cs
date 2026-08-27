using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class MetadataQualityDashboardViewModelTests
{
    [Fact]
    public void Constructor_selects_first_book_of_first_non_empty_issue()
    {
        var book = CreateBook("Zonder auteur", ["Unknown"]);

        var dashboard = new MetadataQualityDashboardViewModel([book], key => key);

        dashboard.SelectedIssue!.Title.Should().Be("MetadataQualityMissingAuthor");
        dashboard.SelectedBook.Should().NotBeNull();
        dashboard.SelectedBookId.Should().Be(book.Id);
    }

    [Fact]
    public void Selecting_issue_selects_its_first_book()
    {
        var first = CreateBook("Eerste", ["Unknown"]);
        var second = CreateBook("Tweede", ["Auteur"]);
        var dashboard = new MetadataQualityDashboardViewModel([first, second], key => key);

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.Title == "MetadataQualityMissingCover");

        dashboard.SelectedBookId.Should().Be(first.Id);
    }

    [Fact]
    public void Selecting_empty_issue_clears_book_selection()
    {
        var book = CreateBook("Compleet", ["Auteur"], coverBytes: [1]);
        var dashboard = new MetadataQualityDashboardViewModel([book], key => key);

        dashboard.SelectedIssue = dashboard.Issues.Single(issue =>
            issue.Title == "MetadataQualityMissingAuthor");

        dashboard.SelectedBook.Should().BeNull();
        dashboard.SelectedBookId.Should().BeNull();
        dashboard.CanOpenSelectedBook.Should().BeFalse();
    }

    [Fact]
    public void Selected_book_enables_open_in_library_action()
    {
        var book = CreateBook("Zonder auteur", ["Unknown"]);

        var dashboard = new MetadataQualityDashboardViewModel([book], key => key);

        dashboard.CanOpenSelectedBook.Should().BeTrue();
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        byte[]? coverBytes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(title, authors, Language: "nl", CoverBytes: coverBytes),
            ReadingStatus.Unread,
            null,
            now,
            now);
    }
}
