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
        var (titleKey, confirmKey, explanationKey) = operation switch
        {
            TagAssignmentOperation.Split => ("TagSplitSelectionTitle", "TagSplitSelection", "TagSplitExplanation"),
            TagAssignmentOperation.Remove => ("TagRemoveSelectionTitle", "TagRemoveSelection", "TagRemoveExplanation"),
            _ => ("TagChangeSelectionTitle", "TagChangeSelection", "TagChangeExplanation")
        };
        Title = localize(titleKey);
        ConfirmText = localize(confirmKey);
        Explanation = string.Format(CultureInfo.CurrentCulture, localize(explanationKey), preview.Name, bookCount);
        if (IsSplit) SplitBooks = preview.Books.OrderBy(book => book.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
    public string OriginalName { get; }
    public TagAssignmentOperation Operation { get; }
    public bool HasTargetName => Operation == TagAssignmentOperation.Replace;
    public bool IsSplit => Operation == TagAssignmentOperation.Split;
    public IReadOnlyList<TagSourceBook> SplitBooks { get; } = [];
    public string Title { get; }
    public string ConfirmText { get; }
    public string Explanation { get; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string newName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    [NotifyPropertyChangedFor(nameof(SplitPreview))]
    private string splitText = "";

    private string[] SplitNames => SplitText.Split(['\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    public string SplitPreview => CanApply && IsSplit ? string.Join(Environment.NewLine, SplitNames) : "";

    public bool CanApply => bookCount > 0 && (Operation switch
    {
        TagAssignmentOperation.Remove => true,
        TagAssignmentOperation.Replace => TagNames.IsValid(NewName) && TagNames.Normalize(NewName) != TagNames.Normalize(OriginalName),
        TagAssignmentOperation.Split => TagNames.AreValidSplitTargets(OriginalName, SplitNames),
        _ => false
    });

    public TagAssignmentRequest CreateRequest() => IsSplit
        ? new(Operation) { TargetNames = Array.AsReadOnly(SplitNames) }
        : new(Operation, NewName);
}
