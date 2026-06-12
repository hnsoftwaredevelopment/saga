using EbookManager.Domain.Books;

namespace EbookManager.Application.Importing;

public sealed class DirectoryScanner
{
    private static readonly EnumerationOptions TopDirectoryOptions = new()
    {
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = true,
        RecurseSubdirectories = false
    };

    private static readonly EnumerationOptions RecursiveDirectoryOptions = new()
    {
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = true,
        RecurseSubdirectories = true
    };

    public string[] Scan(
        string directoryPath,
        bool recursive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rootDirectory = new DirectoryInfo(Path.GetFullPath(directoryPath));
        FileAttributes rootAttributes;
        try
        {
            rootAttributes = rootDirectory.Attributes;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException or PathTooLongException)
        {
            return [];
        }

        if ((rootAttributes & FileAttributes.ReparsePoint) != 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        var matches = new List<string>();
        var options = recursive ? RecursiveDirectoryOptions : TopDirectoryOptions;
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(rootDirectory.FullName, "*", options);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException or PathTooLongException)
        {
            return [];
        }

        foreach (var path in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (EbookFormatExtensions.TryFromFilename(path, out _))
            {
                matches.Add(path);
            }
        }

        matches.Sort(StringComparer.OrdinalIgnoreCase);
        return matches.ToArray();
    }
}
