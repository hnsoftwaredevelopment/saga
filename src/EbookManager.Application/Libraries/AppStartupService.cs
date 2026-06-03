using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;

namespace EbookManager.Libraries;

public sealed class AppStartupService(
    IAppSettingsStore settingsStore,
    LibraryService libraryService,
    CurrentLibrary currentLibrary,
    ILibraryDatabaseInitializer databaseInitializer)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await settingsStore.LoadAsync(cancellationToken);
        var lastLibraryPath = settings.LastLibraryPath;
        if (string.IsNullOrWhiteSpace(lastLibraryPath))
        {
            currentLibrary.Clear();
            return;
        }

        var reopenedLibrary = await TryReopenLibraryAsync(lastLibraryPath, cancellationToken);
        if (reopenedLibrary is null)
        {
            currentLibrary.Clear();
            return;
        }

        currentLibrary.Set(reopenedLibrary);
        await databaseInitializer.InitializeAsync(reopenedLibrary, cancellationToken);
    }

    private async Task<LibraryDescriptor?> TryReopenLibraryAsync(
        string libraryPath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(libraryPath))
            {
                return null;
            }

            return await libraryService.OpenAsync(libraryPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (PathTooLongException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
