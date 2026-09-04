using EbookManager.Application.Books;
using EbookManager.Application.Metadata;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Domain.Metadata;
using EbookManager.Presentation.Abstractions;
using EbookManager.Presentation.ViewModels;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using System.Globalization;

namespace EbookManager.Tests.App.ViewModels;

public sealed class BookDetailsViewModelTests
{
    [Fact]
    public async Task Change_cover_is_available_for_an_existing_cover_and_can_be_undone()
    {
        var originalBytes = new byte[] { 1, 2, 3 };
        var replacementBytes = new byte[] { 4, 5, 6 };
        var candidate = new BookCoverCandidate(
            "source", "candidate", "Source", "New title", ["Author"], replacementBytes, 400, 600);
        var searchService = new StubCoverSearchService(candidate, replacementBytes);
        var viewModel = CreateViewModel(
            out _,
            coverSearchService: searchService,
            showCoverSearch: async (search, cancellationToken) =>
            {
                await search.LoadAsync(cancellationToken);
                search.SelectedCandidate = search.Candidates.Single();
                return true;
            });
        var book = WithCover(CreateBook("Original", ["First Author"]), originalBytes);
        viewModel.Load(book);
        viewModel.Title = "Edited title";

        viewModel.ChangeCoverCommand.CanExecute(null).Should().BeTrue();
        await viewModel.ChangeCoverCommand.ExecuteAsync(null);

        searchService.Query.Should().BeEquivalentTo(new BookCoverSearchQuery(
            "Edited title", ["First Author"], "9780000000000"));
        searchService.DownloadedCandidate.Should().BeSameAs(candidate);
        viewModel.CoverBytes.Should().Equal(replacementBytes);
        viewModel.HasUnsavedChanges.Should().BeTrue();

        viewModel.UndoCommand.Execute(null);

        viewModel.CoverBytes.Should().Equal(originalBytes);
        viewModel.Title.Should().Be("Original");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Saving_a_selected_cover_uses_safe_cover_update_and_notifies_the_saved_book()
    {
        var replacementBytes = new byte[] { 4, 5, 6 };
        var candidate = new BookCoverCandidate(
            "source", "candidate", "Source", "Title", ["Author"], replacementBytes, 400, 600);
        var searchService = new StubCoverSearchService(candidate, replacementBytes);
        var updateService = new StubCoverUpdateService();
        var viewModel = CreateViewModel(
            out _,
            coverSearchService: searchService,
            showCoverSearch: async (search, cancellationToken) =>
            {
                await search.LoadAsync(cancellationToken);
                search.SelectedCandidate = search.Candidates.Single();
                return true;
            },
            coverUpdateService: updateService);
        var book = WithCover(CreateBook("Original", ["First Author"]), [1, 2, 3]);
        Book? notified = null;
        viewModel.BookSaved += (_, saved) => notified = saved;
        viewModel.Load(book);

        await viewModel.ChangeCoverCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        updateService.CoverBytes.Should().Equal(replacementBytes);
        updateService.EditedBook!.Metadata.CoverBytes.Should().Equal(replacementBytes);
        notified!.CoverRelativePath.Should().Be($"books/{book.Id:N}/cover.jpg");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Loading_a_book_does_not_set_dirty_state()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"]);

        viewModel.Load(book);

        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Loading_a_book_with_metadata_whitespace_does_not_set_dirty_state()
    {
        var viewModel = CreateViewModel(out _);
        var now = DateTimeOffset.UtcNow;
        var book = new Book(
            Guid.NewGuid(),
            new BookMetadata(
                " Original ",
                [" First Author "],
                Description: " Description ",
                Language: " en ",
                Publisher: " Publisher ",
                Tags: [" Tag "],
                Series: " Series ",
                Isbn: " 9780000000000 "),
            ReadingStatus.Unread,
            null,
            now,
            now);

        viewModel.Load(book);

        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Editing_metadata_sets_dirty_state_and_undo_restores_original_values()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"]);

        viewModel.Load(book);
        viewModel.Title = "Changed";

        viewModel.HasUnsavedChanges.Should().BeTrue();
        viewModel.UndoCommand.Execute(null);

        viewModel.Title.Should().Be("Original");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Swap_title_and_authors_updates_edit_fields_and_undo_restores_original_values()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Deel 1; Deel 2", ["Author One", "Author Two"]);

        viewModel.Load(book);
        viewModel.SwapTitleAndAuthorsCommand.Execute(null);

        viewModel.Title.Should().Be("Author One, Author Two");
        viewModel.AuthorsText.Should().Be("Deel 1; Deel 2");
        viewModel.HasUnsavedChanges.Should().BeTrue();
        viewModel.ToBook()!.Metadata.Authors.Should().Equal("Deel 1; Deel 2");

        viewModel.UndoCommand.Execute(null);

        viewModel.Title.Should().Be("Deel 1; Deel 2");
        viewModel.AuthorsText.Should().Be("Author One; Author Two");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Loading_a_book_shows_available_formats_without_setting_dirty_state()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Pdf, EbookFormat.Epub]);

        viewModel.Load(book);

        viewModel.FormatsText.Should().Be("EPUB, PDF");
        viewModel.FormatDetails.Select(format => format.DisplayText).Should().Equal("EPUB", "PDF");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Loading_custom_metadata_values_does_not_set_dirty_state()
    {
        var customMetadataRepository = new RecordingCustomMetadataRepository();
        var definition = customMetadataRepository.AddDefinition("Eigen rating", CustomMetadataFieldType.Number);
        var viewModel = CreateViewModel(out _, customMetadataRepository: customMetadataRepository);
        var book = CreateBook("Original", ["First Author"]);
        customMetadataRepository.SeedValue(new CustomMetadataValue(book.Id, definition.Id, NumberValue: 4.5m));

        viewModel.Load(book);
        await viewModel.LoadCustomMetadataValuesAsync(book.Id);

        viewModel.CustomMetadataValues.Should().ContainSingle(value =>
            value.Name == "Eigen rating" &&
            value.Type == CustomMetadataFieldType.Number &&
            value.ValueText == 4.5m.ToString("0.#############################", CultureInfo.CurrentCulture));
        viewModel.HasCustomMetadataValues.Should().BeTrue();
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Editing_custom_metadata_value_sets_dirty_state_and_save_persists_value()
    {
        var customMetadataRepository = new RecordingCustomMetadataRepository();
        var definition = customMetadataRepository.AddDefinition("Leesprioriteit", CustomMetadataFieldType.Text);
        var viewModel = CreateViewModel(out _, customMetadataRepository: customMetadataRepository);
        var book = CreateBook("Original", ["First Author"]);
        customMetadataRepository.SeedValue(new CustomMetadataValue(book.Id, definition.Id, TextValue: "Later"));

        viewModel.Load(book);
        await viewModel.LoadCustomMetadataValuesAsync(book.Id);
        viewModel.CustomMetadataValues.Single().ValueText = "Nu";

        viewModel.HasUnsavedChanges.Should().BeTrue();
        await viewModel.SaveCommand.ExecuteAsync(null);

        customMetadataRepository.Values[(book.Id, definition.Id)].TextValue.Should().Be("Nu");
        viewModel.HasUnsavedChanges.Should().BeFalse();

        viewModel.CustomMetadataValues.Single().ValueText = string.Empty;
        viewModel.HasUnsavedChanges.Should().BeTrue();
        await viewModel.SaveCommand.ExecuteAsync(null);

        customMetadataRepository.Values.Should().NotContainKey((book.Id, definition.Id));
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Theory]
    [InlineData("Ja", true)]
    [InlineData("Nein", false)]
    [InlineData("Oui", true)]
    [InlineData("No", false)]
    [InlineData("Sí", true)]
    [InlineData("Sì", true)]
    public async Task Boolean_custom_metadata_accepts_supported_localized_yes_no_values(
        string input,
        bool expected)
    {
        var customMetadataRepository = new RecordingCustomMetadataRepository();
        var definition = customMetadataRepository.AddDefinition("Gelezen", CustomMetadataFieldType.Boolean);
        var viewModel = CreateViewModel(out _, customMetadataRepository: customMetadataRepository);
        var book = CreateBook("Original", ["First Author"]);

        viewModel.Load(book);
        await viewModel.LoadCustomMetadataValuesAsync(book.Id);
        viewModel.CustomMetadataValues.Single().ValueText = input;
        await viewModel.SaveCommand.ExecuteAsync(null);

        customMetadataRepository.Values[(book.Id, definition.Id)].BooleanValue.Should().Be(expected);
    }

    [Fact]
    public async Task Select_custom_metadata_uses_configured_options_and_persists_selected_values()
    {
        var customMetadataRepository = new RecordingCustomMetadataRepository();
        var single = customMetadataRepository.AddDefinition(
            "Leesprioriteit",
            CustomMetadataFieldType.SingleSelect,
            ["Hoog", "Normaal", "Laag"]);
        var multi = customMetadataRepository.AddDefinition(
            "Genres",
            CustomMetadataFieldType.MultiSelect,
            ["Thriller", "Historisch", "Fantasy"]);
        var viewModel = CreateViewModel(out _, customMetadataRepository: customMetadataRepository);
        var book = CreateBook("Original", ["First Author"]);

        viewModel.Load(book);
        await viewModel.LoadCustomMetadataValuesAsync(book.Id);
        var singleValue = viewModel.CustomMetadataValues.Single(value => value.FieldId == single.Id);
        singleValue.SingleSelectOptions.Should().Equal(null, "Hoog", "Normaal", "Laag");
        singleValue.ValueText = "Hoog";
        var multiValue = viewModel.CustomMetadataValues.Single(value => value.FieldId == multi.Id);
        multiValue.MultiSelectOptions.Single(option => option.Value == "Thriller").IsSelected = true;
        multiValue.MultiSelectOptions.Single(option => option.Value == "Fantasy").IsSelected = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        customMetadataRepository.Values[(book.Id, single.Id)].TextValue.Should().Be("Hoog");
        customMetadataRepository.Values[(book.Id, multi.Id)].TextValue.Should().Be("Thriller; Fantasy");
    }

    [Fact]
    public async Task Invalid_custom_metadata_value_does_not_save_standard_metadata()
    {
        var customMetadataRepository = new RecordingCustomMetadataRepository();
        customMetadataRepository.AddDefinition("Cijfer", CustomMetadataFieldType.Number);
        var viewModel = CreateViewModel(out var repository, customMetadataRepository: customMetadataRepository);
        var book = CreateBook("Original", ["First Author"]);

        viewModel.Load(book);
        await viewModel.LoadCustomMetadataValuesAsync(book.Id);
        viewModel.Title = "Changed";
        viewModel.CustomMetadataValues.Single().ValueText = "abc";
        await viewModel.SaveCommand.ExecuteAsync(null);

        repository.UpdatedBook.Should().BeNull();
        viewModel.SaveErrorMessage.Should().Be("CustomMetadataValidationNumber|Cijfer");
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Undo_restores_custom_metadata_values()
    {
        var customMetadataRepository = new RecordingCustomMetadataRepository();
        var definition = customMetadataRepository.AddDefinition("Leesprioriteit", CustomMetadataFieldType.Text);
        var viewModel = CreateViewModel(out _, customMetadataRepository: customMetadataRepository);
        var book = CreateBook("Original", ["First Author"]);
        customMetadataRepository.SeedValue(new CustomMetadataValue(book.Id, definition.Id, TextValue: "Later"));

        viewModel.Load(book);
        await viewModel.LoadCustomMetadataValuesAsync(book.Id);
        viewModel.CustomMetadataValues.Single().ValueText = "Nu";
        viewModel.UndoCommand.Execute(null);

        viewModel.CustomMetadataValues.Single().ValueText.Should().Be("Later");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Loading_a_book_cleans_html_description_without_setting_dirty_state()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"]) with
        {
            Metadata = new BookMetadata(
                "Original",
                ["First Author"],
                Description: "<p class=\"description\">First line.<br><br>Second &amp; final line.</p>",
                Language: "en")
        };

        viewModel.Load(book);

        viewModel.Description.Should().Be("First line.\n\nSecond & final line.");
        viewModel.HasUnsavedChanges.Should().BeFalse();
        viewModel.ToBook()!.Metadata.Description.Should().Be("First line.\n\nSecond & final line.");
    }

    [Fact]
    public async Task Loading_format_details_shows_file_sizes_per_available_format()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var fileInteraction = new RecordingBookFileInteractionService();
            var viewModel = CreateViewModel(out var repository, fileInteraction);
            var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub, EbookFormat.Pdf]);
            repository.SeedFiles(
                book.Id,
                [
                    CreateBookFile(book.Id, EbookFormat.Pdf, "books/book/original.pdf", 4_404_019),
                    CreateBookFile(book.Id, EbookFormat.Epub, "books/book/original.epub", 1_887_436)
                ]);

            viewModel.Load(book);
            await viewModel.LoadFormatDetailsAsync(book.Id);

            viewModel.FormatDetails.Select(format => format.DisplayText)
                .Should()
                .Equal("EPUB - 1.8 MB", "PDF - 4.2 MB");
            viewModel.FormatDetails.Should().AllSatisfy(format => format.FileId.Should().NotBeNull());
            await viewModel.FormatDetails[0].OpenFileCommand.ExecuteAsync(null);
            await viewModel.FormatDetails[0].OpenContainingFolderCommand.ExecuteAsync(null);
            fileInteraction.OpenedFiles.Should().Equal("books/book/original.epub");
            fileInteraction.OpenedRelativePaths.Should().Equal("books/book/original.epub");
            viewModel.HasUnsavedChanges.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task Loading_format_details_keeps_fallback_formats_when_no_files_are_available()
    {
        var viewModel = CreateViewModel(out _);
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub, EbookFormat.Pdf]);

