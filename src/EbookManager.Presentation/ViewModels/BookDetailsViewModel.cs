using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Books;
using System.Collections.ObjectModel;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class BookDetailsViewModel(BookService bookService) : ObservableObject
{
    private readonly BookService bookService = bookService;
    private Book? originalBook;
    private bool isApplyingBook;

    [ObservableProperty]
    private Guid? bookId;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string authorsText = string.Empty;

    [ObservableProperty]
    private string formatsText = string.Empty;

    public ObservableCollection<BookFormatDetailsViewModel> FormatDetails { get; } = [];

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

    public IAsyncRelayCommand SaveCommand => saveCommand ??= new AsyncRelayCommand(SaveAsync, CanEdit);
    public IAsyncRelayCommand DeleteCommand => deleteCommand ??= new AsyncRelayCommand(DeleteAsync, CanEdit);
    public IRelayCommand UndoCommand => undoCommand ??= new RelayCommand(Undo, CanEdit);

    private AsyncRelayCommand? saveCommand;
    private AsyncRelayCommand? deleteCommand;
    private RelayCommand? undoCommand;

    public event EventHandler<Book>? BookSaved;
    public event EventHandler<Guid>? BookDeleted;

    partial void OnLastSaveResultChanged(BookSaveResult? value)
    {
        OnPropertyChanged(nameof(HasSaveError));
        OnPropertyChanged(nameof(SaveErrorMessage));
    }

    public void Load(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        originalBook = book;
        Apply(book);
        LastSaveResult = null;
        LastDeleteResult = null;
        RefreshLocalizedDisplayNames();
        RefreshDirtyState();
        NotifyCommandState();
    }

    public void Clear()
    {
        originalBook = null;
        BookId = null;
        Title = string.Empty;
        AuthorsText = string.Empty;
        FormatsText = string.Empty;
        FormatDetails.Clear();
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
        LastSaveResult = null;
        LastDeleteResult = null;
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
                SplitList(AuthorsText),
                NormalizeBlank(Description),
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

        LastSaveResult = await bookService.SaveAsync(book, cancellationToken);
        if (LastSaveResult.Status == BookSaveStatus.Succeeded)
        {
            originalBook = book;
            RefreshLocalizedDisplayNames();
            RefreshDirtyState();
            BookSaved?.Invoke(this, book);
        }
    }

    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (originalBook is null)
        {
            return;
        }

        LastDeleteResult = await bookService.DeleteAsync(originalBook.Id, cancellationToken);
        if (LastDeleteResult.Status == BookDeleteStatus.Deleted)
        {
            var deletedBookId = originalBook.Id;
            Clear();
            BookDeleted?.Invoke(this, deletedBookId);
        }
    }

    private void Undo()
    {
        if (originalBook is null)
        {
            return;
        }

        Apply(originalBook);
        LastSaveResult = null;
        RefreshDirtyState();
    }

    partial void OnTitleChanged(string value) => RefreshDirtyState();
    partial void OnAuthorsTextChanged(string value) => RefreshDirtyState();
    partial void OnDescriptionChanged(string? value) => RefreshDirtyState();
    partial void OnLanguageChanged(string? value)
    {
        OnPropertyChanged(nameof(LanguageDisplayName));
        RefreshDirtyState();
    }
    partial void OnPublisherChanged(string? value) => RefreshDirtyState();
    partial void OnPublicationDateChanged(DateOnly? value) => RefreshDirtyState();
    partial void OnTagsTextChanged(string? value) => RefreshDirtyState();
    partial void OnSeriesChanged(string? value) => RefreshDirtyState();
    partial void OnSeriesNumberChanged(decimal? value) => RefreshDirtyState();
    partial void OnIsbnChanged(string? value) => RefreshDirtyState();
    partial void OnReadingStatusChanged(ReadingStatus value) => RefreshDirtyState();
    partial void OnCoverBytesChanged(byte[]? value) => RefreshDirtyState();

    private void Apply(Book book)
    {
        ApplyValues(() =>
        {
            BookId = book.Id;
            Title = book.Metadata.Title;
            AuthorsText = JoinList(book.Metadata.Authors);
            FormatsText = FormatFormats(book.Formats);
            ApplyFormatFallback(book.Formats);
            Description = book.Metadata.Description;
            Language = book.Metadata.Language;
            Publisher = book.Metadata.Publisher;
            PublicationDate = book.Metadata.PublicationDate;
            TagsText = book.Metadata.Tags is null ? null : JoinList(book.Metadata.Tags);
            Series = book.Metadata.Series;
            SeriesNumber = book.Metadata.SeriesNumber;
            Isbn = book.Metadata.Isbn;
            ReadingStatus = book.ReadingStatus;
            CoverBytes = book.Metadata.CoverBytes;
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
            !BooksEquivalentForEditing(originalBook, editedBook);
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

    private void NotifyCommandState()
    {
        saveCommand?.NotifyCanExecuteChanged();
        deleteCommand?.NotifyCanExecuteChanged();
        undoCommand?.NotifyCanExecuteChanged();
    }

    private static bool BooksEquivalentForEditing(Book first, Book second) =>
        NormalizeForEditing(first).Metadata == NormalizeForEditing(second).Metadata &&
        first.ReadingStatus == second.ReadingStatus;

    private static Book NormalizeForEditing(Book book) =>
        book with
        {
            Metadata = new BookMetadata(
                book.Metadata.Title.Trim(),
                SplitList(JoinList(book.Metadata.Authors)),
                NormalizeBlank(book.Metadata.Description),
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

        FormatDetails.Clear();
        foreach (var file in files
            .OrderBy(file => file.Format)
            .ThenBy(file => file.RelativePath, StringComparer.CurrentCultureIgnoreCase))
        {
            FormatDetails.Add(BookFormatDetailsViewModel.FromFile(file));
        }

        if (files.Count > 0)
        {
            FormatsText = FormatFormats(files.Select(file => file.Format).Distinct().ToArray());
        }
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
        (value ?? string.Empty)
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static IReadOnlyList<string>? SplitNullableList(string? value)
    {
        var values = SplitList(value);
        return values.Count == 0 ? null : values;
    }

    private static string? NormalizeBlank(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);
}

public sealed class BookFormatDetailsViewModel
{
    private BookFormatDetailsViewModel(
        Guid? fileId,
        EbookFormat format,
        string? relativePath,
        long? sizeBytes)
    {
        FileId = fileId;
        Format = format;
        RelativePath = relativePath;
        SizeBytes = sizeBytes;
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

    public static BookFormatDetailsViewModel FromFormat(EbookFormat format) =>
        new(null, format, null, null);

    public static BookFormatDetailsViewModel FromFile(BookFile file) =>
        new(file.Id, file.Format, file.RelativePath, file.SizeBytes);

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
