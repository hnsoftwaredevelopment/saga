using Microsoft.Data.Sqlite;

namespace EbookManager.Tests.TestSupport;

public sealed class TemporarySqliteLibrary : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    public string DirectoryPath => directory.DirectoryPath;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        directory.Dispose();
    }
}
