using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;

namespace EbookManager.Libraries;

public sealed class LibraryService(IAppSettingsStore settingsStore)
{
    public async Task<LibraryDescriptor> CreateAsync(
        string name,
        string directoryPath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(directoryPath);
        Directory.CreateDirectory(fullPath);
        Directory.CreateDirectory(Path.Combine(fullPath, "books"));
        return await RememberAsync(new(name, fullPath, DateTimeOffset.UtcNow), cancellationToken);
    }

    public async Task<LibraryDescriptor> OpenAsync(
        string directoryPath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(fullPath);
        }

        Directory.CreateDirectory(Path.Combine(fullPath, "books"));
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath));
        return await RememberAsync(new(name, fullPath, DateTimeOffset.UtcNow), cancellationToken);
    }

    private async Task<LibraryDescriptor> RememberAsync(
        LibraryDescriptor library,
        CancellationToken cancellationToken)
    {
        var libraries = (await settingsStore.ListLibrariesAsync(cancellationToken))
            .Where(existing => !string.Equals(
                existing.DirectoryPath,
                library.DirectoryPath,
                StringComparison.OrdinalIgnoreCase))
            .Append(library)
            .ToArray();

        await settingsStore.SaveLibrariesAsync(libraries, cancellationToken);

        var settings = await settingsStore.LoadAsync(cancellationToken);
        await settingsStore.SaveAsync(settings with { LastLibraryPath = library.DirectoryPath }, cancellationToken);
        return library;
    }
}
