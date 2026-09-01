using EbookManager.Domain.Importing;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.Presentation.Abstractions;

public interface IUserInteractionService
{
    Task<IReadOnlyList<string>> PickBookFilesAsync(CancellationToken cancellationToken);
    Task<string?> PickScanFolderAsync(CancellationToken cancellationToken);
    Task<string?> PickLibraryDirectoryAsync(string title, CancellationToken cancellationToken);
    Task<bool> ConfirmDeleteAsync(string title, CancellationToken cancellationToken);
    Task<bool> ConfirmDeleteViewAsync(string viewName, CancellationToken cancellationToken);
    Task<string?> PromptTextAsync(
        string title,
        string message,
        string initialValue,
        CancellationToken cancellationToken);
    Task<bool> ConfirmMetadataValueRemovalAsync(
        string value,
        int affectedBookCount,
        CancellationToken cancellationToken);
    Task<bool> ConfirmLanguageNormalizationAsync(
        int affectedBookCount,
        CancellationToken cancellationToken);
    Task ShowMessageAsync(
        string title,
        string message,
        CancellationToken cancellationToken);
    Task ShowImportResultAsync(ImportResultViewModel result, CancellationToken cancellationToken);
    Task<Guid?> PickImportRunAsync(ImportHistoryViewModel history, CancellationToken cancellationToken);
    Task ShowDuplicateCandidatesAsync(DuplicateCandidatesViewModel candidates, CancellationToken cancellationToken);
    Task ShowDuplicateExclusionsAsync(DuplicateExclusionsViewModel exclusions, CancellationToken cancellationToken);
    Task ShowMetadataQualityExclusionsAsync(MetadataQualityExclusionsViewModel exclusions, CancellationToken cancellationToken);
    Task<Guid?> ShowMetadataQualityDashboardAsync(MetadataQualityDashboardViewModel dashboard, CancellationToken cancellationToken);
    Task<bool> ShowMetadataQualityAuthorRepairAsync(
        MetadataQualityAuthorRepairViewModel repair,
        CancellationToken cancellationToken);
    Task<MetadataMultiEditResult?> ShowMetadataMultiEditAsync(
        MetadataMultiEditViewModel edit,
        CancellationToken cancellationToken);
}