        viewModel.Load(book);
        await viewModel.LoadFormatDetailsAsync(book.Id);

        viewModel.FormatDetails.Select(format => format.DisplayText).Should().Equal("EPUB", "PDF");
        viewModel.FormatsText.Should().Be("EPUB, PDF");
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Format_details_can_export_to_downloads()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var libraryRoot = temporaryDirectory.CreateSubdirectory("Library").FullName;
        var downloads = temporaryDirectory.CreateSubdirectory("Downloads").FullName;
        var sourcePath = Path.Combine(libraryRoot, "books", "book", "original.epub");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, "content");
        var fileInteraction = new RecordingBookFileInteractionService
        {
            DownloadsFolder = downloads
        };
        var fileStore = new RootedLibraryFileStore(libraryRoot);
        var viewModel = CreateViewModel(
            out var repository,
            fileInteraction,
            fileStore,
            new BookFileExportService(fileStore));
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub]);
        repository.SeedFiles(
            book.Id,
            [CreateBookFile(book.Id, EbookFormat.Epub, "books/book/original.epub", 1_887_436)]);

        viewModel.Load(book);
        await viewModel.LoadFormatDetailsAsync(book.Id);
        await viewModel.FormatDetails[0].ExportToDownloadsCommand.ExecuteAsync(null);

        var exportedPath = Path.Combine(downloads, "First Author - Original.epub");
        File.Exists(exportedPath).Should().BeTrue();
        File.ReadAllText(exportedPath).Should().Be("content");
        viewModel.FormatDetails[0].ExportStatusMessage.Should().Be(
            BookFormatExportStatusMessage.Saved("EPUB", "Downloads"));
    }

    [Fact]
    public async Task Open_file_reports_missing_managed_file()
    {
        var fileInteraction = new RecordingBookFileInteractionService
        {
            ShouldOpenFile = false
        };
        var viewModel = CreateViewModel(out var repository, fileInteraction);
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub]);
        repository.SeedFiles(
            book.Id,
            [CreateBookFile(book.Id, EbookFormat.Epub, "books/book/missing.epub", 1_887_436)]);

        viewModel.Load(book);
        await viewModel.LoadFormatDetailsAsync(book.Id);
        await viewModel.FormatDetails[0].OpenFileCommand.ExecuteAsync(null);

        viewModel.FormatDetails[0].ExportStatusMessage.Should().Be(
            BookFormatExportStatusMessage.FileMissing("EPUB"));
    }

    [Fact]
    public async Task Open_folder_reports_missing_managed_folder()
    {
        var fileInteraction = new RecordingBookFileInteractionService
        {
            ShouldOpenContainingFolder = false
        };
        var viewModel = CreateViewModel(out var repository, fileInteraction);
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub]);
        repository.SeedFiles(
            book.Id,
            [CreateBookFile(book.Id, EbookFormat.Epub, "books/book/missing.epub", 1_887_436)]);

        viewModel.Load(book);
        await viewModel.LoadFormatDetailsAsync(book.Id);
        await viewModel.FormatDetails[0].OpenContainingFolderCommand.ExecuteAsync(null);

        viewModel.FormatDetails[0].ExportStatusMessage.Should().Be(
            BookFormatExportStatusMessage.FolderMissing("EPUB"));
    }

    [Fact]
    public async Task Remove_format_removes_only_selected_format_when_multiple_formats_exist()
    {
        var fileInteraction = new RecordingBookFileInteractionService();
        var viewModel = CreateViewModel(out var repository, fileInteraction);
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub, EbookFormat.Pdf]);
        var epubFile = CreateBookFile(book.Id, EbookFormat.Epub, "books/book/original.epub", 1_887_436);
        var pdfFile = CreateBookFile(book.Id, EbookFormat.Pdf, "books/book/original.pdf", 4_404_019);
        repository.SeedFiles(book.Id, [epubFile, pdfFile]);
        Book? savedBook = null;
        viewModel.BookSaved += (_, book) => savedBook = book;

        viewModel.Load(book);
        await viewModel.LoadFormatDetailsAsync(book.Id);
        await viewModel.FormatDetails[0].RemoveFormatCommand.ExecuteAsync(null);

        fileInteraction.ConfirmRemoveFormatCalls.Should().Be(1);
        viewModel.FormatDetails.Select(format => format.Format).Should().Equal(EbookFormat.Pdf);
        viewModel.FormatsText.Should().Be("PDF");
        repository.FilesFor(book.Id).Select(file => file.Format).Should().Equal(EbookFormat.Pdf);
        savedBook!.Formats.Should().Equal(EbookFormat.Pdf);
    }

    [Fact]
    public async Task Remove_format_blocks_last_format_and_keeps_book()
    {
        var fileInteraction = new RecordingBookFileInteractionService();
        var viewModel = CreateViewModel(out var repository, fileInteraction);
        var book = CreateBook("Original", ["First Author"], [EbookFormat.Epub]);
        repository.SeedFiles(
            book.Id,
            [CreateBookFile(book.Id, EbookFormat.Epub, "books/book/original.epub", 1_887_436)]);

        viewModel.Load(book);
        await viewModel.LoadFormatDetailsAsync(book.Id);
        await viewModel.FormatDetails[0].RemoveFormatCommand.ExecuteAsync(null);

        fileInteraction.ConfirmRemoveFormatCalls.Should().Be(0);
        viewModel.FormatDetails.Should().ContainSingle();
        viewModel.FormatDetails[0].ExportStatusMessage.Should().Be(
            BookFormatExportStatusMessage.LastFormatCannotRemove("EPUB"));
        repository.FilesFor(book.Id).Should().ContainSingle();
    }

    [Fact]
    public void Load_exposes_standard_metadata_fields()
    {
        var viewModel = CreateViewModel(out _);
        var created = new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.Zero);
        var updated = new DateTimeOffset(2026, 7, 16, 11, 45, 0, TimeSpan.Zero);
        var book = new Book(
            Guid.NewGuid(),
            new BookMetadata(
                "Title",
                ["Author"],
                "Description",
                "nl",
                "Publisher",
                new DateOnly(2020, 1, 2),
                ["Tag"],
                "Series",
                1,
                "9780000000000"),
            ReadingStatus.Read,
            null,
            created,
            updated)
        {
            Formats = [EbookFormat.Epub, EbookFormat.Pdf]
        };

        viewModel.Load(book);

        viewModel.Title.Should().Be("Title");
        viewModel.AuthorsText.Should().Be("Author");
        viewModel.Description.Should().Be("Description");
        viewModel.Language.Should().Be("nl");
        viewModel.Publisher.Should().Be("Publisher");
        viewModel.PublicationDate.Should().Be(new DateOnly(2020, 1, 2));
        viewModel.TagsText.Should().Be("Tag");
        viewModel.Series.Should().Be("Series");
        viewModel.SeriesNumber.Should().Be(1);
        viewModel.Isbn.Should().Be("9780000000000");
        viewModel.FormatsText.Should().Be("EPUB, PDF");
        viewModel.ReadingStatus.Should().Be(ReadingStatus.Read);
        viewModel.CreatedUtcText.Should().Be(created.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
        viewModel.UpdatedUtcText.Should().Be(updated.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Load_exposes_friendly_language_display_without_changing_stored_value()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
        try
        {
            var viewModel = CreateViewModel(out _);
            var book = CreateBook("Original", ["First Author"], language: "eng");

            viewModel.Load(book);

            viewModel.Language.Should().Be("eng");
            viewModel.LanguageDisplayName.Should().Be("Engels");
            viewModel.HasUnsavedChanges.Should().BeFalse();

            viewModel.Language = "nl-NL";

            viewModel.Language.Should().Be("nl-NL");
            viewModel.LanguageDisplayName.Should().Be("Nederlands");
            viewModel.HasUnsavedChanges.Should().BeTrue();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Refresh_localized_display_names_updates_language_display_without_dirtying_book()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl-NL");
            var viewModel = CreateViewModel(out _);
            var book = CreateBook("Original", ["First Author"], language: "nl") with
            {
                CreatedUtc = new DateTimeOffset(2026, 7, 15, 10, 30, 0, TimeSpan.Zero),
                UpdatedUtc = new DateTimeOffset(2026, 7, 16, 11, 45, 0, TimeSpan.Zero)
            };
            viewModel.Load(book);

            viewModel.LanguageDisplayName.Should().Be("Nederlands");
            var originalCreatedText = viewModel.CreatedUtcText;
            viewModel.HasUnsavedChanges.Should().BeFalse();

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            viewModel.RefreshLocalizedDisplayNames();

            viewModel.Language.Should().Be("nl");
            viewModel.LanguageDisplayName.Should().Be("Dutch");
            viewModel.CreatedUtcText.Should().NotBe(originalCreatedText);
            viewModel.HasUnsavedChanges.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task Save_updates_metadata_and_clears_dirty_state()
    {
        var viewModel = CreateViewModel(out var repository);
        var book = CreateBook("Original", ["First Author"]);
        viewModel.Load(book);
        viewModel.Title = "Changed";
        viewModel.AuthorsText = "Second Author; Third Author";
        viewModel.ReadingStatus = ReadingStatus.Read;

        await viewModel.SaveCommand.ExecuteAsync(null);

        repository.UpdatedBook.Should().NotBeNull();
        repository.UpdatedBook!.Metadata.Title.Should().Be("Changed");
        repository.UpdatedBook.Metadata.Authors.Should().Equal("Second Author", "Third Author");
        repository.UpdatedBook.ReadingStatus.Should().Be(ReadingStatus.Read);
        viewModel.LastSaveResult!.Status.Should().Be(BookSaveStatus.Succeeded);
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Save_conflict_keeps_dirty_state_and_exposes_save_error()
    {
        var viewModel = CreateViewModel(out var repository);
        repository.ThrowConflictOnUpdate = true;
        var book = CreateBook("Original", ["First Author"]);
        viewModel.Load(book);
        viewModel.AuthorsText = "Second Author";

        await viewModel.SaveCommand.ExecuteAsync(null);

        viewModel.LastSaveResult!.Status.Should().Be(BookSaveStatus.Conflict);
        viewModel.HasSaveError.Should().BeTrue();
        viewModel.SaveErrorMessage.Should().Be("A book with the same title and author already exists.");
        viewModel.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_removes_loaded_book_and_clears_details()
    {
        var viewModel = CreateViewModel(out var repository);
        var book = CreateBook("Original", ["First Author"]);
        viewModel.Load(book);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        repository.DeletedBookId.Should().Be(book.Id);
        viewModel.BookId.Should().BeNull();
        viewModel.LastDeleteResult.Should().BeNull();
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_clears_details_and_preserves_warning_when_file_cleanup_returns_warning()
    {
        var viewModel = CreateViewModel(out var repository, fileStore: new ThrowingLibraryFileStore());
        var book = CreateBook("Original", ["First Author"]);
        viewModel.Load(book);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        repository.DeletedBookId.Should().Be(book.Id);
        viewModel.BookId.Should().BeNull();
        viewModel.LastDeleteResult.Should().Be(new BookDeleteResult(BookDeleteStatus.Deleted, "cleanup failed"));
        viewModel.HasUnsavedChanges.Should().BeFalse();
    }

    private static BookDetailsViewModel CreateViewModel(
        out RecordingBookRepository repository,
        IBookFileInteractionService? fileInteraction = null,
        ILibraryFileStore? fileStore = null,
        BookFileExportService? exportService = null,
        ICustomMetadataRepository? customMetadataRepository = null,
        IBookCoverSearchService? coverSearchService = null,
        Func<MetadataQualityCoverSearchViewModel, CancellationToken, Task<bool>>? showCoverSearch = null,
        IBookCoverUpdateService? coverUpdateService = null)
    {
        repository = new RecordingBookRepository();
        var service = new BookService(
            repository,
            fileStore ?? new NoopLibraryFileStore(),
            new NoopMetadataAdapterResolver());
        return new BookDetailsViewModel(
            service,
            exportService,
            fileInteraction,
            customMetadataRepository,
            coverSearchService,
            showCoverSearch,
            coverUpdateService,
            key => $"loc:{key}");
    }

    private static Book WithCover(Book book, byte[] bytes) => book with
    {
        Metadata = new BookMetadata(
            book.Metadata.Title,
            book.Metadata.Authors,
            book.Metadata.Description,
            book.Metadata.Language,
            book.Metadata.Publisher,
            book.Metadata.PublicationDate,
            book.Metadata.Tags,
            book.Metadata.Series,
            book.Metadata.SeriesNumber,
            book.Metadata.Isbn,
            bytes),
        CoverRelativePath = $"books/{book.Id:N}/cover.jpg"
    };

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        IReadOnlyList<EbookFormat>? formats = null,
        string? language = "en")
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            Guid.NewGuid(),
            new BookMetadata(
                title,
                authors,
                Description: "Description",
                Language: language,
                Publisher: "Publisher",
                Tags: ["Tag"],
                Isbn: "9780000000000"),
            ReadingStatus.Unread,
            null,
            now,
            now)
        {
            Formats = formats ?? []
        };
    }

    private static BookFile CreateBookFile(
        Guid bookId,
        EbookFormat format,
        string relativePath,
        long sizeBytes) =>
        new(
            Guid.NewGuid(),
            bookId,
            format,
            relativePath,
            new string('a', 64),
            sizeBytes,
            MetadataWriteBackStatus.Unsupported,
            null);

    private sealed class StubCoverSearchService(
        BookCoverCandidate candidate,
        byte[] bytes) : IBookCoverSearchService
    {
        public BookCoverSearchQuery? Query { get; private set; }
        public BookCoverCandidate? DownloadedCandidate { get; private set; }

        public Task<BookCoverSearchResult> SearchAsync(BookCoverSearchQuery query, CancellationToken cancellationToken)
        {
            Query = query;
            return Task.FromResult(new BookCoverSearchResult(BookCoverSearchStatus.Succeeded, [candidate]));
        }

        public Task<BookCoverDownloadResult> DownloadAsync(BookCoverCandidate selected, CancellationToken cancellationToken)
        {
            DownloadedCandidate = selected;
            return Task.FromResult(new BookCoverDownloadResult(BookCoverDownloadStatus.Succeeded, bytes, 400, 600));
        }
    }

    private sealed class StubCoverUpdateService : IBookCoverUpdateService
    {
        public Book? EditedBook { get; private set; }
        public byte[]? CoverBytes { get; private set; }

        public Task<BookCoverUpdateResult> UpdateAsync(Book editedBook, byte[] coverBytes, CancellationToken cancellationToken)
        {
            EditedBook = editedBook;
            CoverBytes = coverBytes;
            var saved = editedBook with { CoverRelativePath = $"books/{editedBook.Id:N}/cover.jpg" };
            return Task.FromResult(new BookCoverUpdateResult(
                new BookSaveResult(BookSaveStatus.Succeeded, []),
                saved));
        }
    }

    private sealed class RecordingBookRepository : IBookRepository
    {
        private readonly Dictionary<Guid, IReadOnlyList<BookFile>> filesByBookId = [];
        public Book? UpdatedBook { get; private set; }
        public Guid? DeletedBookId { get; private set; }
        public bool ThrowConflictOnUpdate { get; set; }

        public void SeedFiles(Guid bookId, IReadOnlyList<BookFile> files)
        {
            filesByBookId[bookId] = files;
        }

        public IReadOnlyList<BookFile> FilesFor(Guid bookId) =>
            filesByBookId.TryGetValue(bookId, out var files) ? files : [];

        public Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Book?>(null);
        public Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Book?> FindByNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult<Book?>(null);
        public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(string title, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddFileAsync(BookFile file, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AttachFilesToBookAsync(Guid sourceBookId, Guid targetBookId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            if (ThrowConflictOnUpdate)
            {
                throw new BookConflictException();
            }

            UpdatedBook = book;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            DeletedBookId = id;
            return Task.CompletedTask;
        }

        public Task<BookFileDeleteRepositoryResult> DeleteFileAsync(
            Guid bookId,
            Guid fileId,
            CancellationToken cancellationToken)
        {
            if (!filesByBookId.TryGetValue(bookId, out var files))
            {
                return Task.FromResult(new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.NotFound));
            }

            var remaining = files.Where(file => file.Id != fileId).ToArray();
            if (remaining.Length == files.Count)
            {
                return Task.FromResult(new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.NotFound));
            }

            filesByBookId[bookId] = remaining;
            return Task.FromResult(new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.Deleted));
        }

        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult(filesByBookId.TryGetValue(bookId, out var files)
                ? files
                : (IReadOnlyList<BookFile>)[]);

        public Task UpdateFileWriteBackAsync(Guid fileId, MetadataWriteResult result, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingCustomMetadataRepository : ICustomMetadataRepository
    {
        private readonly List<CustomMetadataFieldDefinition> definitions = [];
        public Dictionary<(Guid BookId, Guid FieldId), CustomMetadataValue> Values { get; } = [];

        public CustomMetadataFieldDefinition AddDefinition(
            string name,
            CustomMetadataFieldType type,
            IReadOnlyList<string>? options = null)
        {
            var now = DateTimeOffset.UtcNow;
            var definition = new CustomMetadataFieldDefinition(
                Guid.NewGuid(),
                name.ToLowerInvariant().Replace(' ', '-'),
                name,
                type,
                options ?? [],
                definitions.Count,
                now,
                now);
            definitions.Add(definition);
            return definition;
        }

        public void SeedValue(CustomMetadataValue value)
        {
            Values[(value.BookId, value.FieldId)] = value;
        }

        public Task<IReadOnlyList<CustomMetadataFieldDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomMetadataFieldDefinition>>(definitions);

        public Task<CustomMetadataFieldDefinition> AddDefinitionAsync(
            string name,
            CustomMetadataFieldType type,
            CancellationToken cancellationToken) =>
            Task.FromResult(AddDefinition(name, type));

        public Task RenameDefinitionAsync(Guid fieldId, string name, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateDefinitionOptionsAsync(
            Guid fieldId,
            IReadOnlyList<string> options,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteDefinitionAsync(Guid fieldId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<CustomMetadataValue>> GetValuesAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomMetadataValue>>(
                Values.Values.Where(value => value.BookId == bookId).ToList());

        public Task<IReadOnlyList<CustomMetadataValue>> GetValuesForBooksAsync(
            IReadOnlyCollection<Guid> bookIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomMetadataValue>>(
                Values.Values.Where(value => bookIds.Contains(value.BookId)).ToList());

        public Task SetValueAsync(CustomMetadataValue value, CancellationToken cancellationToken)
        {
            Values[(value.BookId, value.FieldId)] = value;
            return Task.CompletedTask;
        }

        public Task DeleteValueAsync(Guid bookId, Guid fieldId, CancellationToken cancellationToken)
        {
            Values.Remove((bookId, fieldId));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> CleanupFilterValueAsync(
            Guid fieldId,
            string oldValue,
            string? replacementValue,
            bool remove,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class NoopLibraryFileStore : ILibraryFileStore
    {
        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
            Guid bookId,
            string sourcePath,
            byte[]? coverBytes,
            CancellationToken cancellationToken) =>
            Task.FromResult(($"books/{bookId:N}/book.epub", (string?)null));

        public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken) => Task.CompletedTask;
        public string GetAbsolutePath(string relativePath) => relativePath;
    }

    private sealed class RootedLibraryFileStore(string rootPath) : ILibraryFileStore
    {
        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
            Guid bookId,
            string sourcePath,
            byte[]? coverBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public string GetAbsolutePath(string relativePath) => Path.Combine(rootPath, relativePath);
    }

    private sealed class ThrowingLibraryFileStore : ILibraryFileStore
    {
        public Task<(string RelativeBookPath, string? RelativeCoverPath)> CopyIntoLibraryAsync(
            Guid bookId,
            string sourcePath,
            byte[]? coverBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteBookDirectoryAsync(Guid bookId, CancellationToken cancellationToken) =>
            throw new IOException("cleanup failed");
        public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken) =>
            throw new IOException("cleanup failed");

        public string GetAbsolutePath(string relativePath) => relativePath;
    }

    private sealed class NoopMetadataAdapterResolver : IMetadataAdapterResolver
    {
        public IMetadataAdapter Resolve(EbookFormat format) => new NoopMetadataAdapter();
    }

    private sealed class NoopMetadataAdapter : IMetadataAdapter
    {
        public bool CanHandle(EbookFormat format) => true;

        public Task<MetadataReadResult> ReadAsync(string path, EbookFormat format, CancellationToken cancellationToken) =>
            Task.FromResult(new MetadataReadResult(new BookMetadata("Title", ["Author"])));

        public Task<MetadataWriteResult> WriteAsync(
            string path,
            EbookFormat format,
            BookMetadata metadata,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MetadataWriteResult(MetadataWriteBackStatus.Unsupported));
    }

    private sealed class RecordingBookFileInteractionService : IBookFileInteractionService
    {
        public List<string> OpenedFiles { get; } = [];
        public List<string> OpenedRelativePaths { get; } = [];
        public string? DownloadsFolder { get; set; }
        public string? ExportFolder { get; set; }
        public bool ShouldOpenFile { get; set; } = true;
        public bool ShouldOpenContainingFolder { get; set; } = true;
        public int ConfirmRemoveFormatCalls { get; private set; }

        public Task<bool> OpenFileAsync(string relativePath, CancellationToken cancellationToken)
        {
            OpenedFiles.Add(relativePath);
            return Task.FromResult(ShouldOpenFile);
        }

        public Task<bool> OpenContainingFolderAsync(string relativePath, CancellationToken cancellationToken)
        {
            OpenedRelativePaths.Add(relativePath);
            return Task.FromResult(ShouldOpenContainingFolder);
        }

        public Task<bool> ConfirmRemoveFormatAsync(string formatText, CancellationToken cancellationToken)
        {
            ConfirmRemoveFormatCalls++;
            return Task.FromResult(true);
        }

        public Task<string?> PickExportFolderAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ExportFolder);

        public Task<string> GetDefaultExportFolderAsync(CancellationToken cancellationToken) =>
            Task.FromResult(DownloadsFolder ?? Path.GetTempPath());
    }
}
