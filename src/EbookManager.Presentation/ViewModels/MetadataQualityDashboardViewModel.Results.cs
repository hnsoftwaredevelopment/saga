using System.Collections.ObjectModel;
using System.Globalization;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class MetadataQualityDashboardViewModel
{
    public ObservableCollection<MetadataQualityRepairDetailViewModel> RepairDetails { get; } = [];
    public bool HasRepairDetails => RepairDetails.Count > 0;

    private void ClearRepairFeedback()
    {
        RepairDetails.Clear();
        OnPropertyChanged(nameof(HasRepairDetails));
        StatusMessage = null;
    }

    private void AddRepairDetail(MetadataQualityBookRowViewModel row,
        MetadataQualityBatchRepairStatus status,
        MetadataQualityRepairFailureReason reason = MetadataQualityRepairFailureReason.Unknown,
        string? reasonKey = null)
    {
        var key = reasonKey ?? status switch
        {
            MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors => "MetadataQualityRepairReasonFileWarning",
            MetadataQualityBatchRepairStatus.NotFound => "MetadataQualityRepairReasonUnavailable",
            MetadataQualityBatchRepairStatus.NotApplicable => "MetadataQualityRepairReasonChanged",
            _ => reason switch
            {
                MetadataQualityRepairFailureReason.BookAlreadyExists => "MetadataQualityRepairReasonDuplicate",
                MetadataQualityRepairFailureReason.MetadataChanged => "MetadataQualityRepairReasonChanged",
                _ => "MetadataQualityRepairReasonFailed"
            }
        };
        RepairDetails.Add(new(row.Title, row.Authors, localize(key)));
        OnPropertyChanged(nameof(HasRepairDetails));
    }

    private void SetRepairFailureDetails(IEnumerable<MetadataQualityBookRowViewModel> rows,
        string? reasonKey = null)
    {
        ClearRepairFeedback();
        foreach (var row in rows)
        {
            AddRepairDetail(row, MetadataQualityBatchRepairStatus.Failed, reasonKey: reasonKey);
        }
    }

    private void ReconcileBatchRepair(
        MetadataQualityIssueViewModel issue,
        IReadOnlyList<MetadataQualityBookRowViewModel> selectedRows,
        IReadOnlyList<MetadataQualityBatchRepairItem> items,
        string bulkSucceededKey,
        string bulkResultKey,
        string failedKey,
        string writeBackWarningKey,
        string notNeededKey,
        bool isBulk = false)
    {
        var showFeedback = ReferenceEquals(SelectedIssue, issue);
        if (showFeedback)
        {
            ClearRepairFeedback();
        }
        var results = items
            .GroupBy(item => item.BookId)
            .ToDictionary(group => group.Key, group => group.Last());
        foreach (var row in selectedRows)
        {
            var item = results.GetValueOrDefault(row.Id);
            if (showFeedback && item?.Status != MetadataQualityBatchRepairStatus.Succeeded)
            {
                AddRepairDetail(row, item?.Status ?? MetadataQualityBatchRepairStatus.Failed,
                    item?.FailureReason ?? MetadataQualityRepairFailureReason.Unknown);
            }
        }

        var firstSelectedIndex = selectedRows
            .Select(issue.Rows.IndexOf)
            .Where(index => index >= 0)
            .DefaultIfEmpty(0)
            .Min();
        var failedBookIds = new HashSet<Guid>();
        var succeeded = 0;
        var writeBackWarnings = 0;
        var notApplicable = 0;
        var notFound = 0;
        var reconciledBooks = new List<Book>();

        foreach (var row in selectedRows)
        {
            if (!results.TryGetValue(row.Id, out var result))
            {
                failedBookIds.Add(row.Id);
                continue;
            }

            switch (result.Status)
            {
                case MetadataQualityBatchRepairStatus.Succeeded when result.Book is not null:
                    succeeded++;
                    ReconcileBook(result.Book, notifyLibrary: false);
                    reconciledBooks.Add(result.Book);
                    break;
                case MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors when result.Book is not null:
                    writeBackWarnings++;
                    ReconcileBook(result.Book, notifyLibrary: false);
                    reconciledBooks.Add(result.Book);
                    break;
                case MetadataQualityBatchRepairStatus.NotApplicable when result.Book is not null:
                    notApplicable++;
                    ReconcileBook(result.Book, notifyLibrary: false);
                    reconciledBooks.Add(result.Book);
                    break;
                case MetadataQualityBatchRepairStatus.NotFound:
                    notFound++;
                    RemoveBook(row.Id);
                    break;
                default:
                    failedBookIds.Add(row.Id);
                    if (result.Book is not null)
                    {
                        ReconcileBook(result.Book, notifyLibrary: false);
                        reconciledBooks.Add(result.Book);
                    }
                    break;
            }
        }

        if (reconciledBooks.Count > 0)
        {
            booksRepaired?.Invoke(reconciledBooks);
        }

        if (!showFeedback)
        {
            return;
        }

        var failedRows = issue.Rows.Where(row => failedBookIds.Contains(row.Id)).ToArray();
        SetSelectedBooks(failedRows.Length > 0
            ? failedRows
            : issue.Rows.Count == 0
                ? []
                : [issue.Rows[Math.Min(firstSelectedIndex, issue.Rows.Count - 1)]]);

        if (selectedRows.Count > 1 || isBulk)
        {
            if (succeeded == selectedRows.Count &&
                writeBackWarnings == 0 &&
                notApplicable == 0 &&
                notFound == 0 &&
                failedBookIds.Count == 0)
            {
                StatusMessage = string.Format(
                    CultureInfo.CurrentCulture,
                    localize(bulkSucceededKey),
                    succeeded);
                IsStatusMessageSuccess = true;
            }
            else
            {
                StatusMessage = string.Format(
                    CultureInfo.CurrentCulture,
                    localize(bulkResultKey),
                    succeeded,
                    writeBackWarnings,
                    notApplicable,
                    notFound,
                    failedBookIds.Count);
            }

            return;
        }

        var singleResult = results.GetValueOrDefault(selectedRows[0].Id);
        StatusMessage = singleResult?.Status switch
        {
            MetadataQualityBatchRepairStatus.Succeeded when succeeded == 1 => null,
            MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors when writeBackWarnings == 1 =>
                localize(writeBackWarningKey),
            MetadataQualityBatchRepairStatus.NotApplicable when notApplicable == 1 =>
                localize(notNeededKey),
            MetadataQualityBatchRepairStatus.NotFound when notFound == 1 =>
                localize("MetadataQualityBookUnavailableMessage"),
            _ => localize(failedKey)
        };
    }

    private static MetadataQualityBatchRepairItem ToBatchRepairItem(
        MetadataQualityAuthorRepairItemResult item) =>
        new(item.BookId, item.Status switch
        {
            MetadataQualityAuthorRepairStatus.Succeeded => MetadataQualityBatchRepairStatus.Succeeded,
            MetadataQualityAuthorRepairStatus.SavedWithWriteBackErrors =>
                MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors,
            MetadataQualityAuthorRepairStatus.NotApplicable => MetadataQualityBatchRepairStatus.NotApplicable,
            MetadataQualityAuthorRepairStatus.NotFound => MetadataQualityBatchRepairStatus.NotFound,
            _ => MetadataQualityBatchRepairStatus.Failed
        }, item.Book, item.FailureReason);

    private static MetadataQualityBatchRepairItem ToBatchRepairItem(
        MetadataQualityLanguageRepairItemResult item) =>
        new(item.BookId, item.Status switch
        {
            MetadataQualityLanguageRepairStatus.Succeeded => MetadataQualityBatchRepairStatus.Succeeded,
            MetadataQualityLanguageRepairStatus.SavedWithWriteBackErrors =>
                MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors,
            MetadataQualityLanguageRepairStatus.NotApplicable => MetadataQualityBatchRepairStatus.NotApplicable,
            MetadataQualityLanguageRepairStatus.NotFound => MetadataQualityBatchRepairStatus.NotFound,
            _ => MetadataQualityBatchRepairStatus.Failed
        }, item.Book, item.FailureReason);

    private static MetadataQualityBatchRepairItem ToBatchRepairItem(
        MetadataQualitySeriesRepairItemResult item) =>
        new(item.BookId, item.Status switch
        {
            MetadataQualitySeriesRepairStatus.Succeeded => MetadataQualityBatchRepairStatus.Succeeded,
            MetadataQualitySeriesRepairStatus.SavedWithWriteBackErrors =>
                MetadataQualityBatchRepairStatus.SavedWithWriteBackErrors,
            MetadataQualitySeriesRepairStatus.NotApplicable => MetadataQualityBatchRepairStatus.NotApplicable,
            MetadataQualitySeriesRepairStatus.NotFound => MetadataQualityBatchRepairStatus.NotFound,
            _ => MetadataQualityBatchRepairStatus.Failed
        }, item.Book, item.FailureReason);

    private sealed record MetadataQualityBatchRepairItem(
        Guid BookId,
        MetadataQualityBatchRepairStatus Status,
        Book? Book,
        MetadataQualityRepairFailureReason FailureReason = MetadataQualityRepairFailureReason.Unknown);

    private enum MetadataQualityBatchRepairStatus
    {
        Succeeded,
        SavedWithWriteBackErrors,
        NotApplicable,
        NotFound,
        Failed
    }
}

public sealed record MetadataQualityRepairDetailViewModel(string Title, string Author, string Reason);
