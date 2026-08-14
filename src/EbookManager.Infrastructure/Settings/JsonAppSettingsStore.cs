using System.Collections.Concurrent;
using System.Text.Json;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;
using EbookManager.Domain.Settings;

namespace EbookManager.Infrastructure.Settings;

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private const string CurrentApplicationDirectoryName = "Saga";
    private const string LegacyApplicationDirectoryName = "EbookManager";

    private static readonly AppSettings DefaultSettings = new(
        null,
        "en-US",
        "Light",
        "Detailed",
        true,
        true,
        AuthorSortStrategy.DisplayName,
        true,
        true,
        new DuplicateMergeDefaultSettings(),
        null,
        null,
        null);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TargetLocks = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    private readonly string baseDirectory;
    private readonly string? legacyBaseDirectory;

    public JsonAppSettingsStore(string? baseDirectory = null)
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        this.baseDirectory = baseDirectory ?? Path.Combine(localApplicationData, CurrentApplicationDirectoryName);
        legacyBaseDirectory = baseDirectory is null
            ? Path.Combine(localApplicationData, LegacyApplicationDirectoryName)
            : null;
    }

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) =>
        ReadAsync("settings.json", DefaultSettings, cancellationToken);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken) =>
        WriteAsync("settings.json", settings, cancellationToken);

    public async Task<IReadOnlyList<LibraryDescriptor>> ListLibrariesAsync(
        CancellationToken cancellationToken) =>
        await ReadAsync("libraries.json", Array.Empty<LibraryDescriptor>(), cancellationToken);

    public Task SaveLibrariesAsync(
        IReadOnlyList<LibraryDescriptor> libraries,
        CancellationToken cancellationToken) =>
        WriteAsync("libraries.json", libraries, cancellationToken);

    private async Task<T> ReadAsync<T>(
        string filename,
        T defaultValue,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(baseDirectory, filename);
        var targetLock = GetTargetLock(path);

        await targetLock.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(path))
            {
                if (TryGetLegacyPath(filename) is { } legacyPath && File.Exists(legacyPath))
                {
                    await using var legacyStream = File.OpenRead(legacyPath);
                    return await JsonSerializer.DeserializeAsync<T>(legacyStream, JsonOptions, cancellationToken) ?? defaultValue;
                }

                return defaultValue;
            }

            try
            {
                await using var stream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken) ?? defaultValue;
            }
            catch (JsonException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Quarantine(path);
                return defaultValue;
            }
        }
        finally
        {
            targetLock.Release();
        }
    }

    private async Task WriteAsync<T>(
        string filename,
        T value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(baseDirectory);

        var path = Path.Combine(baseDirectory, filename);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
            }

            var targetLock = GetTargetLock(path);
            await targetLock.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, path, overwrite: true);
            }
            finally
            {
                targetLock.Release();
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static SemaphoreSlim GetTargetLock(string path) =>
        TargetLocks.GetOrAdd(Path.GetFullPath(path), _ => new SemaphoreSlim(1, 1));

    private string? TryGetLegacyPath(string filename)
    {
        if (legacyBaseDirectory is null)
        {
            return null;
        }

        var currentPath = Path.GetFullPath(Path.Combine(baseDirectory, filename));
        var legacyPath = Path.GetFullPath(Path.Combine(legacyBaseDirectory, filename));
        return string.Equals(currentPath, legacyPath, StringComparison.OrdinalIgnoreCase)
            ? null
            : legacyPath;
    }

    private static void Quarantine(string path)
    {
        if (File.Exists(path))
        {
            File.Move(path, $"{path}.{Guid.NewGuid():N}.corrupt");
        }
    }
}
