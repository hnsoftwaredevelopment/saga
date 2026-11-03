using CommunityToolkit.Mvvm.ComponentModel;
using EbookManager.Domain.Series;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class SeriesNumberDialogViewModel : ObservableObject
{
    private readonly SeriesNumberPreview preview;
    private readonly Func<string, string> localize;
    private readonly CultureInfo culture;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(Number))]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyPropertyChangedFor(nameof(DuplicateCount))]
    [NotifyPropertyChangedFor(nameof(HasDuplicate))]
    [NotifyPropertyChangedFor(nameof(DuplicateMessage))]
    private string numberText;

    public SeriesNumberDialogViewModel(SeriesNumberPreview preview, Func<string, string> localize, CultureInfo? culture = null)
    {
        this.preview = preview; this.localize = localize; this.culture = culture ?? CultureInfo.CurrentCulture;
        numberText = preview.Book.Number?.ToString(this.culture) ?? "";
    }
    public string Title => preview.Book.Title;
    public string Authors => preview.Book.Authors;
    public string Series => preview.StoredName;
    public string Help => string.Format(culture, localize("SeriesNumberHelp"), 1.5m.ToString(culture));
    private bool TryNumber(out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(NumberText)) return true;
        if (!decimal.TryParse(NumberText.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, culture, out var parsed)) return false;
        value = parsed; return true;
    }
    public decimal? Number => TryNumber(out var value) ? value : null;
    public bool CanSave => TryNumber(out var value) && value != preview.Book.Number;
    public string ValidationMessage => TryNumber(out _) ? "" : string.Format(culture, localize("SeriesNumberInvalid"), 1.5m.ToString(culture));
    public int DuplicateCount => TryNumber(out var value) && value.HasValue ? preview.OtherNumbers.Count(number => number == value.Value) : 0;
    public bool HasDuplicate => DuplicateCount > 0;
    public string DuplicateMessage => HasDuplicate ? string.Format(culture, localize("SeriesNumberDuplicate"), DuplicateCount) : "";
}
