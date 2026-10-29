using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Tags;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class TagAssignmentDialogViewModel : ObservableObject
{
    private readonly int bookCount;
    public TagAssignmentDialogViewModel(TagAssignmentPreview preview, TagAssignmentOperation operation, Func<string, string> localize)
    {
        OriginalName = preview.Name;
        newName = preview.Name;
        Operation = operation;
        bookCount = preview.Books.Count;
        Title = localize(operation == TagAssignmentOperation.Remove ? "TagRemoveSelectionTitle" : "TagChangeSelectionTitle");
        ConfirmText = localize(operation == TagAssignmentOperation.Remove ? "TagRemoveSelection" : "TagChangeSelection");
        Explanation = string.Format(CultureInfo.CurrentCulture,
            localize(operation == TagAssignmentOperation.Remove ? "TagRemoveExplanation" : "TagChangeExplanation"), preview.Name, bookCount);
    }
    public string OriginalName { get; }
    public TagAssignmentOperation Operation { get; }
    public bool HasTargetName => Operation == TagAssignmentOperation.Replace;
    public string Title { get; }
    public string ConfirmText { get; }
    public string Explanation { get; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string newName;
    public bool CanApply => bookCount > 0 && (Operation == TagAssignmentOperation.Remove ||
        Operation == TagAssignmentOperation.Replace && TagNames.IsValid(NewName) && TagNames.Normalize(NewName) != TagNames.Normalize(OriginalName));
}
