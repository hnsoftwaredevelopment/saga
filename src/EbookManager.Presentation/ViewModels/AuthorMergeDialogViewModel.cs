using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Domain.Authors;

namespace EbookManager.Presentation.ViewModels;

public sealed record AuthorMergeOption(Guid? AuthorId, string Label, string? Value);
public sealed record AuthorComparisonCard(AuthorProfile Profile, int BookCount, string EmptyText)
{
    public string BiographyDisplay => string.IsNullOrWhiteSpace(Profile.Biography) ? EmptyText : Profile.Biography;
    public string WebsiteDisplay => string.IsNullOrWhiteSpace(Profile.WebsiteUrl) ? EmptyText : Profile.WebsiteUrl;
}

public sealed partial class AuthorMergeDialogViewModel : ObservableObject
{
    public AuthorMergeDialogViewModel(AuthorMergePreview preview, Func<string, string> localize)
    {
        Preview = preview;
        Authors = preview.Authors.Select(author => new AuthorSummary(author.Profile.Id, author.Profile.Name, author.BookIds.Count)).ToArray();
        ComparisonAuthors = preview.Authors.Select(author => new AuthorComparisonCard(author.Profile, author.BookIds.Count, localize("AuthorComparisonNoValue"))).ToArray();
        Explanation = string.Format(System.Globalization.CultureInfo.CurrentCulture, localize("AuthorMergeExplanation"), Authors.Count, preview.BookCount);
        BiographyOptions = Options(profile => profile.Biography, localize("AuthorMergeEmpty"));
        WebsiteOptions = Options(profile => profile.WebsiteUrl, localize("AuthorMergeEmpty"));
        PhotoOptions = Options(profile => profile.PhotoRelativePath, localize("AuthorMergeEmpty"), photo: true);
        biographyChoice = Initial(BiographyOptions);
        websiteChoice = Initial(WebsiteOptions);
        photoChoice = Initial(PhotoOptions);
    }
    public AuthorMergePreview Preview { get; }
    public IReadOnlyList<AuthorSummary> Authors { get; }
    public IReadOnlyList<AuthorComparisonCard> ComparisonAuthors { get; }
    public string Explanation { get; }
    public IReadOnlyList<AuthorMergeOption> BiographyOptions { get; }
    public IReadOnlyList<AuthorMergeOption> WebsiteOptions { get; }
    public IReadOnlyList<AuthorMergeOption> PhotoOptions { get; }
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] private AuthorSummary? target;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] private AuthorMergeOption? biographyChoice;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] private AuthorMergeOption? websiteChoice;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] private AuthorMergeOption? photoChoice;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] private bool photoUnavailable;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanMerge))] private bool photoLoading;
    public bool CanMerge => !PhotoLoading && !PhotoUnavailable && Authors.Count >= 2 && Target is not null && Authors.Contains(Target) &&
        BiographyChoice is not null && BiographyOptions.Contains(BiographyChoice) &&
        WebsiteChoice is not null && WebsiteOptions.Contains(WebsiteChoice) && PhotoChoice is not null && PhotoOptions.Contains(PhotoChoice);
    public AuthorMergeSelection Selection => CanMerge
        ? new(Target!.Id, BiographyChoice!.AuthorId, WebsiteChoice!.AuthorId, PhotoChoice!.AuthorId)
        : throw new InvalidOperationException("Explicit merge choices are required.");

    [RelayCommand]
    private void UseTarget(AuthorComparisonCard card) { if (ComparisonAuthors.Contains(card)) Target = Authors.Single(author => author.Id == card.Profile.Id); }
    [RelayCommand]
    private void UseBiography(AuthorComparisonCard card) { if (ComparisonAuthors.Contains(card)) BiographyChoice = BiographyOptions.FirstOrDefault(option => option.AuthorId == card.Profile.Id) ?? BiographyOptions[0]; }
    [RelayCommand]
    private void UseWebsite(AuthorComparisonCard card) { if (ComparisonAuthors.Contains(card)) WebsiteChoice = WebsiteOptions.FirstOrDefault(option => option.AuthorId == card.Profile.Id) ?? WebsiteOptions[0]; }
    [RelayCommand]
    private void UsePhoto(AuthorComparisonCard card) { if (ComparisonAuthors.Contains(card)) PhotoChoice = PhotoOptions.FirstOrDefault(option => option.AuthorId == card.Profile.Id) ?? PhotoOptions[0]; }

    private AuthorMergeOption[] Options(Func<AuthorProfile, string?> field, string empty, bool photo = false) =>
        new[] { new AuthorMergeOption(null, empty, null) }.Concat(Preview.Authors.Where(author => !string.IsNullOrWhiteSpace(field(author.Profile)))
            .Select(author => new AuthorMergeOption(author.Profile.Id,
                photo ? author.Profile.Name : $"{author.Profile.Name}: {Abbreviate(field(author.Profile)!)}", field(author.Profile)))).ToArray();
    private static AuthorMergeOption? Initial(IReadOnlyList<AuthorMergeOption> options) =>
        options.Skip(1).Select(option => option.Value).Distinct(StringComparer.Ordinal).Count() switch { 0 => options[0], 1 => options[1], _ => null };
    private static string Abbreviate(string text) => text.Length > 120 ? text[..120] + "…" : text.ReplaceLineEndings(" ");
}
