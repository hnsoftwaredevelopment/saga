using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class BulkObservableCollectionTests
{
    [Fact]
    public void ReplaceAll_can_use_the_collection_itself_as_source()
    {
        var collection = new BulkObservableCollection<string>
        {
            "first",
            "second"
        };

        collection.ReplaceAll(collection);

        collection.Should().Equal("first", "second");
    }
}
