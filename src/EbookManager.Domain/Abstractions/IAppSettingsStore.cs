using EbookManager.Domain.Libraries;

namespace EbookManager.Domain.Abstractions;

public sealed record AppSettings(
    string? LastLibraryPath,
    string Culture,
    string Theme,
    string DefaultView,
    bool ConfirmDelete,
    bool IncludeScanSubdirectories = true);

public interface IAppSettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
    Task<IReadOnlyList<LibraryDescriptor>> ListLibrariesAsync(CancellationToken cancellationToken);
    Task SaveLibrariesAsync(IReadOnlyList<LibraryDescriptor> libraries, CancellationToken cancellationToken);
}
