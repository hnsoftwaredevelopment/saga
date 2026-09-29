using EbookManager.Infrastructure.Files;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed class ManagedLibraryStorageLayoutTests
{
    [Fact]
    public void Book_directory_uses_the_first_two_lowercase_id_characters_as_shard()
    {
        using var root = new TemporaryDirectory();
        var layout = new ManagedLibraryStorageLayout(root.DirectoryPath);
        var bookId = Guid.ParseExact("7F3A00112233445566778899AABBCCDD", "N");

        var directory = layout.GetBookDirectory(bookId);

        directory.Should().Be(Path.Combine(
            root.DirectoryPath,
            "books",
            "7f",
            "7f3a00112233445566778899aabbccdd"));
        layout.GetRelativeBookDirectory(bookId)
            .Should().Be("books/7f/7f3a00112233445566778899aabbccdd");
    }

    [Fact]
    public void Legacy_directory_remains_available_for_safe_migration()
    {
        using var root = new TemporaryDirectory();
        var layout = new ManagedLibraryStorageLayout(root.DirectoryPath);
        var bookId = Guid.ParseExact("00112233445566778899AABBCCDDEEFF", "N");

        layout.GetLegacyBookDirectory(bookId).Should().Be(Path.Combine(
            root.DirectoryPath,
            "books",
            "00112233445566778899aabbccddeeff"));
        layout.GetLegacyRelativeBookDirectory(bookId)
            .Should().Be("books/00112233445566778899aabbccddeeff");
    }

    [Fact]
    public void Absolute_path_rejects_a_path_outside_the_library()
    {
        using var root = new TemporaryDirectory();
        var layout = new ManagedLibraryStorageLayout(root.DirectoryPath);

        var action = () => layout.GetAbsolutePath("../outside.epub");

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Empty_book_id_is_rejected()
    {
        using var root = new TemporaryDirectory();
        var layout = new ManagedLibraryStorageLayout(root.DirectoryPath);

        var action = () => layout.GetBookDirectory(Guid.Empty);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Resolve_book_directory_uses_legacy_only_when_it_is_the_sole_existing_location()
    {
        using var root = new TemporaryDirectory();
        var layout = new ManagedLibraryStorageLayout(root.DirectoryPath);
        var bookId = Guid.NewGuid();
        var legacyDirectory = layout.GetLegacyBookDirectory(bookId);
        Directory.CreateDirectory(legacyDirectory);

        layout.ResolveExistingOrNewBookDirectory(bookId).Should().Be(legacyDirectory);
    }

    [Fact]
    public void Resolve_book_directory_refuses_conflicting_old_and_new_locations()
    {
        using var root = new TemporaryDirectory();
        var layout = new ManagedLibraryStorageLayout(root.DirectoryPath);
        var bookId = Guid.NewGuid();
        Directory.CreateDirectory(layout.GetLegacyBookDirectory(bookId));
        Directory.CreateDirectory(layout.GetBookDirectory(bookId));

        var action = () => layout.ResolveExistingOrNewBookDirectory(bookId);

        action.Should().Throw<InvalidOperationException>();
    }
}
