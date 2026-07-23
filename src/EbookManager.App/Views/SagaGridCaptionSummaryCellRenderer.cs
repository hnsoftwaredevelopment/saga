using EbookManager.App.Localization;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Cells;

namespace EbookManager.App.Views;

internal sealed class SagaGridCaptionSummaryCellRenderer : GridCaptionSummaryCellRenderer
{
    public override void OnInitializeEditElement(
        DataColumnBase dataColumn,
        GridCaptionSummaryCell uiElement,
        object dataContext)
    {
        base.OnInitializeEditElement(dataColumn, uiElement, dataContext);
        ApplyCaption(uiElement, dataContext);
    }

    public override void OnUpdateEditBinding(
        DataColumnBase dataColumn,
        GridCaptionSummaryCell element,
        object dataContext)
    {
        base.OnUpdateEditBinding(dataColumn, element, dataContext);
        ApplyCaption(element, dataContext);
    }

    private static void ApplyCaption(GridCaptionSummaryCell cell, object dataContext)
    {
        try
        {
            if (dataContext is not Group group)
            {
                return;
            }

            var key = group.Key?.ToString() ?? string.Empty;
            var count = Math.Max(group.GetRecordCount(), 0);
            var bookText = LocalizedStrings.Current[count == 1 ? "BookSingular" : "BookCount"];
            cell.Content = string.IsNullOrWhiteSpace(key)
                ? $"- {count} {bookText}"
                : $"{key} - {count} {bookText}";
        }
        catch
        {
            // Syncfusion can render caption cells while nested groups are being rebuilt.
            // Keep the grid alive and fall back to the framework-provided caption.
        }
    }
}
