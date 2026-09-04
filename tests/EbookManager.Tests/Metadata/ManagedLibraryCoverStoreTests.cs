using EbookManager.Infrastructure.Files;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class ManagedLibraryCoverStoreTests
{
    [Fact]
    public async Task Save_writes_cover_to_the_managed_book_directory()
    {
        using var root = new TemporaryDirectory();
        var store = new ManagedLibraryCoverStore(root.DirectoryPath);
        var bookId = Guid.NewGuid();

        var relativePath = await store.SaveAsync(bookId, [1, 2, 3], CancellationToken.None);

        relativePath.Should().Be($"books/{bookId:N}/cover.jpg");
        var absolutePath = Path.Combine(root.DirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.ReadAllBytes(absolutePath).Should().Equal(1, 2, 3);
        Directory.GetFiles(Path.GetDirectoryName(absolutePath)!, "*.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_removes_only_the_managed_cover()
    {
        using var root = new TemporaryDirectory();
        var store = new ManagedLibraryCoverStore(root.DirectoryPath);
        var bookId = Guid.NewGuid();
        await store.SaveAsync(bookId, [1, 2, 3], CancellationToken.None);
        var sibling = Path.Combine(root.DirectoryPath, "books", bookId.ToString("N"), "book.epub");
        await File.WriteAllTextAsync(sibling, "book");

        await store.DeleteAsync(bookId, CancellationToken.None);

        File.Exists(Path.Combine(root.DirectoryPath, "books", bookId.ToString("N"), "cover.jpg")).Should().BeFalse();
        File.Exists(sibling).Should().BeTrue();
    }
}
