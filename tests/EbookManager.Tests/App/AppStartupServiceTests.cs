using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;
using EbookManager.Libraries;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.App;

public sealed class AppStartupServiceTests
{
    [Fact]
    public async Task InitializeAsync_reopens_valid_last_library_path_and_migrates_database()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var libraryPath = temporaryDirectory.CreateSubdirectory("Library").FullName;
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(new AppSettings(libraryPath, "en-US", "Light", "Detailed", true), default);
        var currentLibrary = new CurrentLibrary();
        var initializer = new RecordingLibraryDatabaseInitializer();
        var service = new AppStartupService(settingsStore, new LibraryService(settingsStore), currentLibrary, initializer);

        await service.InitializeAsync(default);

        currentLibrary.Current.Should().NotBeNull();
        currentLibrary.Current!.DirectoryPath.Should().Be(Path.TrimEndingDirectorySeparator(Path.GetFullPath(libraryPath)));
        initializer.InitializedLibrary.Should().BeEquivalentTo(currentLibrary.Current);
        initializer.CallCount.Should().Be(1);
        Directory.Exists(Path.Combine(libraryPath, "books")).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_missing_last_library_path_leaves_current_library_unset()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var missingLibraryPath = Path.Combine(temporaryDirectory.DirectoryPath, "MissingLibrary");
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(new AppSettings(missingLibraryPath, "en-US", "Light", "Detailed", true), default);
        var currentLibrary = new CurrentLibrary();
        currentLibrary.Set(new LibraryDescriptor("Previous", temporaryDirectory.DirectoryPath, DateTimeOffset.UtcNow));
        var initializer = new RecordingLibraryDatabaseInitializer();
        var service = new AppStartupService(settingsStore, new LibraryService(settingsStore), currentLibrary, initializer);

        await service.InitializeAsync(default);

        currentLibrary.Current.Should().BeNull();
        initializer.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task InitializeAsync_without_last_library_path_leaves_current_library_unset()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var currentLibrary = new CurrentLibrary();
        currentLibrary.Set(new LibraryDescriptor("Previous", Path.GetTempPath(), DateTimeOffset.UtcNow));
        var initializer = new RecordingLibraryDatabaseInitializer();
        var service = new AppStartupService(settingsStore, new LibraryService(settingsStore), currentLibrary, initializer);

        await service.InitializeAsync(default);

        currentLibrary.Current.Should().BeNull();
        initializer.CallCount.Should().Be(0);
    }

    [Fact]
    public void CurrentLibrary_raises_changed_when_set_and_cleared()
    {
        var currentLibrary = new CurrentLibrary();
        var changes = 0;
        currentLibrary.Changed += (_, _) => changes++;

        currentLibrary.Set(new LibraryDescriptor("Library", "C:/Library", DateTimeOffset.UtcNow));
        currentLibrary.Clear();

        changes.Should().Be(2);
    }

    private sealed class RecordingLibraryDatabaseInitializer : ILibraryDatabaseInitializer
    {
        public LibraryDescriptor? InitializedLibrary { get; private set; }

        public int CallCount { get; private set; }

        public Task InitializeAsync(LibraryDescriptor library, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            InitializedLibrary = library;
            return Task.CompletedTask;
        }
    }
}
