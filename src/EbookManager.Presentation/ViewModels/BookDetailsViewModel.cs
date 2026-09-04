using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Presentation.Abstractions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class BookDetailsViewModel(
    BookService bookService,
    BookFileExportService? exportService = null,
    IBookFileInteractionService? fileInteraction = null,
    ICustomMetadataRepository? customMetadataRepository = null,
    IBookCoverSearchService? coverSearchService = null,
    Func<MetadataQualityCoverSearchViewModel, CancellationToken, Task<bool>>? showCoverSearch = null,
    IBookCoverUpdateService? coverUpdateService = null,
    Func<string, string>? localize = null) : ObservableObject
{
    private readonly BookService bookService = bookService;
    private readonly BookFileExportService? exportService = exportService;
    private readonly IBookFileInteractionService? fileInteraction = fileInteraction;
    private readonly ICustomMetadataRepository? customMetadataRepository = customMetadataRepository;
    private readonly IBookCoverSearchService? coverSearchService = coverSearchService;
    private readonly Func<MetadataQualityCoverSearchViewModel, CancellationToken, Task<bool>>? showCoverSearch = showCoverSearch;
    private readonly IBookCoverUpdateService? coverUpdateService = coverUpdateService;
    private readonly Func<string, string> localize = localize ?? (key => key);
    private Book? originalBook;
    private string? currentLibraryPath;
    private Dictionary<Guid, string?> originalCustomMetadataValues = [];
    private bool isApplyingBook;
    private string? singleAuthorText;

    [ObservableProperty]
    private Guid? bookId;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string authorsText = string.Empty;

    [ObservableProperty]
    private string formatsText = string.Empty;

    public ObservableCollection<BookFormatDetailsViewModel> FormatDetails { get; } = [];
    public ObservableCollection<CustomMetadataValueViewModel> CustomMetadataValues { get; } = [];

    public bool HasCustomMetadataValues => CustomMetadataValues.Count > 0;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private string? language;

    public string LanguageDisplayName => string.IsNullOrWhiteSpace(Language)
        ? string.Empty
        : LanguageDisplayService.DisplayName(Language);

    public string CreatedUtcText => originalBook is null ? string.Empty : FormatDateTime(originalBook.CreatedUtc);

    public string UpdatedUtcText => originalBook is null ? string.Empty : FormatDateTime(originalBook.UpdatedUtc);

    public void RefreshLocalizedDisplayNames()
    {
        OnPropertyChanged(nameof(LanguageDisplayName));
        OnPropertyChanged(nameof(CreatedUtcText));
        OnPropertyChanged(nameof(UpdatedUtcText));
    }

    [ObservableProperty]
    private string? publisher;

    [ObservableProperty]
    private DateOnly? publicationDate;

    public DateTime? PublicationDateValue
    {
        get => PublicationDate?.ToDateTime(TimeOnly.MinValue);
        set => PublicationDate = value is null ? null : DateOnly.FromDateTime(value.Value);
    }

    [ObservableProperty]
    private string? tagsText;

    [ObservableProperty]
    private string? series;

    [ObservableProperty]
    private decimal? seriesNumber;

    [ObservableProperty]
    private string? isbn;

    [ObservableProperty]
    private ReadingStatus readingStatus;

    [ObservableProperty]
    private byte[]? coverBytes;

    [ObservableProperty]
    private string? coverPath;

    [ObservableProperty]
    private bool hasUnsavedChanges;

    [ObservableProperty]
    private BookSaveResult? lastSaveResult;

    public bool HasSaveError => LastSaveResult?.Status is BookSaveStatus.Conflict or BookSaveStatus.Failed;

    public string? SaveErrorMessage => LastSaveResult?.Status switch
    {
        BookSaveStatus.Conflict => "A book with the same title and author already exists.",
        BookSaveStatus.Failed => string.IsNullOrWhiteSpace(LastSaveResult.Message)
            ? "The changes could not be saved."
            : LastSaveResult.Message,
        _ => null
    };

    [ObservableProperty]
    private BookDeleteResult? lastDeleteResult;

    [ObservableProperty]
    private string? coverChangeErrorMessage;

    public bool HasCoverChangeError => !string.IsNullOrWhiteSpace(CoverChangeErrorMessage);

    public IAsyncRelayCommand SaveCommand => saveCommand ??= new AsyncRelayCommand(SaveAsync, CanEdit);
    public IAsyncRelayCommand DeleteCommand => deleteCommand ??= new AsyncRelayCommand(DeleteAsync, CanEdit);
    public IRelayCommand UndoCommand => undoCommand ??= new RelayCommand(Undo, CanEdit);
    public IRelayCommand SwapTitleAndAuthorsCommand => swapTitleAndAuthorsCommand ??= new RelayCommand(SwapTitleAndAuthors, CanEdit);
    public IAsyncRelayCommand ChangeCoverCommand => changeCoverCommand ??= new AsyncRelayCommand(ChangeCoverAsync, CanChangeCover);

    private AsyncRelayCommand? saveCommand;
    private AsyncRelayCommand? deleteCommand;
    private RelayCommand? undoCommand;
    private RelayCommand? swapTitleAndAuthorsCommand;
    private AsyncRelayCommand? changeCoverCommand;

    public event EventHandler<Book>? BookSaved;
    public event EventHandler<Guid>? BookDeleted;

    partial void OnLastSaveResultChanged(BookSaveResult? value)
    {
        OnPropertyChanged(nameof(HasSaveError));
        OnPropertyChanged(nameof(SaveErrorMessage));
    }

    partial void OnCoverChangeErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasCoverChangeError));

    public void Load(Book book, string? libraryPath = null)
    {
        ArgumentNullException.ThrowIfNull(book);

        originalBook = book;
        currentLibraryPath = libraryPath;
        Apply(book, libraryPath);
        LastSaveResult = null;
        LastDeleteResult = null;
        CoverChangeErrorMessage = null;
        RefreshLocalizedDisplayNames();
        RefreshDirtyState();
        NotifyCommandState();
    }

    public void Clear()
    {
        originalBook = null;
        currentLibraryPath = null;
        singleAuthorText = null;
        BookId = null;
        Title = string.Empty;
        AuthorsText = string.Empty;
        FormatsText = string.Empty;
        FormatDetails.Clear();
        CustomMetadataValues.Clear();
        OnPropertyChanged(nameof(HasCustomMetadataValues));
        originalCustomMetadataValues = [];
        Description = null;
        Language = null;
        Publisher = null;
        PublicationDate = null;
        TagsText = null;
        Series = null;
        SeriesNumber = null;
        Isbn = null;
        ReadingStatus = ReadingStatus.Unread;
        CoverBytes = null;
        CoverPath = null;
        LastSaveResult = null;
        LastDeleteResult = null;
        CoverChangeErrorMessage = null;
        RefreshLocalizedDisplayNames();
        RefreshDirtyState();
        NotifyCommandState();
    }

    public Book? ToBook()
    {
        if (originalBook is null)
        {
            return null;
        }

        return originalBook with
        {
            Metadata = new BookMetadata(
                Title.Trim(),
                GetEditedAuthors(),
                CleanDescription(Description),
                NormalizeBlank(Language),
                NormalizeBlank(Publisher),
                PublicationDate,
                SplitNullableList(TagsText),
                NormalizeBlank(Series),
                SeriesNumber,
                NormalizeBlank(Isbn),
                CoverBytes),
            ReadingStatus = ReadingStatus,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var book = ToBook();
        if (book is null)
        {
            return;
        }

        var customMetadataChanges = TryCreateCustomMetadataChanges(book.Id);
        if (customMetadataChanges is null)
        {
            return;
        }

        Book savedBook = book;
        if (CoverBytes is { Length: > 0 } coverBytes &&
            originalBook is not null &&
            !CoverEquals(originalBook.Metadata.CoverBytes, coverBytes) &&
            coverUpdateService is not null)
        {
            var updateResult = await coverUpdateService.UpdateAsync(book, coverBytes, cancellationToken);
            LastSaveResult = updateResult.SaveResult;
            savedBook = updateResult.Book ?? book;
        }
        else
        {
            LastSaveResult = await bookService.SaveAsync(book, cancellationToken);
        }

        if (LastSaveResult.Status == BookSaveStatus.Succeeded)
        {
            var customMetadataSaved = await SaveCustomMetadataValuesAsync(customMetadataChanges, cancellationToken);
            if (!customMetadataSaved)
            {
                return;
            }

            originalBook = savedBook;
            originalCustomMetadataValues = SnapshotCustomMetadataValues();
            Apply(savedBook, currentLibraryPath);
            RefreshLocalizedDisplayNames();
            RefreshDirtyState();
            BookSaved?.Invoke(this, savedBook);
        }
    }

    private async Task ChangeCoverAsync(CancellationToken cancellationToken)
    {
        if (originalBook is null || coverSearchService is null || showCoverSearch is null)
        {
            return;
        }

        CoverChangeErrorMessage = null;
        var search = new MetadataQualityCoverSearchViewModel(
            new BookCoverSearchQuery(Title.Trim(), GetEditedAuthors(), NormalizeBlank(Isbn)),
            coverSearchService,
            localize);
        if (!await showCoverSearch(search, cancellationToken) || search.SelectedCandidate is not { } candidate)
        {
            return;
        }

        BookCoverDownloadResult download;
        try
        {
            download = await coverSearchService.DownloadAsync(candidate, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            CoverChangeErrorMessage = localize("MetadataQualityCoverDownloadFailed");
            return;
        }
        if (download.Status != BookCoverDownloadStatus.Succeeded || download.Bytes is not { Length: > 0 } bytes)
        {
            CoverChangeErrorMessage = localize("MetadataQualityCoverDownloadFailed");
            return;
        }

        CoverBytes = bytes;
        CoverPath = null;
        LastSaveResult = null;
    }

    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (originalBook is null)
        {
            return;
        }

        var deleteResult = await bookService.DeleteAsync(originalBook.Id, cancellationToken);
        LastDeleteResult = deleteResult;
        if (deleteResult.Status == BookDeleteStatus.Deleted)
        {
            var deletedBookId = originalBook.Id;
            var shouldPreserveWarning = !string.IsNullOrWhiteSpace(deleteResult.Message);
            Clear();
            if (shouldPreserveWarning)
            {
                LastDeleteResult = deleteResult;
            }

            BookDeleted?.Invoke(this, deletedBookId);
        }
    }

    private void Undo()
    {
        if (originalBook is null)
        {
            return;
        }

        Apply(originalBook, currentLibraryPath);
        ApplyValues(() =>
        {
            foreach (var value in CustomMetadataValues)
            {
                value.ValueText = originalCustomMetadataValues.GetValueOrDefault(value.FieldId);
            }
        });
        LastSaveResult = null;
        CoverChangeErrorMessage = null;
        RefreshDirtyState();
    }

    private void SwapTitleAndAuthors()
    {
        if (originalBook is null)
        {
            return;
        }

        var titleToAuthor = Title.Trim();
        Title = JoinAuthorsForTitle(SplitList(AuthorsText));
        AuthorsText = titleToAuthor;
        singleAuthorText = titleToAuthor;
        LastSaveResult = null;
    }

    partial void OnTitleChanged(string value) => RefreshDirtyState();
    partial void OnAuthorsTextChanged(string value)
    {
        if (!isApplyingBook &&
            singleAuthorText is not null &&
            !string.Equals(value, singleAuthorText, StringComparison.Ordinal))
        {
            singleAuthorText = null;
        }

        RefreshDirtyState();
    }
    partial void OnDescriptionChanged(string? value) => RefreshDirtyState();
    partial void OnLanguageChanged(string? value)
    {
        OnPropertyChanged(nameof(LanguageDisplayName));
        RefreshDirtyState();
    }
    partial void OnPublisherChanged(string? value) => RefreshDirtyState();
    partial void OnPublicationDateChanged(DateOnly? value)
    {
        OnPropertyChanged(nameof(PublicationDateValue));
        RefreshDirtyState();
    }
    partial void OnTagsTextChanged(string? value) => RefreshDirtyState();
    partial void OnSeriesChanged(string? value) => RefreshDirtyState();
    partial void OnSeriesNumberChanged(decimal? value) => RefreshDirtyState();
    partial void OnIsbnChanged(string? value) => RefreshDirtyState();
    partial void OnReadingStatusChanged(ReadingStatus value) => RefreshDirtyState();
    partial void OnCoverBytesChanged(byte[]? value) => RefreshDirtyState();

    public async Task LoadCustomMetadataValuesAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        if (BookId != bookId || customMetadataRepository is null)
        {
            return;
        }

        var definitions = await customMetadataRepository.ListDefinitionsAsync(cancellationToken);
        var values = await customMetadataRepository.GetValuesAsync(bookId, cancellationToken);
        if (BookId != bookId)
        {
            return;
        }

        var valuesByField = values.ToDictionary(value => value.FieldId);
        ApplyValues(() =>
        {
            CustomMetadataValues.Clear();
            foreach (var definition in definitions)
            {
                valuesByField.TryGetValue(definition.Id, out var value);
                var item = new CustomMetadataValueViewModel(
                    definition.Id,
                    definition.Name,
                    definition.Type,
                    definition.Options,
                    FormatCustomMetadataValue(definition.Type, value));
                item.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(CustomMetadataValueViewModel.ValueText))
                    {
                        RefreshDirtyState();
                    }
                };
                CustomMetadataValues.Add(item);
            }

            originalCustomMetadataValues = SnapshotCustomMetadataValues();
            OnPropertyChanged(nameof(HasCustomMetadataValues));
        });
        RefreshDirtyState();
    }

    private void Apply(Book book, string? libraryPath)
    {
        ApplyValues(() =>
        {
            BookId = book.Id;
            Title = book.Metadata.Title;
            AuthorsText = JoinList(book.Metadata.Authors);
            singleAuthorText = book.Metadata.Authors.Count == 1 ? book.Metadata.Authors[0] : null;
            FormatsText = FormatFormats(book.Formats);
            ApplyFormatFallback(book.Formats);
            Description = CleanDescription(book.Metadata.Description);
            Language = book.Metadata.Language;
            Publisher = book.Metadata.Publisher;
            PublicationDate = book.Metadata.PublicationDate;
            TagsText = book.Metadata.Tags is null ? null : JoinList(book.Metadata.Tags);
            Series = book.Metadata.Series;
            SeriesNumber = book.Metadata.SeriesNumber;
            Isbn = book.Metadata.Isbn;
            ReadingStatus = book.ReadingStatus;
            CoverBytes = book.Metadata.CoverBytes;
            CoverPath = libraryPath is null || string.IsNullOrWhiteSpace(book.CoverRelativePath)
                ? null
                : Path.Combine(libraryPath, book.CoverRelativePath);
        });
    }

    private void RefreshDirtyState()
    {
        if (isApplyingBook)
        {
            return;
        }

        var editedBook = ToBook();
        HasUnsavedChanges = originalBook is not null &&
            editedBook is not null &&
            (!BooksEquivalentForEditing(originalBook, editedBook) ||
                !CustomMetadataValuesEquivalentForEditing(originalCustomMetadataValues, SnapshotCustomMetadataValues()));
    }

    private void ApplyValues(Action apply)
    {
        isApplyingBook = true;
        try
        {
            apply();
        }
        finally
        {
            isApplyingBook = false;
        }
    }

    private bool CanEdit() => originalBook is not null;

    private bool CanChangeCover() => CanEdit() && coverSearchService is not null && showCoverSearch is not null;

    private void NotifyCommandState()
    {
        saveCommand?.NotifyCanExecuteChanged();
        deleteCommand?.NotifyCanExecuteChanged();
        undoCommand?.NotifyCanExecuteChanged();
        swapTitleAndAuthorsCommand?.NotifyCanExecuteChanged();
        changeCoverCommand?.NotifyCanExecuteChanged();
    }

    private static bool CoverEquals(byte[]? first, byte[]? second) =>
        ReferenceEquals(first, second) || first is not null && second is not null && first.SequenceEqual(second);

    private static bool BooksEquivalentForEditing(Book first, Book second) =>
        NormalizeForEditing(first).Metadata == NormalizeForEditing(second).Metadata &&
        first.ReadingStatus == second.ReadingStatus;

    private static Book NormalizeForEditing(Book book) =>
        book with
        {
            Metadata = new BookMetadata(
                book.Metadata.Title.Trim(),
                NormalizeAuthorsForEditing(book.Metadata.Authors),
                CleanDescription(book.Metadata.Description),
                NormalizeBlank(book.Metadata.Language),
                NormalizeBlank(book.Metadata.Publisher),
                book.Metadata.PublicationDate,
                SplitNullableList(book.Metadata.Tags is null ? null : JoinList(book.Metadata.Tags)),
                NormalizeBlank(book.Metadata.Series),
                book.Metadata.SeriesNumber,
                NormalizeBlank(book.Metadata.Isbn),
                book.Metadata.CoverBytes)
        };

    private static string JoinList(IReadOnlyList<string> values) => string.Join("; ", values);

    private static string JoinAuthorsForTitle(IReadOnlyList<string> values) => string.Join(", ", values);

    private IReadOnlyList<string> GetEditedAuthors() =>
        singleAuthorText is not null &&
        string.Equals(AuthorsText, singleAuthorText, StringComparison.Ordinal)
            ? [singleAuthorText]
            : SplitList(AuthorsText);

    private static IReadOnlyList<string> NormalizeAuthorsForEditing(IReadOnlyList<string> authors) =>
        authors.Count == 1
            ? [authors[0].Trim()]
            : SplitList(JoinList(authors));

    private static string FormatFormats(IReadOnlyList<EbookFormat> formats) =>
        string.Join(", ", formats
            .Distinct()
            .OrderBy(format => format)
            .Select(format => format.ToString().ToUpperInvariant()));

    public async Task LoadFormatDetailsAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        if (BookId != bookId)
        {
            return;
        }

        var files = await bookService.ListFilesAsync(bookId, cancellationToken);
        if (BookId != bookId)
        {
            return;
        }

        if (files.Count == 0)
        {
            ApplyFormatFallback(originalBook?.Formats ?? []);
            return;
        }

        FormatDetails.Clear();
        foreach (var file in files
            .OrderBy(file => file.Format)
            .ThenBy(file => file.RelativePath, StringComparer.CurrentCultureIgnoreCase))
        {
            FormatDetails.Add(BookFormatDetailsViewModel.FromFile(
                originalBook,
                file,
                bookService,
                exportService,
                fileInteraction,
                () => FormatDetails.Count));
            FormatDetails[^1].FormatRemoved += OnFormatRemoved;
        }

        FormatsText = FormatFormats(files.Select(file => file.Format).Distinct().ToArray());
    }

    private void OnFormatRemoved(object? sender, BookFormatDetailsViewModel removedFormat)
    {
        FormatDetails.Remove(removedFormat);
        var formats = FormatDetails
            .Select(format => format.Format)
            .Distinct()
            .OrderBy(format => format)
            .ToArray();
        FormatsText = FormatFormats(formats);
        if (originalBook is null)
        {
            return;
        }

        originalBook = originalBook with { Formats = formats };
        BookSaved?.Invoke(this, originalBook);
    }

    private void ApplyFormatFallback(IReadOnlyList<EbookFormat> formats)
    {
        FormatDetails.Clear();
        foreach (var format in formats.Distinct().OrderBy(format => format))
        {
            FormatDetails.Add(BookFormatDetailsViewModel.FromFormat(format));
        }
    }

    private static IReadOnlyList<string> SplitList(string? value) =>
        CustomMetadataValueParser.SplitList(value);

    private static IReadOnlyList<string>? SplitNullableList(string? value)
        => CustomMetadataValueParser.SplitNullableList(value);

    private static string? NormalizeBlank(string? value)
        => CustomMetadataValueParser.NormalizeBlank(value);

    private static string? CleanDescription(string? value) =>
        DescriptionTextCleaner.Clean(value);

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private IReadOnlyList<CustomMetadataValueChange>? TryCreateCustomMetadataChanges(Guid bookId)
    {
        try
        {
            return CustomMetadataValues
                .Select(value => string.IsNullOrWhiteSpace(value.ValueText)
                    ? new CustomMetadataValueChange(bookId, value.FieldId, null)
                    : new CustomMetadataValueChange(bookId, value.FieldId, CreateCustomMetadataValue(bookId, value)))
                .ToList();
        }
        catch (FormatException exception)
        {
            LastSaveResult = new BookSaveResult(BookSaveStatus.Failed, [], exception.Message);
            return null;
        }
        catch (InvalidOperationException exception)
        {
            LastSaveResult = new BookSaveResult(BookSaveStatus.Failed, [], exception.Message);
            return null;
        }
    }

    private async Task<bool> SaveCustomMetadataValuesAsync(
        IReadOnlyList<CustomMetadataValueChange> changes,
        CancellationToken cancellationToken)
    {
        if (customMetadataRepository is null)
        {
            return true;
        }

        try
        {
            foreach (var change in changes)
            {
                if (change.Value is null)
                {
                    await customMetadataRepository.DeleteValueAsync(change.BookId, change.FieldId, cancellationToken);
                    continue;
                }

                await customMetadataRepository.SetValueAsync(change.Value, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            LastSaveResult = new BookSaveResult(BookSaveStatus.Failed, [], exception.Message);
            if (changes.Count > 0)
            {
                await LoadCustomMetadataValuesAsync(changes[0].BookId, cancellationToken);
            }

            return false;
        }

        return true;
    }

    private Dictionary<Guid, string?> SnapshotCustomMetadataValues() =>
        CustomMetadataValues.ToDictionary(
            value => value.FieldId,
            value => NormalizeBlank(value.ValueText),
            EqualityComparer<Guid>.Default);

    private static bool CustomMetadataValuesEquivalentForEditing(
        IReadOnlyDictionary<Guid, string?> original,
        IReadOnlyDictionary<Guid, string?> edited)
    {
        if (original.Count != edited.Count)
        {
            return false;
        }

        return original.All(pair =>
            edited.TryGetValue(pair.Key, out var value) &&
            string.Equals(pair.Value, value, StringComparison.Ordinal));
    }

    private static string? FormatCustomMetadataValue(CustomMetadataFieldType type, CustomMetadataValue? value) =>
        CustomMetadataValueParser.Format(type, value);

    private static CustomMetadataValue CreateCustomMetadataValue(Guid bookId, CustomMetadataValueViewModel value) =>
        CustomMetadataValueParser.Create(bookId, value.FieldId, value.Name, value.Type, value.ValueText);
}

public sealed record CustomMetadataValueChange(Guid BookId, Guid FieldId, CustomMetadataValue? Value);

public sealed partial class CustomMetadataValueViewModel : ObservableObject
{
    private bool isSynchronizingMultiSelectOptions;
    public Guid FieldId { get; }
    public string Name { get; }
    public CustomMetadataFieldType Type { get; }
    public IReadOnlyList<string> Options { get; }
    public IReadOnlyList<string?> SingleSelectOptions { get; }
    public ObservableCollection<CustomMetadataOptionValueViewModel> MultiSelectOptions { get; }
    public bool IsTextEditor => Type == CustomMetadataFieldType.Text;
    public bool IsNumberEditor => Type == CustomMetadataFieldType.Number;
    public bool IsDateEditor => Type == CustomMetadataFieldType.Date;
    public bool IsBooleanEditor => Type == CustomMetadataFieldType.Boolean;
    public bool IsSingleSelectEditor => Type == CustomMetadataFieldType.SingleSelect;
    public bool IsMultiSelectEditor => Type == CustomMetadataFieldType.MultiSelect;

    [ObservableProperty]
    private string? valueText;

    public CustomMetadataValueViewModel(
        Guid fieldId,
        string name,
        CustomMetadataFieldType type,
        IReadOnlyList<string> options,
        string? valueText)
    {
        FieldId = fieldId;
        Name = name;
        Type = type;
        Options = options;
        SingleSelectOptions = new string?[] { null }.Concat(options).ToArray();
        MultiSelectOptions = new ObservableCollection<CustomMetadataOptionValueViewModel>(
            options.Select(option => new CustomMetadataOptionValueViewModel(option)));
        this.valueText = valueText;
        SynchronizeMultiSelectOptionsFromValueText();
        foreach (var option in MultiSelectOptions)
        {
            option.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CustomMetadataOptionValueViewModel.IsSelected))
                {
                    UpdateValueTextFromMultiSelectOptions();
                }
            };
        }
    }

    public bool? BooleanValue
    {
        get => bool.TryParse(ValueText, out var value) ? value : null;
        set => ValueText = value?.ToString(CultureInfo.InvariantCulture);
    }

    public DateTime? DateValue
    {
        get
        {
            if (DateOnly.TryParse(ValueText, CultureInfo.CurrentCulture, out var date) ||
                DateOnly.TryParseExact(ValueText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date.ToDateTime(TimeOnly.MinValue);
            }

            return null;
        }
        set => ValueText = value is null
            ? null
            : DateOnly.FromDateTime(value.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    partial void OnValueTextChanged(string? value)
    {
        OnPropertyChanged(nameof(BooleanValue));
        OnPropertyChanged(nameof(DateValue));
        SynchronizeMultiSelectOptionsFromValueText();
    }

    private void SynchronizeMultiSelectOptionsFromValueText()
    {
        if (Type != CustomMetadataFieldType.MultiSelect || isSynchronizingMultiSelectOptions)
        {
            return;
        }

        isSynchronizingMultiSelectOptions = true;
        try
        {
            var selectedValues = ParseMultiSelectValues(ValueText).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var option in MultiSelectOptions)
            {
                option.IsSelected = selectedValues.Contains(option.Value);
            }
        }
        finally
        {
            isSynchronizingMultiSelectOptions = false;
        }
    }

    private void UpdateValueTextFromMultiSelectOptions()
    {
        if (isSynchronizingMultiSelectOptions)
        {
            return;
        }

        ValueText = string.Join("; ", MultiSelectOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Value));
    }

    private static IEnumerable<string> ParseMultiSelectValues(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

public sealed partial class CustomMetadataOptionValueViewModel(string value) : ObservableObject
{
    public string Value { get; } = value;

    [ObservableProperty]
    private bool isSelected;
}

public sealed partial class BookFormatDetailsViewModel : ObservableObject
{
    private readonly Book? book;
    private readonly BookFile? file;
    private readonly BookService? bookService;
    private readonly BookFileExportService? exportService;
    private readonly IBookFileInteractionService? fileInteraction;
    private readonly Func<int>? formatCountProvider;

    private BookFormatDetailsViewModel(
        Guid? fileId,
        Book? book,
        BookFile? file,
        BookService? bookService,
        EbookFormat format,
        string? relativePath,
        long? sizeBytes,
        BookFileExportService? exportService,
        IBookFileInteractionService? fileInteraction,
        Func<int>? formatCountProvider)
    {
        FileId = fileId;
        this.book = book;
        this.file = file;
        this.bookService = bookService;
        Format = format;
        RelativePath = relativePath;
        SizeBytes = sizeBytes;
        this.exportService = exportService;
        this.fileInteraction = fileInteraction;
        this.formatCountProvider = formatCountProvider;
        OpenFileCommand = new AsyncRelayCommand(
            OpenFileAsync,
            () => CanOpenFile);
        OpenContainingFolderCommand = new AsyncRelayCommand(
            OpenContainingFolderAsync,
            () => CanOpenContainingFolder);
        ExportToDownloadsCommand = new AsyncRelayCommand(
            ExportToDownloadsAsync,
            () => CanExport);
        ExportToFolderCommand = new AsyncRelayCommand(
            ExportToFolderAsync,
            () => CanExport);
        RemoveFormatCommand = new AsyncRelayCommand(
            RemoveFormatAsync,
            () => CanRemoveFormat);
    }

    public Guid? FileId { get; }
    public EbookFormat Format { get; }
    public string? RelativePath { get; }
    public long? SizeBytes { get; }
    public string FormatText => Format.ToString().ToUpperInvariant();
    public string SizeText => SizeBytes is null ? string.Empty : FormatSize(SizeBytes.Value);
    public string DisplayText => string.IsNullOrWhiteSpace(SizeText)
        ? FormatText
        : $"{FormatText} - {SizeText}";
    public bool CanOpenFile =>
        fileInteraction is not null && !string.IsNullOrWhiteSpace(RelativePath);
    public bool CanOpenContainingFolder =>
        fileInteraction is not null && !string.IsNullOrWhiteSpace(RelativePath);
    public bool CanExport =>
        book is not null && file is not null && exportService is not null && fileInteraction is not null;
    public bool CanRemoveFormat =>
        book is not null && file is not null && bookService is not null && fileInteraction is not null;
    [ObservableProperty]
    private BookFormatExportStatusMessage? exportStatusMessage;
    public IAsyncRelayCommand OpenFileCommand { get; }
    public IAsyncRelayCommand OpenContainingFolderCommand { get; }
    public IAsyncRelayCommand ExportToDownloadsCommand { get; }
    public IAsyncRelayCommand ExportToFolderCommand { get; }
    public IAsyncRelayCommand RemoveFormatCommand { get; }
    public event EventHandler<BookFormatDetailsViewModel>? FormatRemoved;

    public static BookFormatDetailsViewModel FromFormat(EbookFormat format) =>
        new(null, null, null, null, format, null, null, null, null, null);

    public static BookFormatDetailsViewModel FromFile(
        Book? book,
        BookFile file,
        BookService? bookService = null,
        BookFileExportService? exportService = null,
        IBookFileInteractionService? fileInteraction = null,
        Func<int>? formatCountProvider = null) =>
        new(file.Id, book, file, bookService, file.Format, file.RelativePath, file.SizeBytes, exportService, fileInteraction, formatCountProvider);

    private async Task OpenFileAsync(CancellationToken cancellationToken)
    {
        if (fileInteraction is null || string.IsNullOrWhiteSpace(RelativePath))
        {
            return;
        }

        var opened = await fileInteraction.OpenFileAsync(RelativePath, cancellationToken);
        if (!opened)
        {
            ExportStatusMessage = BookFormatExportStatusMessage.FileMissing(FormatText);
        }
    }

    private async Task OpenContainingFolderAsync(CancellationToken cancellationToken)
    {
        if (fileInteraction is null || string.IsNullOrWhiteSpace(RelativePath))
        {
            return;
        }

        var opened = await fileInteraction.OpenContainingFolderAsync(RelativePath, cancellationToken);
        if (!opened)
        {
            ExportStatusMessage = BookFormatExportStatusMessage.FolderMissing(FormatText);
        }
    }

    private async Task ExportToDownloadsAsync(CancellationToken cancellationToken)
    {
        if (!CanExport)
        {
            return;
        }

        var folder = await fileInteraction!.GetDefaultExportFolderAsync(cancellationToken);
        await ExportAsync(folder, cancellationToken);
    }

    private async Task ExportToFolderAsync(CancellationToken cancellationToken)
    {
        if (!CanExport)
        {
            return;
        }

        var folder = await fileInteraction!.PickExportFolderAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        await ExportAsync(folder, cancellationToken);
    }

    private async Task ExportAsync(string destinationFolder, CancellationToken cancellationToken)
    {
        var result = await exportService!.ExportAsync(book!, file!, destinationFolder, cancellationToken);
        ExportStatusMessage = result.Status == BookFileExportStatus.Exported
            ? CreateExportSuccessMessage(destinationFolder)
            : BookFormatExportStatusMessage.FromMessage(result.Message);
    }

    private async Task RemoveFormatAsync(CancellationToken cancellationToken)
    {
        if (!CanRemoveFormat)
        {
            return;
        }

        if ((formatCountProvider?.Invoke() ?? 0) <= 1)
        {
            ExportStatusMessage = BookFormatExportStatusMessage.LastFormatCannotRemove(FormatText);
            return;
        }

        if (!await fileInteraction!.ConfirmRemoveFormatAsync(FormatText, cancellationToken))
        {
            return;
        }

        var result = await bookService!.DeleteFileAsync(book!.Id, file!.Id, cancellationToken);
        if (result.Status == BookFileDeleteStatus.Deleted)
        {
            ExportStatusMessage = BookFormatExportStatusMessage.Removed(FormatText);
            FormatRemoved?.Invoke(this, this);
            return;
        }

        ExportStatusMessage = result.Status == BookFileDeleteStatus.LastFormat
            ? BookFormatExportStatusMessage.LastFormatCannotRemove(FormatText)
            : BookFormatExportStatusMessage.RemoveFailed(FormatText);
    }

    private BookFormatExportStatusMessage CreateExportSuccessMessage(string destinationFolder)
    {
        var folderName = Path.GetFileName(destinationFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(folderName))
        {
            folderName = destinationFolder;
        }

        return BookFormatExportStatusMessage.Saved(FormatText, folderName);
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} {units[unitIndex]}"
            : $"{value:0.#} {units[unitIndex]}";
    }
}

public sealed record BookFormatExportStatusMessage(
    string ResourceKey,
    string FormatText,
    string FolderName,
    string? Message = null)
{
    public static BookFormatExportStatusMessage Saved(string formatText, string folderName) =>
        new("FormatSavedToFolder", formatText, folderName);

    public static BookFormatExportStatusMessage FileMissing(string formatText) =>
        new("FormatFileMissing", formatText, string.Empty);

    public static BookFormatExportStatusMessage FolderMissing(string formatText) =>
        new("FormatFolderMissing", formatText, string.Empty);

    public static BookFormatExportStatusMessage Removed(string formatText) =>
        new("FormatRemovedFromLibrary", formatText, string.Empty);

    public static BookFormatExportStatusMessage LastFormatCannotRemove(string formatText) =>
        new("FormatLastFormatCannotRemove", formatText, string.Empty);

    public static BookFormatExportStatusMessage RemoveFailed(string formatText) =>
        new("FormatRemoveFailed", formatText, string.Empty);

    public static BookFormatExportStatusMessage? FromMessage(string? message) =>
        string.IsNullOrWhiteSpace(message)
            ? null
            : new(string.Empty, string.Empty, string.Empty, message);
}
