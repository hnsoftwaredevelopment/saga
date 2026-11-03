using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Series;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed class SeriesNumberProposalRow(SeriesNumberSource source) : ObservableObject
{
    public SeriesNumberSource Source { get; } = source;
    public string Title => Source.Book.Title;
    public string Authors => Source.Book.Authors;
    public decimal? CurrentNumber => Source.Book.Number;
    private decimal? proposed;
    public decimal? ProposedNumber { get => proposed; internal set => SetProperty(ref proposed, value); }
}

public sealed class SeriesNumbersDialogViewModel : ObservableObject
{
    private readonly SeriesNumbersPreview preview;
    private readonly Func<string, string> localize;
    private readonly CultureInfo culture;
    private string startNumberText = "1";
    private SeriesNumberProposalRow? selectedRow;
    private bool valid;
    public SeriesNumbersDialogViewModel(SeriesNumbersPreview preview, Func<string, string> localize, CultureInfo? culture = null)
    {
        this.preview = preview; this.localize = localize; this.culture = culture ?? CultureInfo.CurrentCulture;
        Rows = new(preview.Books.Select(book => new SeriesNumberProposalRow(book)));
        selectedRow = Rows.FirstOrDefault(); Recalculate();
    }
    public ObservableCollection<SeriesNumberProposalRow> Rows { get; }
    public string Name => preview.Name;
    public string StartNumberText { get => startNumberText; set { if (SetProperty(ref startNumberText, value)) Recalculate(); } }
    public SeriesNumberProposalRow? SelectedRow
    {
        get => selectedRow;
        set { if (SetProperty(ref selectedRow, value)) NotifyMoves(); }
    }
    public bool CanSave => valid && Rows.Any(row => row.ProposedNumber != row.CurrentNumber);
    public bool CanMoveUp => selectedRow is not null && Rows.IndexOf(selectedRow) > 0;
    public bool CanMoveDown => selectedRow is not null && Rows.IndexOf(selectedRow) is var index && index >= 0 && index < Rows.Count - 1;
    public int DuplicateCount { get; private set; }
    public string DuplicateMessage => DuplicateCount > 0 ? string.Format(culture, localize("SeriesNumbersDuplicates"), DuplicateCount) : "";
    public string ValidationMessage => valid ? "" : localize("SeriesNumbersInvalid");

    public void MoveSelected(int offset)
    {
        if (selectedRow is null || offset is not (-1 or 1)) return;
        var index = Rows.IndexOf(selectedRow); var next = index + offset;
        if (index < 0 || next < 0 || next >= Rows.Count) return;
        Rows.Move(index, next); Recalculate();
    }
    public IReadOnlyList<SeriesNumberAssignment> CreateAssignments() => CanSave
        ? Rows.Select(row => new SeriesNumberAssignment(row.Source.Book.Id, row.ProposedNumber!.Value)).ToArray()
        : throw new InvalidOperationException("A valid changed proposal is required.");
    private void Recalculate()
    {
        valid = Rows.Count >= 2 && decimal.TryParse(StartNumberText.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, culture, out _);
        decimal[] numbers = [];
        if (valid)
        {
            var start = decimal.Parse(StartNumberText.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, culture);
            try { numbers = Enumerable.Range(0, Rows.Count).Select(i => checked(start + i)).ToArray(); }
            catch (OverflowException) { valid = false; }
        }
        for (var i = 0; i < Rows.Count; i++) Rows[i].ProposedNumber = valid ? numbers[i] : null;
        var proposed = numbers.ToHashSet();
        DuplicateCount = valid ? preview.OtherNumbers.Count(proposed.Contains) : 0;
        OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(DuplicateCount)); OnPropertyChanged(nameof(DuplicateMessage)); NotifyMoves();
    }
    private void NotifyMoves() { OnPropertyChanged(nameof(CanMoveUp)); OnPropertyChanged(nameof(CanMoveDown)); }
}
