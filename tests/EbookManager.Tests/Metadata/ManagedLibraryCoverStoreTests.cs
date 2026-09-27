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
        var shard = bookId.ToString("N")[..2];

        var relativePath = await store.SaveAsync(bookId, [1, 2, 3], CancellationToken.None);

        relativePath.Should().Be($"books/{shard}/{bookId:N}/cover.jpg");
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
        var shard = bookId.ToString("N")[..2];
        await store.SaveAsync(bookId, [1, 2, 3], CancellationToken.None);
        var sibling = Path.Combine(root.DirectoryPath, "books", shard, bookId.ToString("N"), "book.epub");
        await File.WriteAllTextAsync(sibling, "book");

        await store.DeleteAsync(bookId, CancellationToken.None);

        File.Exists(Path.Combine(root.DirectoryPath, "books", shard, bookId.ToString("N"), "cover.jpg")).Should().BeFalse();
        File.Exists(sibling).Should().BeTrue();
    }

    [Fact]
    public async Task Save_uses_an_existing_legacy_book_directory_until_migration()
    {
        using var root = new TemporaryDirectory();
        var bookId = Guid.NewGuid();
        var legacyDirectory = Path.Combine(root.DirectoryPath, "books", bookId.ToString("N"));
        Directory.CreateDirectory(legacyDirectory);
        var store = new ManagedLibraryCoverStore(root.DirectoryPath);

        var relativePath = await store.SaveAsync(bookId, [1, 2, 3], CancellationToken.None);

        relativePath.Should().Be($"books/{bookId:N}/cover.jpg");
        File.ReadAllBytes(Path.Combine(legacyDirectory, "cover.jpg")).Should().Equal(1, 2, 3);
        Directory.Exists(Path.Combine(
                root.DirectoryPath,
                "books",
                bookId.ToString("N")[..2],
                bookId.ToString("N")))
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Save_refuses_a_book_directory_that_is_a_symbolic_link()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var root = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var bookId = Guid.NewGuid();
        var booksDirectory = Directory.CreateDirectory(Path.Combine(root.DirectoryPath, "books"));
        var shardDirectory = booksDirectory.CreateSubdirectory(bookId.ToString("N")[..2]);
        var linkedBookDirectory = Path.Combine(shardDirectory.FullName, bookId.ToString("N"));
        try
        {
            Directory.CreateSymbolicLink(linkedBookDirectory, outside.DirectoryPath);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            return;
        }

        var store = new ManagedLibraryCoverStore(root.DirectoryPath);
        var action = () => store.SaveAsync(bookId, [1, 2, 3], CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        File.Exists(Path.Combine(outside.DirectoryPath, "cover.jpg")).Should().BeFalse();
    }
}
