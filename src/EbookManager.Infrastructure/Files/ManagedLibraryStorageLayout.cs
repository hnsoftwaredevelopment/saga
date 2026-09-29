namespace EbookManager.Infrastructure.Files;

public sealed class ManagedLibraryStorageLayout
{
    private readonly string libraryRoot;

    public ManagedLibraryStorageLayout(string libraryRootPath)
    {
        if (string.IsNullOrWhiteSpace(libraryRootPath))
        {
            throw new ArgumentException("The library root path must not be blank.", nameof(libraryRootPath));
        }

        libraryRoot = Canonicalize(libraryRootPath);
        BooksDirectory = EnsureContained(Path.Combine(libraryRoot, "books"));
    }

    public string BooksDirectory { get; }

    public string GetBookDirectory(Guid bookId) =>
        GetAbsolutePath(GetRelativeBookDirectory(bookId));

    public string GetRelativeBookDirectory(Guid bookId)
    {
        var id = FormatBookId(bookId);
        return $"books/{id[..2]}/{id}";
    }

    public string GetLegacyBookDirectory(Guid bookId) =>
        GetAbsolutePath(GetLegacyRelativeBookDirectory(bookId));

    public string GetLegacyRelativeBookDirectory(Guid bookId) =>
        $"books/{FormatBookId(bookId)}";

    public string ResolveExistingOrNewBookDirectory(Guid bookId)
    {
        var legacyDirectory = GetLegacyBookDirectory(bookId);
        var bookDirectory = GetBookDirectory(bookId);
        var legacyExists = Directory.Exists(legacyDirectory);
        var bookExists = Directory.Exists(bookDirectory);

        if (legacyExists && bookExists)
        {
            throw new InvalidOperationException(
                $"Book '{bookId:N}' exists in both the legacy and sharded storage locations.");
        }

        return legacyExists ? legacyDirectory : bookDirectory;
    }

    public string GetStagingDirectory(Guid bookId, Guid operationId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(operationId, Guid.Empty);
        var parentDirectory = Path.GetDirectoryName(ResolveExistingOrNewBookDirectory(bookId))
            ?? throw new InvalidOperationException("The managed book directory has no parent directory.");
        return EnsureContained(Path.Combine(
            parentDirectory,
            $".{FormatBookId(bookId)}.{operationId:N}.staging"));
    }

    public string GetAbsolutePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("The relative path must not be blank.", nameof(relativePath));
        }

        return EnsureContained(Path.Combine(libraryRoot, relativePath));
    }

    public string ToRelativePath(string absolutePath) =>
        Path.GetRelativePath(libraryRoot, EnsureContained(absolutePath))
            .Replace(Path.DirectorySeparatorChar, '/');

    public void EnsureNoReparsePoints(string path)
    {
        var containedPath = EnsureContained(path);
        var relativePath = Path.GetRelativePath(libraryRoot, containedPath);
        var current = libraryRoot;
        foreach (var segment in relativePath.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((Directory.Exists(current) || File.Exists(current)) &&
                File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException(
                    "The managed library path contains a symbolic link or reparse point.");
            }
        }
    }

    private string EnsureContained(string path)
    {
        var fullPath = Canonicalize(path);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var rootWithSeparator = libraryRoot + Path.DirectorySeparatorChar;
        if (!fullPath.Equals(libraryRoot, comparison) &&
            !fullPath.StartsWith(rootWithSeparator, comparison))
        {
            throw new InvalidOperationException("The managed path escapes the active library.");
        }

        return fullPath;
    }

    private static string FormatBookId(Guid bookId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bookId, Guid.Empty);
        return bookId.ToString("N");
    }

    private static string Canonicalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
