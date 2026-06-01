using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;
using EbookManager.Infrastructure.Settings;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.Libraries;

public sealed class JsonAppSettingsStoreTests : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory = new();

    [Fact]
    public async Task Settings_round_trip_through_injected_base_directory()
    {
        var store = new JsonAppSettingsStore(temporaryDirectory.DirectoryPath);
        var settings = new AppSettings("C:\\Books", "nl-NL", "Dark", "List", false);

        await store.SaveAsync(settings, CancellationToken.None);

        var loaded = await new JsonAppSettingsStore(temporaryDirectory.DirectoryPath).LoadAsync(default);
        loaded.Should().Be(settings);
        File.Exists(Path.Combine(temporaryDirectory.DirectoryPath, "settings.json")).Should().BeTrue();
        File.Exists(Path.Combine(temporaryDirectory.DirectoryPath, "libraries.json")).Should().BeFalse();
    }

    [Fact]
    public async Task Libraries_round_trip_separately_from_settings()
    {
        var store = new JsonAppSettingsStore(temporaryDirectory.DirectoryPath);
        LibraryDescriptor[] libraries =
        [
            new("First", "C:\\First", DateTimeOffset.Parse("2026-06-01T08:00:00Z")),
            new("Second", "C:\\Second", DateTimeOffset.Parse("2026-06-01T09:00:00Z"))
        ];

        await store.SaveLibrariesAsync(libraries, CancellationToken.None);

        var loaded = await new JsonAppSettingsStore(temporaryDirectory.DirectoryPath).ListLibrariesAsync(default);
        loaded.Should().Equal(libraries);
        File.Exists(Path.Combine(temporaryDirectory.DirectoryPath, "libraries.json")).Should().BeTrue();
        File.Exists(Path.Combine(temporaryDirectory.DirectoryPath, "settings.json")).Should().BeFalse();
    }

    [Fact]
    public async Task Repeated_save_overwrites_target_and_leaves_no_temporary_file()
    {
        var store = new JsonAppSettingsStore(temporaryDirectory.DirectoryPath);
        await store.SaveAsync(new(null, "en-US", "Light", "Detailed", true), default);

        var updated = new AppSettings("C:\\Updated", "nl-NL", "Dark", "List", false);
        await store.SaveAsync(updated, default);

        (await store.LoadAsync(default)).Should().Be(updated);
        File.Exists(Path.Combine(temporaryDirectory.DirectoryPath, "settings.json.tmp")).Should().BeFalse();
    }

    public void Dispose() => temporaryDirectory.Dispose();
}
