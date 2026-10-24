using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Authors;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class AuthorRenameDialogViewModel : ObservableObject
{
    public AuthorRenameDialogViewModel(AuthorRenamePreview preview, Func<string, string> localize)
    {
        OriginalName = preview.Profile.Name;
        BookCount = preview.BookIds.Count;
        newName = OriginalName;
        Explanation = string.Format(CultureInfo.CurrentCulture, localize("AuthorRenameExplanation"), BookCount);
    }

    public string OriginalName { get; }
    public int BookCount { get; }
    public string Explanation { get; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string newName;
    public bool CanSave => MetadataQualityAuthorRules.IsUsable(NewName) && NewName.Trim() != OriginalName;
}
