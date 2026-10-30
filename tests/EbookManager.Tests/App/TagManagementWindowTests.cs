using System.Globalization;
using System.Xml.Linq;
using FluentAssertions;

namespace EbookManager.Tests.App;

public sealed class TagManagementWindowTests
{
    [Fact]
    public void Tag_view_is_read_only_with_visible_splitter_and_bounded_virtualized_lists()
    {
        var view = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "TagManagementWindow.xaml"));
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var grid = view.Descendants(ns + "DataGrid").Single();
        grid.Attribute("IsReadOnly")!.Value.Should().Be("True");
        grid.Attribute("CanUserSortColumns")!.Value.Should().Be("False");
        grid.Attribute("Height").Should().NotBeNull();
        view.Descendants(ns + "GridSplitter").Single().Attribute("Style")!.Value.Should().Contain("VisibleVerticalGridSplitterStyle");
        view.Descendants(ns + "ListBox").Should().OnlyContain(list => list.Attribute("VirtualizingPanel.IsVirtualizing")!.Value == "True");
        view.Descendants(ns + "ListBox").Single(list => list.Attribute("ItemsSource")!.Value == "{Binding Warnings}").Attribute("MaxHeight").Should().NotBeNull();
    }

    [Fact]
    public void All_tag_resource_references_exist_in_six_languages_and_placeholders_are_valid()
    {
        var paths = new[] { "TagManagementWindow.xaml", "TagRenameWindow.xaml", "TagAssignmentWindow.xaml" };
        var xaml = string.Join("\n", paths.Select(path => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestAssets", path))));
        var keys = System.Text.RegularExpressions.Regex.Matches(xaml, @"\{loc:Loc (Tag\w+)\}").Select(match => match.Groups[1].Value)
            .Concat(new[] { "TagLoadFailed", "TagRenameWorking", "TagRenameSaved", "TagRenameSavedWithWarnings", "TagRenameNameExists", "TagRenameInvalid", "TagRenameUnchanged", "TagRenameConflict", "TagRenameFailed", "TagRenameRefreshFailed", "TagRenameExplanation", "TagSelectedBooks", "TagChangeSelectionTitle", "TagChangeExplanation", "TagRemoveSelectionTitle", "TagRemoveExplanation", "TagAssignmentWorking", "TagAssignmentSaved", "TagAssignmentSavedWithWarnings", "TagAssignmentConflict", "TagAssignmentUnchanged", "TagAssignmentFailed", "TagAssignmentRefreshFailed" }).Distinct().ToArray();
        foreach (var language in new[] { "", ".nl", ".de", ".fr", ".es", ".it" })
        {
            var resources = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestAssets", "Strings", $"AppResources{language}.resx"))
                .Descendants("data").ToDictionary(row => row.Attribute("name")!.Value, row => row.Element("value")!.Value);
            foreach (var key in keys)
            {
                resources.Should().ContainKey(key);
                resources[key].Should().NotBeNullOrWhiteSpace().And.NotBe(key);
                FluentActions.Invoking(() => string.Format(CultureInfo.InvariantCulture, resources[key], 318, 2)).Should().NotThrow();
            }
        }
    }
}
