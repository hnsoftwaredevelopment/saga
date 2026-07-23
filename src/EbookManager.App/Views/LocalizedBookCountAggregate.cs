using System.Collections;
using System.ComponentModel;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;
using Syncfusion.Data;

namespace EbookManager.App.Views;

internal sealed class LocalizedBookCountAggregate : ISummaryAggregate
{
    public string Text { get; private set; } = FormatCount(0);

    public Action<IEnumerable, string, PropertyDescriptor> CalculateAggregateFunc() =>
        (items, _, _) => Text = FormatCount(CountDistinctBooks(items));

    private static int CountDistinctBooks(object? source)
    {
        var ids = new HashSet<Guid>();
        AddBookIds(source, ids);
        return ids.Count;
    }

    private static void AddBookIds(object? value, ISet<Guid> ids)
    {
        switch (value)
        {
            case null:
                return;
            case BookRowViewModel row:
                ids.Add(row.Id);
                return;
            case RecordEntry recordEntry:
                AddBookIds(recordEntry.Data, ids);
                return;
            case Group group:
                AddBookIds(group.Records, ids);
                AddBookIds(group.Groups, ids);
                return;
            case IEnumerable values when value is not string:
                foreach (var item in values)
                {
                    AddBookIds(item, ids);
                }

                return;
        }
    }

    private static string FormatCount(int count)
    {
        var resourceKey = count == 1 ? "BookSingular" : "BookCount";
        return $"{count} {LocalizedStrings.Current[resourceKey]}";
    }
}
