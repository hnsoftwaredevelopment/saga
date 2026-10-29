using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Tags;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class TagRenameDialogViewModel : ObservableObject
{
    public TagRenameDialogViewModel(TagRenamePreview preview, Func<string, string> localize)
    {
        OriginalName = preview.Name;
        newName = preview.Name;
        Explanation = string.Format(CultureInfo.CurrentCulture, localize("TagRenameExplanation"), preview.Books.Count);
    }
    public string OriginalName { get; }
    public string Explanation { get; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string newName;
    public bool CanSave => TagNames.IsValid(NewName) && NewName.Trim() != OriginalName;
}
