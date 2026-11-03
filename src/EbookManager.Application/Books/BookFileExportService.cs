using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Books;

public sealed class BookFileExportService(ILibraryFileStore fileStore)
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private readonly ILibraryFileStore fileStore = fileStore;

    public Task<BookFileExportResult> ExportAsync(
        Book book,
        BookFile file,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        return ExportAsync(new BookExportSource(book.Id, book.Metadata.Title, book.Metadata.Authors, []), file, destinationDirectory, cancellationToken);
    }

    public async Task<BookFileExportResult> ExportAsync(BookExportSource book, BookFile file, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(file);

        cancellationToken.ThrowIfCancellationRequested();
        if (file.BookId != book.Id)
        {
            return new BookFileExportResult(BookFileExportStatus.Failed, null, "The selected file does not belong to this book.");
        }

        var sourcePath = fileStore.GetAbsolutePath(file.RelativePath);
        if (!File.Exists(sourcePath))
        {
            return new BookFileExportResult(BookFileExportStatus.SourceMissing, null, "The managed book file is missing.");
        }

        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            return new BookFileExportResult(BookFileExportStatus.Failed, null, "No export folder was selected.");
        }

        Directory.CreateDirectory(destinationDirectory);
        var temporary = Path.Combine(destinationDirectory, $".saga-export-{Guid.NewGuid():N}.tmp");
        var owned = false;
        try
        {
            await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
            await using (var destination = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                owned = true;
                await source.CopyToAsync(destination, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destinationPath = CreateUniqueDestinationPath(book.Title, book.Authors, file, destinationDirectory);
                try { File.Move(temporary, destinationPath, overwrite: false); owned = false; return new(BookFileExportStatus.Exported, destinationPath, null); }
                catch (IOException) when (File.Exists(destinationPath)) { /* Another writer claimed the name: choose a fresh one. */ }
            }
        }
        finally { if (owned) File.Delete(temporary); }
    }

    public string CreateUniqueDestinationPath(
        Book book,
        BookFile file,
        string destinationDirectory)
        => CreateUniqueDestinationPath(book.Metadata.Title, book.Metadata.Authors, file, destinationDirectory);

    private static string CreateUniqueDestinationPath(string title, IReadOnlyList<string> authors, BookFile file, string destinationDirectory)
    {
        var extension = Path.GetExtension(file.RelativePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = $".{file.Format.ToString().ToLowerInvariant()}";
        }

        var baseName = CreateBaseFileName(title, authors);
        var candidate = Path.Combine(destinationDirectory, $"{baseName}{extension}");
        for (var index = 1; File.Exists(candidate); index++)
        {
            candidate = Path.Combine(destinationDirectory, $"{baseName} ({index}){extension}");
        }

        return candidate;
    }

    private static string CreateBaseFileName(string title, IReadOnlyList<string> authors)
    {
        var author = authors.FirstOrDefault();
        var parts = string.IsNullOrWhiteSpace(author)
            ? [title]
            : new[] { author, title };
        var value = string.Join(" - ", parts.Select(SanitizeFileNamePart).Where(part => part.Length > 0));
        return string.IsNullOrWhiteSpace(value) ? "book" : value;
    }

    private static string SanitizeFileNamePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(value
            .Trim()
            .Select(character => InvalidFileNameChars.Contains(character) ? '_' : character)
            .ToArray());
        while (sanitized.Contains("  ", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return sanitized.Trim(' ', '.');
    }
}

public enum BookFileExportStatus
{
    Exported,
    SourceMissing,
    Failed
}

public sealed record BookFileExportResult(
    BookFileExportStatus Status,
    string? DestinationPath,
    string? Message);
