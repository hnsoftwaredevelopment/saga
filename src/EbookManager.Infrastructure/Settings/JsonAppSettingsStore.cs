using System.Text.Json;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;

namespace EbookManager.Infrastructure.Settings;

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private static readonly AppSettings DefaultSettings = new(null, "en-US", "Light", "Detailed", true);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string baseDirectory;

    public JsonAppSettingsStore(string? baseDirectory = null)
    {
        this.baseDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EbookManager");
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
        var path = Path.Combine(baseDirectory, filename);
        if (!File.Exists(path))
        {
            return defaultValue;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken) ?? defaultValue;
    }

    private async Task WriteAsync<T>(
        string filename,
        T value,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(baseDirectory);

        var path = Path.Combine(baseDirectory, filename);
        var temporaryPath = $"{path}.tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }
}
