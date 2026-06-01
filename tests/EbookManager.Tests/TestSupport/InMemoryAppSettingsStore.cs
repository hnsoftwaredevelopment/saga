using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;

namespace EbookManager.Tests.TestSupport;

public sealed class InMemoryAppSettingsStore : IAppSettingsStore
{
    public AppSettings Settings { get; private set; } = new(null, "en-US", "Light", "Detailed", true);

    public List<LibraryDescriptor> Libraries { get; private set; } = [];

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Settings);

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        Settings = settings;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LibraryDescriptor>> ListLibrariesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LibraryDescriptor>>(Libraries);

    public Task SaveLibrariesAsync(
        IReadOnlyList<LibraryDescriptor> libraries,
        CancellationToken cancellationToken)
    {
        Libraries = [.. libraries];
        return Task.CompletedTask;
    }
}
