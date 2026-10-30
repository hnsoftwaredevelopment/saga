using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class AuthorManagementWindowTests
{
    [Fact]
    public void Profile_scrolls_in_a_small_work_area_while_the_books_keep_a_bounded_viewport()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "AuthorManagementWindow.xaml"));
        XNamespace p = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        double.Parse(document.Root!.Attribute("MinHeight")!.Value).Should().BeLessThanOrEqualTo(520);
        var scroll = document.Descendants(p + "ScrollViewer").Single(element => (string?)element.Attribute(x + "Name") == "ProfileScrollViewer");
        scroll.Attribute("VerticalScrollBarVisibility")!.Value.Should().Be("Auto");
        var books = scroll.Descendants(p + "DataGrid").Single();
        double.Parse(books.Attribute("Height")!.Value).Should().BeInRange(100, 300);
        books.Attribute("EnableRowVirtualization")!.Value.Should().Be("True");
    }

    [Fact]
    public void Window_has_virtualized_authors_visible_splitter_and_readonly_book_list()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "AuthorManagementWindow.xaml"));
        XNamespace p = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var authors = document.Descendants(p + "ListBox").Single(element => (string?)element.Attribute(xaml + "Name") == "AuthorsList");
        authors.Attribute("VirtualizingPanel.VirtualizationMode")!.Value.Should().Be("Recycling");
        authors.Attribute("IsEnabled")!.Value.Should().Be("{Binding CanChooseAuthors}");
        authors.Attribute("SelectionMode")!.Value.Should().Be("Extended");
        document.Descendants(p + "GridSplitter").Single().Attribute("Style")!.Value
            .Should().Be("{StaticResource VisibleVerticalGridSplitterStyle}");
        document.Descendants(p + "DataGrid").Single().Attribute("IsReadOnly")!.Value.Should().Be("True");
        document.Descendants(p + "TextBox").Should().OnlyContain(x => x.Attribute("AutomationProperties.Name") != null);
        document.Descendants(p + "Button").Should().Contain(x => (string?)x.Attribute("IsCancel") == "True");
    }

    [Fact]
    public void Author_texts_are_present_in_every_language()
    {
        var english = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", "AppResources.resx"));
        var keys = english.Descendants("data").Select(x => (string)x.Attribute("name")!)
            .Where(x => x.StartsWith("Author", StringComparison.Ordinal) && x != "Authors").ToArray();
        keys.Should().Contain("AuthorManagementTitle");
        foreach (var culture in new[] { "nl", "de", "fr", "es", "it" })
        {
            var translated = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources.{culture}.resx"));
            foreach (var key in keys)
                translated.Descendants("data").Single(x => (string?)x.Attribute("name") == key)
                    .Element("value")!.Value.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void AuthorMerge_dialog_has_scrollable_choices_fixed_actions_and_valid_localized_placeholders()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "AuthorMergeWindow.xaml"));
        XNamespace p = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        document.Descendants(p + "ComboBox").Should().HaveCount(4);
        document.Descendants(p + "CheckBox").Should().BeEmpty();
        var cards = document.Descendants(p + "ListBox").Single();
        cards.Attribute("VirtualizingPanel.VirtualizationMode")!.Value.Should().Be("Recycling");
        document.Descendants(p + "TextBlock").Should().Contain(text => (string?)text.Attribute("Text") == "{Binding BiographyDisplay}");
        document.Descendants(p + "ScrollViewer").Should().Contain(view => (string?)view.Attribute("VerticalScrollBarVisibility") == "Auto");
        document.Descendants(p + "Button").Should().Contain(button => (string?)button.Attribute("IsEnabled") == "{Binding CanMerge}");
        foreach (var language in new[] { "", ".nl", ".de", ".fr", ".es", ".it" })
        {
            var resources = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources{language}.resx"));
            foreach (var text in resources.Descendants("data").Where(row => ((string?)row.Attribute("name"))?.StartsWith("AuthorMerge", StringComparison.Ordinal) == true))
                FluentActions.Invoking(() => string.Format(System.Globalization.CultureInfo.InvariantCulture, text.Element("value")!.Value, 205, 510)).Should().NotThrow();
        }
    }

    [Fact]
    public void AuthorDuplicate_suggestions_are_readonly_virtualized_pairs_with_explicit_comparison()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "AuthorDuplicatesWindow.xaml"));
        XNamespace p = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var grid = document.Descendants(p + "DataGrid").Single();
        grid.Attribute("IsReadOnly")!.Value.Should().Be("True");
        grid.Attribute("EnableRowVirtualization")!.Value.Should().Be("True");
        grid.Attribute("SelectionMode")!.Value.Should().Be("Single");
        document.Descendants(p + "Button").Should().Contain(button => (string?)button.Attribute("IsEnabled") == "{Binding CanCompare}");
    }
}
