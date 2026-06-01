using EbookManager.Tests.TestSupport;
using FluentAssertions;
using EbookManager.Libraries;

namespace EbookManager.Tests.Libraries;

public sealed class LibraryServiceTests : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory = new();

    [Fact]
    public async Task Create_creates_books_directory_and_remembers_library()
    {
        var root = Path.Combine(temporaryDirectory.DirectoryPath, "ELibrary");
        var store = new InMemoryAppSettingsStore();
        var service = new LibraryService(store);

        var library = await service.CreateAsync("ELibrary", root, CancellationToken.None);

        Directory.Exists(Path.Combine(root, "books")).Should().BeTrue();
        (await store.ListLibrariesAsync(default)).Should().ContainSingle(x => x.DirectoryPath == root);
        (await store.LoadAsync(default)).LastLibraryPath.Should().Be(root);
        library.Name.Should().Be("ELibrary");
    }

    [Fact]
    public async Task Open_rejects_missing_directory()
    {
        var root = Path.Combine(temporaryDirectory.DirectoryPath, "Missing");
        var service = new LibraryService(new InMemoryAppSettingsStore());

        var act = () => service.OpenAsync(root, CancellationToken.None);

        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task Open_existing_directory_creates_books_directory_and_remembers_library()
    {
        var root = temporaryDirectory.CreateSubdirectory("Existing").FullName;
        var store = new InMemoryAppSettingsStore();
        var service = new LibraryService(store);

        var library = await service.OpenAsync(root, CancellationToken.None);

        Directory.Exists(Path.Combine(root, "books")).Should().BeTrue();
        (await store.ListLibrariesAsync(default)).Should().ContainSingle(x => x.DirectoryPath == root);
        (await store.LoadAsync(default)).LastLibraryPath.Should().Be(root);
        library.Name.Should().Be("Existing");
    }

    public void Dispose() => temporaryDirectory.Dispose();
}
