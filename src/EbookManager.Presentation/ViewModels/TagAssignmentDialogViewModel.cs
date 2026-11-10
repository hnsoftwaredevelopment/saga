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
        newName = operation == TagAssignmentOperation.Add ? "" : preview.Name;
        Operation = operation;
        bookCount = preview.Books.Count;
        var (title, action, explanation) = operation switch
        {
            TagAssignmentOperation.Add => ("TagAddSelection", "TagAddSelection", "TagAddExplanation"),
            TagAssignmentOperation.Remove => ("TagRemoveSelectionTitle", "TagRemoveSelection", "TagRemoveExplanation"),
            _ => ("TagChangeSelectionTitle", "TagChangeSelection", "TagChangeExplanation")
        };
        Title = localize(title);
        ConfirmText = localize(action);
        Explanation = string.Format(CultureInfo.CurrentCulture, localize(explanation), preview.Name, bookCount);
    }
    public string OriginalName { get; }
    public TagAssignmentOperation Operation { get; }
    public bool HasTargetName => Operation == TagAssignmentOperation.Replace;
    public bool IsAdd => Operation == TagAssignmentOperation.Add;
    public string Title { get; }
    public string ConfirmText { get; }
    public string Explanation { get; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    private string newName;

    public IReadOnlyList<string> QueuedTags { get; private set; } = [];
    public bool CanApply => bookCount > 0 && (Operation switch
    {
        TagAssignmentOperation.Add => QueuedTags.Count > 0 && string.IsNullOrWhiteSpace(NewName),
        TagAssignmentOperation.Remove => true,
        TagAssignmentOperation.Replace => TagNames.IsValid(NewName) && TagNames.Normalize(NewName) != TagNames.Normalize(OriginalName),
        _ => false
    });

    public bool QueueTag(string name) => QueueTags([name]);

    public bool QueuePastedTags(string text) => QueueTags(text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
        .Where(line => !string.IsNullOrWhiteSpace(line)).ToArray());

    private bool QueueTags(string[] names)
    {
        if (!IsAdd || names.Length == 0 || !names.All(TagNames.IsValid)) return false;
        QueuedTags = QueuedTags.Concat(names.Select(name => name.Trim())).DistinctBy(TagNames.Normalize).ToArray();
        NewName = "";
        NotifyQueuedTags();
        return true;
    }

    public void UnqueueTag(string name)
    {
        QueuedTags = QueuedTags.Where(tag => tag != name).ToArray();
        NotifyQueuedTags();
    }

    private void NotifyQueuedTags()
    {
        OnPropertyChanged(nameof(QueuedTags));
        OnPropertyChanged(nameof(CanApply));
    }

    public TagAssignmentRequest CreateRequest() => IsAdd
        ? new(Operation) { TargetNames = Array.AsReadOnly(QueuedTags.ToArray()) }
        : new(Operation, NewName);
}
