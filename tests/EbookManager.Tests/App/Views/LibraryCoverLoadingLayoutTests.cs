using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class LibraryCoverLoadingLayoutTests
{
    private static readonly XNamespace Controls = "clr-namespace:EbookManager.App.Controls";

    [Theory]
    [InlineData("BookshelfView.xaml", "220")]
    [InlineData("DetailedGridView.xaml", "48")]
    public void Library_view_loads_cover_paths_through_async_images(string fileName, string expectedWidth)
    {
        var document = Load(fileName);

        var cover = document.Descendants(Controls + "AsyncCoverImage").Single();
        cover.Attribute("SourcePath")!.Value.Should().Be("{Binding CoverPath}");
        cover.Attribute("DecodePixelWidth")!.Value.Should().Be(expectedWidth);
        document.ToString().Should().NotContain("FilePathToImageSourceConverter");
    }

    [Fact]
    public void List_view_uses_the_shared_column_row_control_for_cover_cells()
    {
        var document = Load("LibraryListView.xaml");

        document.Descendants(Controls + "LibraryColumnRowGrid").Should().NotBeEmpty();
    }

    private static XDocument Load(string fileName) =>
        XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", fileName));
}
