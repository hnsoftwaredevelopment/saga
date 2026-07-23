using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;

namespace EbookManager.Application.Books;

public sealed class BookFileExportService(ILibraryFileStore fileStore)
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private readonly ILibraryFileStore fileStore = fileStore;

    public async Task<BookFileExportResult> ExportAsync(
        Book book,
        BookFile file,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
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
        var destinationPath = CreateUniqueDestinationPath(book, file, destinationDirectory);
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        await using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(destination, cancellationToken);
        return new BookFileExportResult(BookFileExportStatus.Exported, destinationPath, null);
    }

    public string CreateUniqueDestinationPath(
        Book book,
        BookFile file,
        string destinationDirectory)
    {
        var extension = Path.GetExtension(file.RelativePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = $".{file.Format.ToString().ToLowerInvariant()}";
        }

        var baseName = CreateBaseFileName(book);
        var candidate = Path.Combine(destinationDirectory, $"{baseName}{extension}");
        for (var index = 1; File.Exists(candidate); index++)
        {
            candidate = Path.Combine(destinationDirectory, $"{baseName} ({index}){extension}");
        }

        return candidate;
    }

    private static string CreateBaseFileName(Book book)
    {
        var author = book.Metadata.Authors.FirstOrDefault();
        var parts = string.IsNullOrWhiteSpace(author)
            ? [book.Metadata.Title]
            : new[] { author, book.Metadata.Title };
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
