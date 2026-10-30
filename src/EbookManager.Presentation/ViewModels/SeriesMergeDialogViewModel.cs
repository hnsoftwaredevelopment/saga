using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed record SeriesMergeBookRow(Guid Id, string Series, string Title, string Authors, decimal? Number, string Note);

public sealed partial class SeriesMergeDialogViewModel : ObservableObject
{
    private readonly SeriesMergeBookRow[] ordered;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] [NotifyPropertyChangedFor(nameof(TargetName))]
    private SeriesMergeGroup? selectedTarget;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasMore))]
    private IReadOnlyList<SeriesMergeBookRow> books;
    public IReadOnlyList<SeriesMergeGroup> Targets { get; }
    public string Summary { get; }
    public bool CanMerge => SelectedTarget is not null && Targets.Contains(SelectedTarget);
    public string TargetName => CanMerge ? SelectedTarget!.Name : "";
    public bool HasMore => Books.Count < ordered.Length;

    public SeriesMergeDialogViewModel(SeriesMergePreview preview, Func<string, string> localize)
    {
        Targets = preview.Groups;
        var all = preview.Groups.SelectMany(group => group.Books).DistinctBy(book => book.Id).ToArray();
        var repeated = all.Where(book => book.Number.HasValue).GroupBy(book => book.Number!.Value)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        ordered = all.OrderBy(book => book.Number is null).ThenBy(book => book.Number)
            .ThenBy(book => book.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(book => book.Id)
            .Select(book => new SeriesMergeBookRow(book.Id, book.StoredName, book.Title, book.Authors, book.Number,
                book.Number is null ? localize("SeriesMergeMissingNumber") : repeated.Contains(book.Number.Value) ? localize("SeriesMergeRepeatedNumber") : "")).ToArray();
        books = ordered.Take(200).ToArray();
        Summary = string.Format(CultureInfo.CurrentCulture, localize("SeriesMergeSummary"), all.Length);
    }
    public void LoadMore() { if (HasMore) Books = ordered.Take(Books.Count + 200).ToArray(); }
}
