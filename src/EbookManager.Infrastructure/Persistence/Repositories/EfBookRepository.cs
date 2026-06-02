using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed class EfBookRepository(
    LibraryDbContextFactory contextFactory,
    string libraryPath) : IBookRepository
{
    public async Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var books = await BooksWithMetadata(context)
            .AsNoTracking()
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return books.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var book = await BooksWithMetadata(context)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return book is null ? null : ToDomain(book);
    }

    public async Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        return await context.BookFiles.AnyAsync(x => x.Sha256 == sha256, cancellationToken);
    }

    public async Task<bool> HasNormalizedTitleAndAuthorAsync(
        string title,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken)
    {
        var normalizedTitle = Normalize(title);
        var normalizedAuthors = authors
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        await using var context = contextFactory.Create(libraryPath);
        var candidates = await context.Books
            .AsNoTracking()
            .Where(x => x.NormalizedTitle == normalizedTitle)
            .Select(x => x.BookAuthors
                .Select(bookAuthor => bookAuthor.Author.NormalizedName)
                .OrderBy(name => name)
                .ToArray())
            .ToListAsync(cancellationToken);

        return candidates.Any(candidate =>
            candidate.Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .SequenceEqual(normalizedAuthors, StringComparer.Ordinal));
    }

    public async Task AddAsync(
        Book book,
        BookFile file,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = ToEntity(book);
        context.Books.Add(entity);
        await AddAuthorsAsync(context, entity, book.Metadata.Authors, cancellationToken);
        await AddTagsAsync(context, entity, book.Metadata.Tags, cancellationToken);
        entity.Files.Add(ToEntity(file));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await context.Books
            .SingleOrDefaultAsync(x => x.Id == book.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Book '{book.Id}' does not exist.");

        Apply(book, entity);
        await context.SaveChangesAsync(cancellationToken);
        await context.BookAuthors
            .Where(x => x.BookId == book.Id)
            .ExecuteDeleteAsync(cancellationToken);
        await context.BookTags
            .Where(x => x.BookId == book.Id)
            .ExecuteDeleteAsync(cancellationToken);
        context.ChangeTracker.Clear();
        entity = await context.Books.SingleAsync(x => x.Id == book.Id, cancellationToken);
        await AddAuthorsAsync(context, entity, book.Metadata.Authors, cancellationToken);
        await AddTagsAsync(context, entity, book.Metadata.Tags, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await RemoveOrphanedMetadataAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await context.Books.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        context.Books.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        await RemoveOrphanedMetadataAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static IQueryable<BookEntity> BooksWithMetadata(LibraryDbContext context) =>
        context.Books
            .Include(x => x.BookAuthors)
                .ThenInclude(x => x.Author)
            .Include(x => x.BookTags)
                .ThenInclude(x => x.Tag);

    private static async Task AddAuthorsAsync(
        LibraryDbContext context,
        BookEntity book,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken)
    {
        for (var order = 0; order < authors.Count; order++)
        {
            var name = authors[order];
            var normalizedName = Normalize(name);
            var author = context.Authors.Local
                .SingleOrDefault(x => x.NormalizedName == normalizedName)
                ?? await context.Authors
                    .SingleOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
            if (author is null)
            {
                author = new AuthorEntity
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    NormalizedName = normalizedName
                };
                context.Authors.Add(author);
            }

            context.BookAuthors.Add(new BookAuthorEntity
            {
                BookId = book.Id,
                AuthorId = author.Id,
                Order = order
            });
        }
    }

    private static async Task AddTagsAsync(
        LibraryDbContext context,
        BookEntity book,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        if (tags is null)
        {
            return;
        }

        foreach (var name in tags)
        {
            var normalizedName = Normalize(name);
            var tag = context.Tags.Local
                .SingleOrDefault(x => x.NormalizedName == normalizedName)
                ?? await context.Tags
                    .SingleOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
            if (tag is null)
            {
                tag = new TagEntity
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    NormalizedName = normalizedName
                };
                context.Tags.Add(tag);
            }

            context.BookTags.Add(new BookTagEntity
            {
                BookId = book.Id,
                TagId = tag.Id
            });
        }
    }

    private static async Task RemoveOrphanedMetadataAsync(
        LibraryDbContext context,
        CancellationToken cancellationToken)
    {
        var authors = await context.Authors
            .Where(x => !x.BookAuthors.Any())
            .ToListAsync(cancellationToken);
        var tags = await context.Tags
            .Where(x => !x.BookTags.Any())
            .ToListAsync(cancellationToken);
        context.Authors.RemoveRange(authors);
        context.Tags.RemoveRange(tags);
    }

    private static BookEntity ToEntity(Book book)
    {
        var entity = new BookEntity();
        Apply(book, entity);
        return entity;
    }

    private static void Apply(Book book, BookEntity entity)
    {
        entity.Id = book.Id;
        entity.Title = book.Metadata.Title;
        entity.NormalizedTitle = Normalize(book.Metadata.Title);
        entity.Description = book.Metadata.Description;
        entity.Language = book.Metadata.Language;
        entity.Publisher = book.Metadata.Publisher;
        entity.PublicationDate = book.Metadata.PublicationDate;
        entity.Series = book.Metadata.Series;
        entity.SeriesNumber = book.Metadata.SeriesNumber;
        entity.Isbn = book.Metadata.Isbn;
        entity.CoverBytes = book.Metadata.CoverBytes;
        entity.ReadingStatus = book.ReadingStatus;
        entity.CoverRelativePath = book.CoverRelativePath;
        entity.CreatedUtc = book.CreatedUtc;
        entity.UpdatedUtc = book.UpdatedUtc;
    }

    private static BookFileEntity ToEntity(BookFile file) =>
        new()
        {
            Id = file.Id,
            BookId = file.BookId,
            Format = file.Format,
            RelativePath = file.RelativePath,
            Sha256 = file.Sha256,
            SizeBytes = file.SizeBytes,
            WriteBackStatus = file.WriteBackStatus,
            WriteBackMessage = file.WriteBackMessage
        };

    private static Book ToDomain(BookEntity entity) =>
        new(
            entity.Id,
            new BookMetadata(
                entity.Title,
                entity.BookAuthors
                    .OrderBy(x => x.Order)
                    .Select(x => x.Author.Name)
                    .ToList(),
                entity.Description,
                entity.Language,
                entity.Publisher,
                entity.PublicationDate,
                entity.BookTags
                    .Select(x => x.Tag.Name)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList(),
                entity.Series,
                entity.SeriesNumber,
                entity.Isbn,
                entity.CoverBytes),
            entity.ReadingStatus,
            entity.CoverRelativePath,
            entity.CreatedUtc,
            entity.UpdatedUtc);

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
