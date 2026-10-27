using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.Abstractions;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.WpfSmokeTests;

internal sealed class SmokeBookRepository(IReadOnlyList<Book> books) : IBookRepository
{
    public Task<IReadOnlyList<Book>> ListAsync(CancellationToken token) => Task.FromResult(books);
    public Task<Book?> GetAsync(Guid id, CancellationToken token) => Task.FromResult(books.FirstOrDefault(book => book.Id == id));
    public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyList<BookFile>>([]);
    public Task<bool> HasHashAsync(string hash, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> HasNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken token) => throw new NotSupportedException();
    public Task<Book?> FindByNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken token) => throw new NotSupportedException();
    public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(string title, CancellationToken token) => throw new NotSupportedException();
    public Task AddAsync(Book book, BookFile file, CancellationToken token) => throw new NotSupportedException();
    public Task AddFileAsync(BookFile file, CancellationToken token) => throw new NotSupportedException();
    public Task AttachFilesToBookAsync(Guid source, Guid target, CancellationToken token) => throw new NotSupportedException();
    public Task UpdateAsync(Book book, CancellationToken token) => throw new NotSupportedException();
    public Task DeleteAsync(Guid id, CancellationToken token) => throw new NotSupportedException();
    public Task<BookFileDeleteRepositoryResult> DeleteFileAsync(Guid book, Guid file, CancellationToken token) => throw new NotSupportedException();
    public Task UpdateFileWriteBackAsync(Guid id, MetadataWriteResult result, CancellationToken token) => throw new NotSupportedException();
}

internal sealed class SmokeInteraction : IUserInteractionService
{
    public Task<IReadOnlyList<string>> PickBookFilesAsync(CancellationToken token) => throw new NotSupportedException();
    public Task<string?> PickScanFolderAsync(CancellationToken token) => throw new NotSupportedException();
    public Task<string?> PickLibraryDirectoryAsync(string title, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ConfirmDeleteAsync(string title, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ConfirmDeleteViewAsync(string title, CancellationToken token) => throw new NotSupportedException();
    public Task<string?> PromptTextAsync(string title, string message, string initial, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ConfirmMetadataValueRemovalAsync(string value, int count, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ConfirmLanguageNormalizationAsync(int count, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ConfirmQualityIssuesCorrectAsync(int count, CancellationToken token) => throw new NotSupportedException();
    public Task ShowMessageAsync(string title, string message, CancellationToken token) => throw new NotSupportedException();
    public Task ShowImportResultAsync(ImportResultViewModel result, CancellationToken token) => throw new NotSupportedException();
    public Task<Guid?> PickImportRunAsync(ImportHistoryViewModel history, CancellationToken token) => throw new NotSupportedException();
    public Task ShowDuplicateCandidatesAsync(DuplicateCandidatesViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task ShowDuplicateExclusionsAsync(DuplicateExclusionsViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task ShowMetadataQualityExclusionsAsync(MetadataQualityExclusionsViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<Guid?> ShowMetadataQualityDashboardAsync(MetadataQualityDashboardViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ShowMetadataQualityAuthorRepairAsync(MetadataQualityAuthorRepairViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ShowMetadataQualityLanguageRepairAsync(MetadataQualityLanguageRepairViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ShowMetadataQualitySeriesRepairAsync(MetadataQualitySeriesRepairViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ShowMetadataQualityTitleAuthorRepairAsync(MetadataQualityTitleAuthorRepairViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ShowMetadataQualityTagRepairAsync(MetadataQualityTagRepairViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<bool> ShowMetadataQualityCoverSearchAsync(MetadataQualityCoverSearchViewModel view, CancellationToken token) => throw new NotSupportedException();
    public Task<MetadataMultiEditResult?> ShowMetadataMultiEditAsync(MetadataMultiEditViewModel view, CancellationToken token) => Task.FromResult<MetadataMultiEditResult?>(null);
}
