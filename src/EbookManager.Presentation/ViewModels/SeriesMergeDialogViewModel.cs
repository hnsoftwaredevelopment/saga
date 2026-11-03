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
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] [NotifyPropertyChangedFor(nameof(TargetName))]
    private string newNameText = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasMore))]
    private IReadOnlyList<SeriesMergeBookRow> books;
    public IReadOnlyList<SeriesMergeGroup> Targets { get; }
    public string Summary { get; }
    public string Help { get; }
    public bool CanUseNewName { get; }
    private bool HasExistingTarget => SelectedTarget is not null && Targets.Contains(SelectedTarget);
    public bool CanMerge => HasExistingTarget || (CanUseNewName && !string.IsNullOrWhiteSpace(NewNameText));
    public string TargetName => CanUseNewName && !string.IsNullOrWhiteSpace(NewNameText) ? NewNameText.Trim() : HasExistingTarget ? SelectedTarget!.Name : "";
    public bool HasMore => Books.Count < ordered.Length;

    public SeriesMergeDialogViewModel(SeriesMergePreview preview, Func<string, string> localize)
    {
        CanUseNewName = preview.AllowNewName;
        Help = localize(CanUseNewName ? "SeriesManualMergeHelp" : "SeriesMergeHelp");
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
    partial void OnSelectedTargetChanged(SeriesMergeGroup? value) { if (value is not null) NewNameText = ""; }
    partial void OnNewNameTextChanged(string value) { if (CanUseNewName && value.Length > 0) SelectedTarget = null; }
    public void LoadMore() { if (HasMore) Books = ordered.Take(Books.Count + 200).ToArray(); }
}
