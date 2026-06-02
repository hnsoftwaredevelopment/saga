using EbookManager.Domain.Books;

namespace EbookManager.Application.Importing;

public sealed class DirectoryScanner
{
    public string[] Scan(
        string directoryPath,
        bool recursive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var matches = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directoryPath, "*", searchOption))
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
