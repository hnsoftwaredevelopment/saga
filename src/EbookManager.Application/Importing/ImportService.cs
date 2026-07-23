using System.Diagnostics;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Importing;
using EbookManager.Domain.Metadata;

namespace EbookManager.Application.Importing;

public sealed class ImportService(
    IBookRepository bookRepository,
    IImportRepository importRepository,
    ILibraryFileStore fileStore,
    IFileHasher hasher,
    IMetadataSourceResolver metadataSourceResolver,
    IImportExceptionClassifier exceptionClassifier) : IImportRunner
{
    private const string InvalidSourceDisplayName = "(invalid source)";
    private const long LargeFileSinglePassCopyThresholdBytes = 16 * 1024 * 1024;

    private readonly IBookRepository bookRepository = bookRepository;
    private readonly IImportRepository importRepository = importRepository;
    private readonly ILibraryFileStore fileStore = fileStore;
    private readonly IFileHasher hasher = hasher;
    private readonly IImportExceptionClassifier exceptionClassifier = exceptionClassifier;
    private readonly IMetadataSourceResolver metadataSourceResolver = metadataSourceResolver;

    public async Task<ImportBatchResult> ImportAsync(
        IReadOnlyList<string> sourcePaths,
        CancellationToken cancellationToken = default) =>
        await ImportAsync(sourcePaths, progress: null, cancellationToken);

    public async Task<ImportBatchResult> ImportAsync(
        IReadOnlyList<string> sourcePaths,
        IProgress<ImportProgress>? progress,
        CancellationToken cancellationToken = default,
        ImportRunContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);

        var startedUtc = DateTimeOffset.UtcNow;
        var runId = await importRepository.StartRunAsync(startedUtc, context, cancellationToken);
        var results = new List<ImportItemResult>(sourcePaths.Count);
        var addedCount = 0;
        var exactDuplicateCount = 0;
        var possibleDuplicateCount = 0;
        var failedCount = 0;
        var wasCancelled = false;
        var duplicateTracker = await ImportDuplicateTracker.CreateAsync(bookRepository, cancellationToken);

        try
        {
            for (var sequence = 0; sequence < sourcePaths.Count; sequence++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = await ImportSingleAsync(runId, sequence, sourcePaths[sequence], duplicateTracker, cancellationToken);
                results.Add(item);
                switch (item.Outcome)
                {
                    case ImportOutcome.Added:
                        addedCount++;
                        break;
                    case ImportOutcome.ExactDuplicate:
                        exactDuplicateCount++;
                        break;
                    case ImportOutcome.PossibleDuplicate:
                        possibleDuplicateCount++;
                        break;
                    case ImportOutcome.Failed:
                        failedCount++;
                        break;
                }

                progress?.Report(new ImportProgress(
                    runId,
                    sourcePaths.Count,
                    results.Count,
                    addedCount,
                    exactDuplicateCount,
                    possibleDuplicateCount,
                    failedCount,
                    item));
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }
        finally
        {
            try
            {
                await importRepository.CompleteRunAsync(runId, DateTimeOffset.UtcNow, CancellationToken.None);
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        return new ImportBatchResult(runId, results, wasCancelled);
    }

    private async Task<ImportItemResult> ImportSingleAsync(
        Guid runId,
        int sequence,
        string? sourcePath,
        ImportDuplicateTracker duplicateTracker,
        CancellationToken cancellationToken)
    {
        var sourceDisplayName = GetSafeSourceDisplayName(sourcePath);
        var stopwatch = Stopwatch.StartNew();
        EbookFormat? resolvedFormat = null;
        long? sourceLength = null;
        ImportItemResult? result = null;
        Guid? cleanupBookId = null;

        Task<ImportItemResult> RecordAsync(ImportItemResult item)
        {
            stopwatch.Stop();
            var diagnostics = new ImportItemDiagnostics(
                stopwatch.Elapsed,
                sourceLength,
                resolvedFormat);
            return RecordAndReturnAsync(
                runId,
                sequence,
                sourceDisplayName,
                item with { Diagnostics = diagnostics },
                cleanupBookId,
                cancellationToken);
        }

        if (sourceDisplayName == InvalidSourceDisplayName)
        {
            result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.InvalidSourcePath);
        }
        else if (!TryResolveFormat(sourceDisplayName, out var format))
        {
            result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.UnsupportedFormat);
        }
        else
        {
            resolvedFormat = format;
            var bookId = Guid.NewGuid();
            var copied = false;
            var bookPersisted = false;
            var shouldCleanup = false;
            var targetBookId = bookId;
            var addFileToExistingBook = false;

            try
            {
                if (!IsSourceLocallyAvailable(sourcePath!))
                {
                    result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.SourceUnreadable);
                    return await RecordAsync(result);
                }

                sourceLength = GetSourceLengthOrNull(sourcePath!);
                if (sourceLength is null)
                {
                    result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.SourceUnreadable);
                    return await RecordAsync(result);
                }

                var hashingFileStore = fileStore as IHashingLibraryFileStore;
                var useSinglePassCopy =
                    hashingFileStore is not null &&
                    sourceLength.Value >= LargeFileSinglePassCopyThresholdBytes;
                string? sha256 = null;

                if (!useSinglePassCopy)
                {
                    try
                    {
                        sha256 = CanonicalizeSha256(await hasher.ComputeSha256Async(sourcePath!, cancellationToken));
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.SourceUnreadable);
                        return await RecordAsync(result);
                    }

                    if (await duplicateTracker.HasHashAsync(sha256, cancellationToken))
                    {
                        result = CreateSuccessResult(
                            sourcePath,
                            ImportOutcome.ExactDuplicate,
                            SafeImportMessages.ExactDuplicate);
                        return await RecordAsync(result);
                    }
                }

                MetadataReadResult metadata;
                try
                {
                    metadata = await metadataSourceResolver.ReadAsync(sourcePath!, format, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.MetadataReadFailed);
                    return await RecordAsync(result);
                }

                var duplicateKey = DuplicateKeyNormalizer.BuildDuplicateKey(
                    metadata.Metadata.Title,
                    metadata.Metadata.Authors);
                var isPossibleDuplicate = false;
                ImportItemSuggestion? possibleTitleMatch = null;
                var duplicate = await duplicateTracker.FindDuplicateAsync(
                        metadata.Metadata.Title,
                        metadata.Metadata.Authors,
                        duplicateKey,
                        cancellationToken);
                if (duplicate is not null)
                {
                    if (duplicate.BookId is null || duplicate.Formats.Contains(format))
                    {
                        if (!useSinglePassCopy)
                        {
                            result = CreateSuccessResult(
                                sourcePath,
                                ImportOutcome.PossibleDuplicate,
                                SafeImportMessages.PossibleDuplicate);
                            return await RecordAsync(result);
                        }

                        isPossibleDuplicate = true;
                    }
                    else
                    {
                        targetBookId = duplicate.BookId.Value;
                        addFileToExistingBook = true;
                    }
                }
                else
                {
                    possibleTitleMatch = await duplicateTracker.FindPossibleTitleMatchAsync(
                        metadata.Metadata.Title,
                        metadata.Metadata.Authors,
                        cancellationToken);
                }

                (string RelativeBookPath, string? RelativeCoverPath) copy;
                try
                {
                    if (useSinglePassCopy)
                    {
                        var hashingCopy = await hashingFileStore!.CopyIntoLibraryWithHashAsync(
                            targetBookId,
                            sourcePath!,
                            metadata.Metadata.CoverBytes,
                            cancellationToken);
                        copy = (hashingCopy.RelativeBookPath, hashingCopy.RelativeCoverPath);
                        sha256 = CanonicalizeSha256(hashingCopy.Sha256);
                    }
                    else
                    {
                        copy = await fileStore.CopyIntoLibraryAsync(
                            targetBookId,
                            sourcePath!,
                            metadata.Metadata.CoverBytes,
                            cancellationToken);
                    }

                    copied = true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.ManagedCopyFailed);
                    return await RecordAsync(result);
                }

                if (useSinglePassCopy && await duplicateTracker.HasHashAsync(sha256!, cancellationToken))
                {
                    result = CreateSuccessResult(
                        sourcePath,
                        ImportOutcome.ExactDuplicate,
                        SafeImportMessages.ExactDuplicate);
                    shouldCleanup = true;
                }
                else if (isPossibleDuplicate)
                {
                    result = CreateSuccessResult(
                        sourcePath,
                        ImportOutcome.PossibleDuplicate,
                        SafeImportMessages.PossibleDuplicate);
                    shouldCleanup = true;
                }
                else
                {
                    var now = DateTimeOffset.UtcNow;
                    var book = new Book(
                        targetBookId,
                        metadata.Metadata,
                        ReadingStatus.Unread,
                        copy.RelativeCoverPath,
                        now,
                        now);
                    var file = new BookFile(
                        Guid.NewGuid(),
                        targetBookId,
                        format,
                        copy.RelativeBookPath,
                        sha256!,
                        sourceLength.Value,
                        MetadataWriteBackStatus.NotAttempted,
                        null);

                    try
                    {
                        if (addFileToExistingBook)
                        {
                            await bookRepository.AddFileAsync(file, cancellationToken);
                        }
                        else
                        {
                            await bookRepository.AddAsync(book, file, cancellationToken);
                        }

                        bookPersisted = true;
                        cleanupBookId = addFileToExistingBook ? null : targetBookId;
                        result = CreateAddedResult(
                            sourcePath,
                            metadata.Warning,
                            targetBookId,
                            possibleTitleMatch);
                        duplicateTracker.Add(sha256!, duplicateKey, targetBookId, format);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception) when (exceptionClassifier.IsDuplicateKeyViolation(exception))
                    {
                        result = CreateSuccessResult(
                            sourcePath,
                            ImportOutcome.PossibleDuplicate,
                            SafeImportMessages.PossibleDuplicate);
                        shouldCleanup = true;
                    }
                    catch
                    {
                        result = CreateFailedResult(sourcePath, sourceDisplayName, SafeImportMessages.ImportFailed);
                        shouldCleanup = true;
                    }
                }
            }
            finally
            {
                Guid? copiedBookCanBeCleaned = addFileToExistingBook ? null : targetBookId;
                if ((shouldCleanup || (!bookPersisted && copied)) && copiedBookCanBeCleaned is { } bookToClean)
                {
                    var cleanupIncomplete = await CleanupImportedBookAsync(bookToClean);
                    if (
                        cleanupIncomplete
                        && result is not null
                        && result.Outcome is ImportOutcome.Failed or ImportOutcome.PossibleDuplicate)
                    {
                        result = result with { Message = AppendCleanupIncomplete(result.Message) };
                    }
                }
            }
        }

        return await RecordAsync(result!);
    }

    private async Task<ImportItemResult> RecordAndReturnAsync(
        Guid runId,
        int sequence,
        string sourceDisplayName,
        ImportItemResult result,
        Guid? cleanupBookId,
        CancellationToken cancellationToken)
    {
        try
        {
            await importRepository.RecordItemAsync(
                runId,
                sequence,
                sourceDisplayName,
                result.Outcome,
                result.Message,
                result.BookId,
                CancellationToken.None,
                result.Diagnostics,
                result.Suggestion);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var cleanupIncomplete = cleanupBookId is { } bookId && await CleanupImportedBookAsync(bookId);
            var message = cleanupIncomplete
                ? $"{SafeImportMessages.CannotPersistResult}; {SafeImportMessages.CleanupIncomplete}"
                : SafeImportMessages.CannotPersistResult;
            throw new ImportPersistenceException(message, exception);
        }
    }

    private static ImportItemResult CreateFailedResult(
        string? sourcePath,
        string sourceDisplayName,
        string message) =>
        new(IsBlank(sourcePath) ? sourceDisplayName : sourcePath!, ImportOutcome.Failed, message);

    private static ImportItemResult CreateSuccessResult(
        string? sourcePath,
        ImportOutcome outcome,
        string message) =>
        new(IsBlank(sourcePath) ? InvalidSourceDisplayName : sourcePath!, outcome, message);

    private static ImportItemResult CreateAddedResult(
        string? sourcePath,
        string? warning,
        Guid bookId,
        ImportItemSuggestion? possibleTitleMatch = null)
    {
        var message = SafeImportMessages.Added;
        if (!string.IsNullOrWhiteSpace(warning))
        {
            message = $"{message}; {SafeImportMessages.MetadataWarning}: {warning}";
        }

        if (possibleTitleMatch is not null)
        {
            message = $"{message}; {SafeImportMessages.PossibleTitleMatch}: {possibleTitleMatch.Title}";
        }

        return new(
            IsBlank(sourcePath) ? InvalidSourceDisplayName : sourcePath!,
            ImportOutcome.Added,
            message,
            bookId,
            Suggestion: possibleTitleMatch);
    }

    private async Task<bool> CleanupImportedBookAsync(Guid bookId)
    {
        var cleanupIncomplete = false;

        try
        {
            await bookRepository.DeleteAsync(bookId, CancellationToken.None);
        }
        catch
        {
            cleanupIncomplete = true;
        }

        try
        {
            await fileStore.DeleteBookDirectoryAsync(bookId, CancellationToken.None);
        }
        catch
        {
            cleanupIncomplete = true;
        }

        return cleanupIncomplete;
    }

    private static bool TryResolveFormat(string sourceDisplayName, out EbookFormat format) =>
        EbookFormatExtensions.TryFromFilename(sourceDisplayName, out format);

    private static string GetSafeSourceDisplayName(string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return InvalidSourceDisplayName;
        }

        try
        {
            var fileName = Path.GetFileName(sourcePath);
            return string.IsNullOrWhiteSpace(fileName) ? InvalidSourceDisplayName : fileName;
        }
        catch
        {
            return InvalidSourceDisplayName;
        }
    }

    private static string AppendCleanupIncomplete(string message) =>
        $"{message}; {SafeImportMessages.CleanupIncomplete}";

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    private static bool IsSourceLocallyAvailable(string sourcePath)
    {
        try
        {
            var attributes = File.GetAttributes(sourcePath);
            return !attributes.HasFlag(FileAttributes.Offline);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or FileNotFoundException or PathTooLongException)
        {
            return false;
        }
    }

    private static long? GetSourceLengthOrNull(string sourcePath)
    {
        try
        {
            return new FileInfo(sourcePath).Length;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or FileNotFoundException or PathTooLongException)
        {
            return null;
        }
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

    private sealed class ImportDuplicateTracker
    {
        private readonly IBookRepository bookRepository;
        private readonly bool snapshotLoaded;
        private readonly HashSet<string> knownHashes;
        private readonly HashSet<string> knownDuplicateKeys;
        private readonly Dictionary<string, DuplicateBookReference> duplicateBooksByKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyList<Book>> titleMatchesByTitle = new(StringComparer.Ordinal);

        private ImportDuplicateTracker(
            IBookRepository bookRepository,
            bool snapshotLoaded,
            IEnumerable<string> knownHashes,
            IEnumerable<string> knownDuplicateKeys)
        {
            this.bookRepository = bookRepository;
            this.snapshotLoaded = snapshotLoaded;
            this.knownHashes = knownHashes.ToHashSet(StringComparer.Ordinal);
            this.knownDuplicateKeys = knownDuplicateKeys.ToHashSet(StringComparer.Ordinal);
        }

        public static async Task<ImportDuplicateTracker> CreateAsync(
            IBookRepository bookRepository,
            CancellationToken cancellationToken)
        {
            if (bookRepository is IBookDuplicateSnapshotRepository snapshotRepository)
            {
                var snapshot = await snapshotRepository.CreateDuplicateSnapshotAsync(cancellationToken);
                return new ImportDuplicateTracker(
                    bookRepository,
                    snapshotLoaded: true,
                    snapshot.FileHashes,
                    snapshot.DuplicateKeys);
            }

            return new ImportDuplicateTracker(
                bookRepository,
                snapshotLoaded: false,
                knownHashes: [],
                knownDuplicateKeys: []);
        }

        public async Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken)
        {
            var canonicalSha256 = CanonicalizeSha256(sha256);
            if (knownHashes.Contains(canonicalSha256))
            {
                return true;
            }

            if (snapshotLoaded)
            {
                return false;
            }

            if (await bookRepository.HasHashAsync(canonicalSha256, cancellationToken))
            {
                knownHashes.Add(canonicalSha256);
                return true;
            }

            return false;
        }

        public async Task<DuplicateBookReference?> FindDuplicateAsync(
            string title,
            IReadOnlyList<string> authors,
            string duplicateKey,
            CancellationToken cancellationToken)
        {
            if (duplicateBooksByKey.TryGetValue(duplicateKey, out var cachedDuplicate))
            {
                return cachedDuplicate;
            }

            if (knownDuplicateKeys.Contains(duplicateKey))
            {
                var knownBook = await bookRepository.FindByNormalizedTitleAndAuthorAsync(title, authors, cancellationToken);
                var knownReference = knownBook is null
                    ? new DuplicateBookReference(null, EbookFormatExtensions.Supported.ToHashSet())
                    : new DuplicateBookReference(
                        knownBook.Id,
                        knownBook.Formats.ToHashSet());
                duplicateBooksByKey[duplicateKey] = knownReference;
                return knownReference;
            }

            if (snapshotLoaded)
            {
                return null;
            }

            var book = await bookRepository.FindByNormalizedTitleAndAuthorAsync(title, authors, cancellationToken);
            if (book is not null)
            {
                knownDuplicateKeys.Add(duplicateKey);
                var reference = new DuplicateBookReference(
                    book.Id,
                    book.Formats.ToHashSet());
                duplicateBooksByKey[duplicateKey] = reference;
                return reference;
            }

            return null;
        }

        public async Task<ImportItemSuggestion?> FindPossibleTitleMatchAsync(
            string title,
            IReadOnlyList<string> authors,
            CancellationToken cancellationToken)
        {
            var normalizedTitle = DuplicateKeyNormalizer.NormalizeSqliteText(title);
            if (normalizedTitle.Length == 0)
            {
                return null;
            }

            if (!titleMatchesByTitle.TryGetValue(normalizedTitle, out var matches))
            {
                matches = await bookRepository.FindByNormalizedTitleAsync(title, cancellationToken);
                titleMatchesByTitle[normalizedTitle] = matches;
            }

            var match = matches.FirstOrDefault();
            return match is null
                ? null
                : new ImportItemSuggestion(
                    ImportItemSuggestionKind.TitleMatch,
                    match.Id,
                    match.Metadata.Title,
                    string.Join("; ", match.Metadata.Authors));
        }

        public void Add(string sha256, string duplicateKey, Guid bookId, EbookFormat format)
        {
            knownHashes.Add(CanonicalizeSha256(sha256));
            knownDuplicateKeys.Add(duplicateKey);
            if (!duplicateBooksByKey.TryGetValue(duplicateKey, out var reference) || reference.BookId != bookId)
            {
                duplicateBooksByKey[duplicateKey] = new DuplicateBookReference(bookId, [format]);
                return;
            }

            reference.Formats.Add(format);
        }
    }

    private sealed record DuplicateBookReference(Guid? BookId, HashSet<EbookFormat> Formats);

    private static class SafeImportMessages
    {
        public const string Added = "added";
        public const string CleanupIncomplete = "cleanup incomplete";
        public const string ExactDuplicate = "exact duplicate skipped";
        public const string ImportFailed = "import failed";
        public const string CannotPersistResult = "cannot persist result";
        public const string InvalidSourcePath = "invalid source path";
        public const string ManagedCopyFailed = "managed copy failed";
        public const string MetadataReadFailed = "metadata read failed";
        public const string MetadataWarning = "metadata warning";
        public const string PossibleTitleMatch = "possible title match";
        public const string PossibleDuplicate = "possible duplicate";
        public const string SourceUnreadable = "source unreadable; make sure the file is available locally";
        public const string UnsupportedFormat = "unsupported format";
    }
}
