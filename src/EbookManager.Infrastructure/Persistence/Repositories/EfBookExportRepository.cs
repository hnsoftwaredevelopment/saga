using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed class EfBookExportRepository(LibraryDbContextFactory factory, string libraryPath) : IBookExportRepository
{
    public async Task<IReadOnlyList<BookExportSource>> ReadAsync(IReadOnlyCollection<Guid> ids, CancellationToken token)
    {
        var result = new List<BookExportSource>();
        await using var context = factory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        foreach (var batch in ids.Distinct().Chunk(500))
        {
            var books = await context.Books.AsNoTracking().Where(book => batch.Contains(book.Id)).Select(book => new { book.Id, book.Title }).ToArrayAsync(token);
            var authors = (await context.BookAuthors.AsNoTracking().Where(link => batch.Contains(link.BookId)).OrderBy(link => link.Order)
                .Select(link => new { link.BookId, link.Author.Name }).ToArrayAsync(token)).ToLookup(link => link.BookId, link => link.Name);
            var files = (await context.BookFiles.AsNoTracking().Where(file => batch.Contains(file.BookId))
                .Select(file => new BookFile(file.Id, file.BookId, file.Format, file.RelativePath, file.Sha256, file.SizeBytes, file.WriteBackStatus, file.WriteBackMessage))
                .ToArrayAsync(token)).ToLookup(file => file.BookId);
            result.AddRange(books.Select(book => new BookExportSource(book.Id, book.Title, authors[book.Id].ToArray(), files[book.Id].OrderBy(file => file.Id).ToArray())));
        }
        return result;
    }
}
