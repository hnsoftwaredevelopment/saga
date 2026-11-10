using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesRenameDialogViewModel : ObservableObject
{
    public SeriesRenameDialogViewModel(SeriesRenamePreview preview, Func<string, string> localize)
    {
        OriginalName = preview.Name;
        IsSplit = preview.SelectedBookIds is not null;
        ActionLabel = localize(IsSplit ? "SeriesSplitAction" : "SeriesRename");
        newName = IsSplit ? "" : preview.Name;
        if (preview.SelectedBookIds is { } ids)
        {
            var booksById = preview.Books.ToDictionary(book => book.Id);
            SplitBooks = ids.Select(id => booksById[id]).ToArray();
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
    public string ActionLabel { get; }
    public IReadOnlyList<SeriesSourceBook> SplitBooks { get; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string newName;
    public bool CanSave => !string.IsNullOrWhiteSpace(NewName) && (IsSplit
        ? !SeriesNames.Same(NewName, OriginalName)
        : hasNonCanonicalNames || NewName.Trim() != OriginalName);
}
