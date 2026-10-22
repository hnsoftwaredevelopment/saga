using EbookManager.Infrastructure.Files;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed class AuthorPhotoStoreTests
{
    [Fact]
    public async Task Stores_unique_portable_photos_and_only_deletes_the_requested_owned_photo()
    {
        using var directory = new TemporaryDirectory();
        var store = new ManagedAuthorPhotoStore(directory.DirectoryPath);
        var author = Guid.NewGuid();
        byte[] bytes = [0xff, 0xd8, 0xff, 0xd9];
        var first = await store.SaveJpegAsync(author, bytes, default);
        var second = await store.SaveJpegAsync(author, bytes, default);
        first.Should().StartWith($"authors/{author:N}"[..10] + "/");
        first.Should().NotBe(second);
        (await store.ReadAsync(author, first, default)).Should().Equal(bytes);
        await store.DeleteAsync(author, second, default);
        (await store.ReadAsync(author, first, default)).Should().Equal(bytes);
        (await store.ReadAsync(author, second, default)).Should().BeNull();
    }

    [Theory]
    [InlineData("../outside.jpg")]
    [InlineData("books/cover.jpg")]
    [InlineData("authors/ab/someone-elses.jpg")]
    public async Task Rejects_unowned_paths_for_read_and_delete(string path)
    {
        using var directory = new TemporaryDirectory();
        var store = new ManagedAuthorPhotoStore(directory.DirectoryPath);
        var read = () => store.ReadAsync(Guid.NewGuid(), path, default);
        var delete = () => store.DeleteAsync(Guid.NewGuid(), path, default);
        await read.Should().ThrowAsync<ArgumentException>();
        await delete.Should().ThrowAsync<ArgumentException>();
    }
}
