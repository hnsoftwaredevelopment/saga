using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesRenameDialogViewModel : ObservableObject
{
    private readonly Func<string, string> localize;
    public SeriesRenameDialogViewModel(SeriesRenamePreview preview, Func<string, string> localize)
    {
        this.localize = localize;
        OriginalName = preview.Name;
        IsSplit = preview.SelectedBookIds is not null;
        ActionLabel = localize(IsSplit ? "SeriesSplitAction" : "SeriesRename");
        newName = IsSplit ? "" : preview.Name;
        if (preview.SelectedBookIds is { } ids)
        {
            var booksById = preview.Books.ToDictionary(book => book.Id);
            SplitBooks = ids.Select(id => booksById[id]).ToArray();
            CanCreateSeries = SplitBooks.Count < preview.Books.Count;
            Explanation = string.Format(CultureInfo.CurrentCulture, localize("SeriesSplitExplanation"), SplitBooks.Count, preview.Books.Count - SplitBooks.Count);
        }
        else
        {
            Explanation = string.Format(CultureInfo.CurrentCulture, localize("SeriesRenameExplanation"), preview.Books.Count);
        }
        hasNonCanonicalNames = preview.Books.Any(book => book.StoredName != preview.Name);
    }
    private readonly bool hasNonCanonicalNames;
    public string OriginalName { get; }
    public string Explanation { get; }
    public bool IsSplit { get; }
    public bool CanCreateSeries { get; }
    public string ActionLabel { get; }
    public IReadOnlyList<SeriesSourceBook> SplitBooks { get; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string newName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool canUseNewName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(OverlapMessage))]
    private SeriesMoveDestination? destination;

    public string OverlapMessage => Destination is { OverlappingNumbers.Count: > 0 } target
        ? string.Format(CultureInfo.CurrentCulture, localize("SeriesMoveOverlap"),
            string.Join(" · ", target.OverlappingNumbers.Select(number => number.ToString(CultureInfo.CurrentCulture))))
        : "";

    public string NameLabel => localize(IsSplit ? "SeriesMoveTarget" : "SeriesRenameNew");
    public string SaveName => Destination?.Name ?? NewName;
    public bool CanSave => !string.IsNullOrWhiteSpace(NewName) && (IsSplit
        ? !SeriesNames.Same(NewName, OriginalName) && (Destination is not null && SeriesNames.Same(Destination.Name, NewName) || CanCreateSeries && CanUseNewName)
        : hasNonCanonicalNames || NewName.Trim() != OriginalName);

    partial void OnNewNameChanged(string value)
    {
        CanUseNewName = false;
        Destination = null;
    }
}
