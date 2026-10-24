using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesRenameDialogViewModel : ObservableObject
{
    public SeriesRenameDialogViewModel(SeriesRenamePreview preview, Func<string, string> localize)
    {
        OriginalName = preview.Name;
        newName = preview.Name;
        Explanation = string.Format(CultureInfo.CurrentCulture, localize("SeriesRenameExplanation"), preview.Books.Count);
        hasNonCanonicalNames = preview.Books.Any(book => book.StoredName != preview.Name);
    }
    private readonly bool hasNonCanonicalNames;
    public string OriginalName { get; }
    public string Explanation { get; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string newName;
    public bool CanSave => !string.IsNullOrWhiteSpace(NewName) && (hasNonCanonicalNames || NewName.Trim() != OriginalName);
}
