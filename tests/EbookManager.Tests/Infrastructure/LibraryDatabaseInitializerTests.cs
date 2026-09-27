using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Libraries;
using EbookManager.Infrastructure.Persistence;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace EbookManager.Tests.Infrastructure;

public sealed class LibraryDatabaseInitializerTests
{
    [Fact]
    public async Task Initialize_migrates_the_database_before_migrating_storage()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var factory = new LibraryDbContextFactory();
        var library = new LibraryDescriptor(
            "Test",
            temporaryDirectory.DirectoryPath,
            DateTimeOffset.UtcNow);
        var storageMigrator = new RecordingStorageMigrator();
        var initializer = new LibraryDatabaseInitializer(factory, storageMigrator);
        var progress = new Progress<LibraryStorageMigrationProgress>();

        await initializer.InitializeAsync(library, progress, default);

        storageMigrator.DatabaseExisted.Should().BeTrue();
        storageMigrator.Progress.Should().BeSameAs(progress);
        SqliteConnection.ClearAllPools();
    }

    private sealed class RecordingStorageMigrator : ILibraryStorageMigrator
    {
        public bool DatabaseExisted { get; private set; }
        public IProgress<LibraryStorageMigrationProgress>? Progress { get; private set; }

        public Task<LibraryStorageMigrationResult> MigrateAsync(
            LibraryDescriptor library,
            IProgress<LibraryStorageMigrationProgress>? progress,
            CancellationToken cancellationToken)
        {
            DatabaseExisted = File.Exists(Path.Combine(library.DirectoryPath, "library.db"));
            Progress = progress;
            return Task.FromResult(new LibraryStorageMigrationResult(
                LibraryStorageMigrationStatus.NotRequired,
                0,
                0));
        }
    }
}
