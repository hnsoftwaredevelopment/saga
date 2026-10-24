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
        authors.Attribute("IsEnabled")!.Value.Should().Be("{Binding CanBrowse}");
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
}
