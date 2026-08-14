using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Infrastructure.Metadata;
using EbookManager.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace EbookManager.Infrastructure.Persistence.Repositories;

public sealed class EfBookRepository(
    LibraryDbContextFactory contextFactory,
    string libraryPath) : IBookRepository, IBookDuplicateSnapshotRepository, IBookPagedRepository, IBookBulkMetadataRepository
{
    private const int SqliteParameterChunkSize = 500;

    public async Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var books = await ListProjectionQuery(context)
            .ToListAsync(cancellationToken);
        var formatsByBookId = await ListFormatsAsync(context, books.Select(book => book.Id).ToList(), cancellationToken);
        return books.Select(book => ToDomain(book, formatsByBookId)).ToList().AsReadOnly();
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        return await context.Books
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> ListPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        await using var context = contextFactory.Create(libraryPath);
        var books = await ListProjectionQuery(context)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        var formatsByBookId = await ListFormatsAsync(context, books.Select(book => book.Id).ToList(), cancellationToken);
        return books.Select(book => ToDomain(book, formatsByBookId)).ToList().AsReadOnly();
    }

    public async Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var book = await BooksWithMetadata(context)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return book is null ? null : ToDomain(book, includeCoverBytes: true);
    }

    public async Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken)
    {
        var canonicalSha256 = CanonicalizeSha256(sha256);
        await using var context = contextFactory.Create(libraryPath);
        return await context.BookFiles.AnyAsync(x => x.Sha256 == canonicalSha256, cancellationToken);
    }

    public async Task<bool> HasNormalizedTitleAndAuthorAsync(
        string title,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken)
    {
        var duplicateKey = DuplicateKeyNormalizer.BuildDuplicateKey(title, authors);
        await using var context = contextFactory.Create(libraryPath);
        return await context.Books
            .AsNoTracking()
            .AnyAsync(x => x.DuplicateKey == duplicateKey, cancellationToken);
    }

    public async Task<Book?> FindByNormalizedTitleAndAuthorAsync(
        string title,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken)
    {
        var duplicateKey = DuplicateKeyNormalizer.BuildDuplicateKey(title, authors);
        await using var context = contextFactory.Create(libraryPath);
        var book = await BooksWithMetadata(context)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.DuplicateKey == duplicateKey, cancellationToken);
        return book is null ? null : ToDomain(book, includeCoverBytes: true);
    }

    public async Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(
        string title,
        CancellationToken cancellationToken)
    {
        var normalizedTitle = Normalize(title);
        await using var context = contextFactory.Create(libraryPath);
        var books = await BooksWithMetadata(context)
            .AsNoTracking()
            .Where(x => x.NormalizedTitle == normalizedTitle)
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return books.Select(book => ToDomain(book, includeCoverBytes: false)).ToList().AsReadOnly();
    }

    public async Task<BookDuplicateSnapshot> CreateDuplicateSnapshotAsync(CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var hashRows = await context.BookFiles
            .AsNoTracking()
            .Select(x => new { x.Sha256, x.BookId })
            .ToListAsync(cancellationToken);
        var duplicateKeys = await context.Books
            .AsNoTracking()
            .Select(x => x.DuplicateKey)
            .ToListAsync(cancellationToken);

        return new BookDuplicateSnapshot(
            hashRows.Select(row => row.Sha256).ToHashSet(StringComparer.Ordinal),
            duplicateKeys.ToHashSet(StringComparer.Ordinal))
        {
            BookIdsByFileHash = hashRows
                .GroupBy(row => row.Sha256, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().BookId, StringComparer.Ordinal)
        };
    }

    public async Task AddAsync(
        Book book,
        BookFile file,
        CancellationToken cancellationToken)
    {
        var fileEntity = ToEntity(file);
        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = ToEntity(book);
        context.Books.Add(entity);
        await AddAuthorsAsync(context, entity, book.Metadata.Authors, cancellationToken);
        await AddTagsAsync(context, entity, book.Metadata.Tags, cancellationToken);
        entity.Files.Add(fileEntity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddFileAsync(BookFile file, CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var bookExists = await context.Books
            .AnyAsync(x => x.Id == file.BookId, cancellationToken);
        if (!bookExists)
        {
            throw new KeyNotFoundException($"Book '{file.BookId}' does not exist.");
        }

        context.BookFiles.Add(ToEntity(file));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AttachFilesToBookAsync(
        Guid sourceBookId,
        Guid targetBookId,
        CancellationToken cancellationToken)
    {
        if (sourceBookId == targetBookId)
        {
            return;
        }

        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var sourceBook = await context.Books
            .Include(x => x.Files)
            .SingleOrDefaultAsync(x => x.Id == sourceBookId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source book '{sourceBookId}' does not exist.");
        var targetExists = await context.Books
            .AnyAsync(x => x.Id == targetBookId, cancellationToken);
        if (!targetExists)
        {
            throw new KeyNotFoundException($"Target book '{targetBookId}' does not exist.");
        }

        var previousAuthorIds = await context.BookAuthors
            .Where(x => x.BookId == sourceBookId)
            .Select(x => x.AuthorId)
            .ToListAsync(cancellationToken);
        var previousTagIds = await context.BookTags
            .Where(x => x.BookId == sourceBookId)
            .Select(x => x.TagId)
            .ToListAsync(cancellationToken);

        foreach (var file in sourceBook.Files)
        {
            file.BookId = targetBookId;
        }

        await context.ImportItems
            .Where(x => x.BookId == sourceBookId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.BookId, targetBookId),
                cancellationToken);
        context.Books.Remove(sourceBook);
        await context.SaveChangesAsync(cancellationToken);
        await RemoveOrphanedMetadataAsync(context, previousAuthorIds, previousTagIds, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = contextFactory.Create(libraryPath);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var entity = await context.Books
                .SingleOrDefaultAsync(x => x.Id == book.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Book '{book.Id}' does not exist.");
            var previousAuthorIds = await context.BookAuthors
                .Where(x => x.BookId == book.Id)
                .Select(x => x.AuthorId)
                .ToListAsync(cancellationToken);
            var previousTagIds = await context.BookTags
                .Where(x => x.BookId == book.Id)
                .Select(x => x.TagId)
                .ToListAsync(cancellationToken);

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
            await RemoveOrphanedMetadataAsync(context, previousAuthorIds, previousTagIds, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsDuplicateKeyViolation(exception))
        {
            throw new BookConflictException();
        }
    }

    public async Task<int> UpdateScalarMetadataAsync(
        IReadOnlyCollection<Guid> bookIds,
        BookScalarMetadataField field,
        string? value,
        CancellationToken cancellationToken)
    {
        if (bookIds.Count == 0)
        {
            return 0;
        }

        var total = 0;
        var updatedUtc = DateTimeOffset.UtcNow;
        await using var context = contextFactory.Create(libraryPath);
        foreach (var batch in bookIds.Chunk(SqliteParameterChunkSize))
        {
            var ids = batch;
            total += field switch
            {
                BookScalarMetadataField.Series => await context.Books
                    .Where(book => ids.Contains(book.Id))
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(book => book.Series, value)
                            .SetProperty(book => book.UpdatedUtc, updatedUtc),
                        cancellationToken),
                BookScalarMetadataField.Language => await context.Books
                    .Where(book => ids.Contains(book.Id))
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(book => book.Language, value)
                            .SetProperty(book => book.UpdatedUtc, updatedUtc),
                        cancellationToken),
                _ => 0
            };
        }

        return total;
    }

    public async Task<int> UpdateListMetadataAsync(
        IReadOnlyCollection<Book> books,
        BookListMetadataField field,
        CancellationToken cancellationToken)
    {
        if (field is not (BookListMetadataField.Authors or BookListMetadataField.Tags))
        {
            throw new ArgumentOutOfRangeException(nameof(field), field, "Unsupported list metadata field.");
        }

        try
        {
            if (books.Count == 0)
            {
                return 0;
            }

            var booksById = books
                .GroupBy(book => book.Id)
                .ToDictionary(group => group.Key, group => group.Last());
            var bookIds = booksById.Keys.ToArray();
            var updatedUtc = DateTimeOffset.UtcNow;
            await using var context = contextFactory.Create(libraryPath);
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var existingBookIds = new HashSet<Guid>();
            foreach (var batch in bookIds.Chunk(SqliteParameterChunkSize))
            {
                var ids = batch;
                var existingIds = await context.Books
                    .Where(book => ids.Contains(book.Id))
                    .Select(book => book.Id)
                    .ToListAsync(cancellationToken);
                foreach (var id in existingIds)
                {
                    existingBookIds.Add(id);
                }
            }

            if (existingBookIds.Count == 0)
            {
                return 0;
            }

            var previousAuthorIds = new List<Guid>();
            var previousTagIds = new List<Guid>();
            foreach (var batch in existingBookIds.Chunk(SqliteParameterChunkSize))
            {
                var ids = batch;
                if (field == BookListMetadataField.Authors)
                {
                    previousAuthorIds.AddRange(await context.BookAuthors
                        .Where(bookAuthor => ids.Contains(bookAuthor.BookId))
                        .Select(bookAuthor => bookAuthor.AuthorId)
                        .ToListAsync(cancellationToken));
                    await context.BookAuthors
                        .Where(bookAuthor => ids.Contains(bookAuthor.BookId))
                        .ExecuteDeleteAsync(cancellationToken);
                }
                else
                {
                    previousTagIds.AddRange(await context.BookTags
                        .Where(bookTag => ids.Contains(bookTag.BookId))
                        .Select(bookTag => bookTag.TagId)
                        .ToListAsync(cancellationToken));
                    await context.BookTags
                        .Where(bookTag => ids.Contains(bookTag.BookId))
                        .ExecuteDeleteAsync(cancellationToken);
                }
            }

            context.ChangeTracker.Clear();
            if (field == BookListMetadataField.Authors)
            {
                var authorNamesByBookId = booksById
                    .Where(item => existingBookIds.Contains(item.Key))
                    .ToDictionary(item => item.Key, item => NormalizeMetadataNames(item.Value.Metadata.Authors));
                var authorsByNormalizedName = await LoadOrCreateAuthorsAsync(
                    context,
                    authorNamesByBookId.Values.SelectMany(values => values),
                    cancellationToken);
                foreach (var (bookId, authors) in authorNamesByBookId)
                {
                    AddBookAuthors(context, bookId, authors, authorsByNormalizedName);
                }

                foreach (var batch in existingBookIds.Chunk(SqliteParameterChunkSize))
                {
                    var ids = batch;
                    var entities = await context.Books
                        .Where(book => ids.Contains(book.Id))
                        .ToListAsync(cancellationToken);
                    foreach (var entity in entities)
                    {
                        var book = booksById[entity.Id];
                        entity.DuplicateKey = DuplicateKeyNormalizer.BuildDuplicateKey(
                            book.Metadata.Title,
                            book.Metadata.Authors);
                        entity.UpdatedUtc = updatedUtc;
                    }
                }
            }
            else
            {
                var tagNamesByBookId = booksById
                    .Where(item => existingBookIds.Contains(item.Key))
                    .ToDictionary(item => item.Key, item => NormalizeMetadataNames(item.Value.Metadata.Tags));
                var tagsByNormalizedName = await LoadOrCreateTagsAsync(
                    context,
                    tagNamesByBookId.Values.SelectMany(values => values),
                    cancellationToken);
                foreach (var (bookId, tags) in tagNamesByBookId)
                {
                    AddBookTags(context, bookId, tags, tagsByNormalizedName);
                }

                foreach (var batch in existingBookIds.Chunk(SqliteParameterChunkSize))
                {
                    var ids = batch;
                    await context.Books
                        .Where(book => ids.Contains(book.Id))
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(book => book.UpdatedUtc, updatedUtc),
                            cancellationToken);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            if (field == BookListMetadataField.Authors)
            {
                await RemoveOrphanedMetadataAsync(
                    context,
                    previousAuthorIds.Distinct().ToArray(),
                    [],
                    cancellationToken);
            }
            else
            {
                await RemoveOrphanedMetadataAsync(
                    context,
                    [],
                    previousTagIds.Distinct().ToArray(),
                    cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return existingBookIds.Count;
        }
        catch (Exception exception) when (IsDuplicateKeyViolation(exception))
        {
            throw new BookConflictException();
        }
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

    public async Task<BookFileDeleteRepositoryResult> DeleteFileAsync(
        Guid bookId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await context.BookFiles
            .SingleOrDefaultAsync(x => x.Id == fileId && x.BookId == bookId, cancellationToken);
        if (entity is null)
        {
            return new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.NotFound);
        }

        var fileCount = await context.BookFiles
            .CountAsync(x => x.BookId == bookId, cancellationToken);
        if (fileCount <= 1)
        {
            return new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.LastFormat);
        }

        context.BookFiles.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.Deleted);
    }

    public async Task<IReadOnlyList<BookFile>> ListFilesAsync(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var files = await context.BookFiles
            .AsNoTracking()
            .Where(x => x.BookId == bookId)
            .OrderBy(x => x.RelativePath)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return files.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task UpdateFileWriteBackAsync(
        Guid fileId,
        MetadataWriteResult result,
        CancellationToken cancellationToken)
    {
        await using var context = contextFactory.Create(libraryPath);
        var entity = await context.BookFiles
            .SingleOrDefaultAsync(x => x.Id == fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Book file '{fileId}' does not exist.");

        entity.WriteBackStatus = result.Status;
        entity.WriteBackMessage = result.Message;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<BookEntity> BooksWithMetadata(LibraryDbContext context) =>
        context.Books
            .Include(x => x.BookAuthors)
                .ThenInclude(x => x.Author)
            .Include(x => x.BookTags)
                .ThenInclude(x => x.Tag)
            .Include(x => x.Files);

    private static IQueryable<BookListProjection> ListProjectionQuery(LibraryDbContext context) =>
        context.Books
            .AsNoTracking()
            .OrderBy(x => x.NormalizedTitle)
            .ThenBy(x => x.Id)
            .Select(x => new BookListProjection(
                x.Id,
                x.Title,
                x.Description,
                x.Language,
                x.Publisher,
                x.PublicationDate,
                x.Series,
                x.SeriesNumber,
                x.Isbn,
                x.ReadingStatus,
                x.CoverRelativePath,
                x.CreatedUtc,
                x.UpdatedUtc,
                x.BookAuthors
                    .OrderBy(bookAuthor => bookAuthor.Order)
                    .Select(bookAuthor => bookAuthor.Author.Name)
                    .ToList(),
                x.BookTags
                    .OrderBy(bookTag => bookTag.Order)
                    .Select(bookTag => bookTag.Tag.Name)
                    .ToList()));

    private static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<EbookFormat>>> ListFormatsAsync(
        LibraryDbContext context,
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken)
    {
        if (bookIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<EbookFormat>>();
        }

        var rows = await context.BookFiles
            .AsNoTracking()
            .Where(file => bookIds.Contains(file.BookId))
            .Select(file => new { file.BookId, file.Format })
            .Distinct()
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.BookId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<EbookFormat>)group
                    .Select(row => row.Format)
                    .OrderBy(format => format)
                    .ToList());
    }

    private static async Task AddAuthorsAsync(
        LibraryDbContext context,
        BookEntity book,
        IReadOnlyList<string> authors,
        CancellationToken cancellationToken)
    {
        var normalizedAuthors = NormalizeMetadataNames(authors);
        var normalizedNames = normalizedAuthors
            .Select(x => x.NormalizedName)
            .ToList();
        var existingAuthors = normalizedNames.Count == 0
            ? new Dictionary<string, AuthorEntity>(StringComparer.Ordinal)
            : await context.Authors
                .Where(x => normalizedNames.Contains(x.NormalizedName))
                .ToDictionaryAsync(x => x.NormalizedName, StringComparer.Ordinal, cancellationToken);

        for (var order = 0; order < normalizedAuthors.Count; order++)
        {
            var (name, normalizedName) = normalizedAuthors[order];
            if (!existingAuthors.TryGetValue(normalizedName, out var author))
            {
                author = new AuthorEntity
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    NormalizedName = normalizedName
                };
                context.Authors.Add(author);
                existingAuthors.Add(normalizedName, author);
            }
            else
            {
                author.Name = name;
            }

            context.BookAuthors.Add(new BookAuthorEntity
            {
                BookId = book.Id,
                AuthorId = author.Id,
                Order = order
            });
        }
    }

    private static async Task<Dictionary<string, AuthorEntity>> LoadOrCreateAuthorsAsync(
        LibraryDbContext context,
        IEnumerable<NormalizedMetadataName> authorNames,
        CancellationToken cancellationToken)
    {
        var normalizedAuthors = authorNames
            .GroupBy(author => author.NormalizedName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        var normalizedNames = normalizedAuthors.Select(author => author.NormalizedName).ToArray();
        var existingAuthors = new Dictionary<string, AuthorEntity>(StringComparer.Ordinal);
        foreach (var batch in normalizedNames.Chunk(SqliteParameterChunkSize))
        {
            var names = batch;
            var authors = await context.Authors
                .Where(author => names.Contains(author.NormalizedName))
                .ToListAsync(cancellationToken);
            foreach (var author in authors)
            {
                existingAuthors[author.NormalizedName] = author;
            }
        }

        foreach (var (name, normalizedName) in normalizedAuthors)
        {
            if (existingAuthors.TryGetValue(normalizedName, out var author))
            {
                author.Name = name;
                continue;
            }

            author = new AuthorEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                NormalizedName = normalizedName
            };
            context.Authors.Add(author);
            existingAuthors.Add(normalizedName, author);
        }

        return existingAuthors;
    }

    private static void AddBookAuthors(
        LibraryDbContext context,
        Guid bookId,
        IReadOnlyList<NormalizedMetadataName> authors,
        IReadOnlyDictionary<string, AuthorEntity> authorsByNormalizedName)
    {
        for (var order = 0; order < authors.Count; order++)
        {
            var author = authorsByNormalizedName[authors[order].NormalizedName];
            context.BookAuthors.Add(new BookAuthorEntity
            {
                BookId = bookId,
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

        var normalizedTags = NormalizeMetadataNames(tags);
        var normalizedNames = normalizedTags
            .Select(x => x.NormalizedName)
            .ToList();
        var existingTags = normalizedNames.Count == 0
            ? new Dictionary<string, TagEntity>(StringComparer.Ordinal)
            : await context.Tags
                .Where(x => normalizedNames.Contains(x.NormalizedName))
                .ToDictionaryAsync(x => x.NormalizedName, StringComparer.Ordinal, cancellationToken);

        for (var order = 0; order < normalizedTags.Count; order++)
        {
            var (name, normalizedName) = normalizedTags[order];
            if (!existingTags.TryGetValue(normalizedName, out var tag))
            {
                tag = new TagEntity
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    NormalizedName = normalizedName
                };
                context.Tags.Add(tag);
                existingTags.Add(normalizedName, tag);
            }
            else
            {
                tag.Name = name;
            }

            context.BookTags.Add(new BookTagEntity
            {
                BookId = book.Id,
                TagId = tag.Id,
                Order = order
            });
        }
    }

    private static async Task<Dictionary<string, TagEntity>> LoadOrCreateTagsAsync(
        LibraryDbContext context,
        IEnumerable<NormalizedMetadataName> tagNames,
        CancellationToken cancellationToken)
    {
        var normalizedTags = tagNames
            .GroupBy(tag => tag.NormalizedName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        var normalizedNames = normalizedTags.Select(tag => tag.NormalizedName).ToArray();
        var existingTags = new Dictionary<string, TagEntity>(StringComparer.Ordinal);
        foreach (var batch in normalizedNames.Chunk(SqliteParameterChunkSize))
        {
            var names = batch;
            var tags = await context.Tags
                .Where(tag => names.Contains(tag.NormalizedName))
                .ToListAsync(cancellationToken);
            foreach (var tag in tags)
            {
                existingTags[tag.NormalizedName] = tag;
            }
        }

        foreach (var (name, normalizedName) in normalizedTags)
        {
            if (existingTags.TryGetValue(normalizedName, out var tag))
            {
                tag.Name = name;
                continue;
            }

            tag = new TagEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                NormalizedName = normalizedName
            };
            context.Tags.Add(tag);
            existingTags.Add(normalizedName, tag);
        }

        return existingTags;
    }

    private static void AddBookTags(
        LibraryDbContext context,
        Guid bookId,
        IReadOnlyList<NormalizedMetadataName> tags,
        IReadOnlyDictionary<string, TagEntity> tagsByNormalizedName)
    {
        for (var order = 0; order < tags.Count; order++)
        {
            var tag = tagsByNormalizedName[tags[order].NormalizedName];
            context.BookTags.Add(new BookTagEntity
            {
                BookId = bookId,
                TagId = tag.Id,
                Order = order
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

    private static async Task RemoveOrphanedMetadataAsync(
        LibraryDbContext context,
        IReadOnlyList<Guid> authorIds,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        foreach (var batch in authorIds.Chunk(SqliteParameterChunkSize))
        {
            var ids = batch;
            var authors = await context.Authors
                .Where(x => ids.Contains(x.Id) && !x.BookAuthors.Any())
                .ToListAsync(cancellationToken);
            context.Authors.RemoveRange(authors);
        }

        foreach (var batch in tagIds.Chunk(SqliteParameterChunkSize))
        {
            var ids = batch;
            var tags = await context.Tags
                .Where(x => ids.Contains(x.Id) && !x.BookTags.Any())
                .ToListAsync(cancellationToken);
            context.Tags.RemoveRange(tags);
        }
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
        entity.DuplicateKey = DuplicateKeyNormalizer.BuildDuplicateKey(book.Metadata.Title, book.Metadata.Authors);
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
            Sha256 = CanonicalizeSha256(file.Sha256),
            SizeBytes = file.SizeBytes,
            WriteBackStatus = file.WriteBackStatus,
            WriteBackMessage = file.WriteBackMessage
        };

    private static Book ToDomain(BookEntity entity, bool includeCoverBytes) =>
        new(
            entity.Id,
            new BookMetadata(
                entity.Title,
                entity.BookAuthors
                    .OrderBy(x => x.Order)
                    .Select(x => x.Author.Name)
                    .ToList(),
                BookMetadataCleaner.CleanDescription(entity.Description),
                entity.Language,
                entity.Publisher,
                entity.PublicationDate,
                entity.BookTags
                    .OrderBy(x => x.Order)
                    .Select(x => x.Tag.Name)
                    .ToList(),
                entity.Series,
                entity.SeriesNumber,
                entity.Isbn,
                includeCoverBytes ? entity.CoverBytes : null),
            entity.ReadingStatus,
            entity.CoverRelativePath,
            entity.CreatedUtc,
            entity.UpdatedUtc)
        {
            Formats = entity.Files
                .Select(file => file.Format)
                .Distinct()
                .OrderBy(format => format)
                .ToList()
        };

    private static Book ToDomain(
        BookListProjection projection,
        IReadOnlyDictionary<Guid, IReadOnlyList<EbookFormat>> formatsByBookId)
    {
        formatsByBookId.TryGetValue(projection.Id, out var formats);
        return new Book(
            projection.Id,
            new BookMetadata(
                projection.Title,
                projection.Authors,
                BookMetadataCleaner.CleanDescription(projection.Description),
                projection.Language,
                projection.Publisher,
                projection.PublicationDate,
                projection.Tags,
                projection.Series,
                projection.SeriesNumber,
                projection.Isbn),
            projection.ReadingStatus,
            projection.CoverRelativePath,
            projection.CreatedUtc,
            projection.UpdatedUtc)
        {
            Formats = formats ?? []
        };
    }

    private static BookFile ToDomain(BookFileEntity entity) =>
        new(
            entity.Id,
            entity.BookId,
            entity.Format,
            entity.RelativePath,
            entity.Sha256,
            entity.SizeBytes,
            entity.WriteBackStatus,
            entity.WriteBackMessage);

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static IReadOnlyList<NormalizedMetadataName> NormalizeMetadataNames(
        IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        var normalizedNames = new List<NormalizedMetadataName>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var name = value.Trim();
            if (name.Length == 0)
            {
                continue;
            }

            var normalizedName = Normalize(name);
            if (seen.Add(normalizedName))
            {
                normalizedNames.Add(new NormalizedMetadataName(name, normalizedName));
            }
        }

        return normalizedNames;
    }

    private static string CanonicalizeSha256(string sha256)
    {
        ArgumentNullException.ThrowIfNull(sha256);
        if (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("SHA-256 hashes must contain exactly 64 hexadecimal characters.", nameof(sha256));
        }

        return sha256.ToUpperInvariant();
    }

    private static bool IsDuplicateKeyViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqliteException sqliteException &&
                sqliteException.SqliteErrorCode == 19 &&
                sqliteException.SqliteExtendedErrorCode == 2067)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record NormalizedMetadataName(string Name, string NormalizedName);

    private sealed record BookListProjection(
        Guid Id,
        string Title,
        string? Description,
        string? Language,
        string? Publisher,
        DateOnly? PublicationDate,
        string? Series,
        decimal? SeriesNumber,
        string? Isbn,
        ReadingStatus ReadingStatus,
        string? CoverRelativePath,
        DateTimeOffset CreatedUtc,
        DateTimeOffset UpdatedUtc,
        IReadOnlyList<string> Authors,
        IReadOnlyList<string> Tags);
}
