using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace EbookManager.Presentation.ViewModels;

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool suppressNotifications;

    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var replacement = new List<T>(items);
        suppressNotifications = true;
        try
        {
            Items.Clear();
            foreach (var item in replacement)
            {
                Items.Add(item);
            }
        }
        finally
        {
            suppressNotifications = false;
        }

        OnPropertyChanged(new(nameof(Count)));
        OnPropertyChanged(new("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!suppressNotifications)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!suppressNotifications)
        {
            base.OnPropertyChanged(e);
        }
    }
}
