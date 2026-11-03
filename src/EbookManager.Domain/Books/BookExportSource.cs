namespace EbookManager.Domain.Books;

public sealed record BookExportSource(Guid Id, string Title, IReadOnlyList<string> Authors, IReadOnlyList<BookFile> Files, bool Available = true);
