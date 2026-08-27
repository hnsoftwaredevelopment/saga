using EbookManager.Application.Books;
using EbookManager.Application.Importing;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;
using EbookManager.Domain.Importing;
using EbookManager.Domain.Libraries;
using EbookManager.Domain.Metadata;
using EbookManager.Domain.Settings;
using EbookManager.Libraries;
using EbookManager.Presentation.Abstractions;
using EbookManager.Presentation.ViewModels;
using EbookManager.Tests.TestSupport;
using FluentAssertions;
using System.Globalization;

namespace EbookManager.Tests.App.ViewModels;

public sealed class LibraryViewModelTests
{
    [Fact]
    public async Task Refresh_loads_books_and_search_filters_visible_books()
    {
        var first = CreateBook("The Hobbit", ["Tolkien"]);
        var second = CreateBook("Dune", ["Frank Herbert"]);
        var viewModel = CreateViewModel([second, first]);

        await viewModel.RefreshAsync();
        viewModel.SearchText = "tolkien";

        viewModel.VisibleBooks.Should().ContainSingle();
        viewModel.VisibleBooks[0].Title.Should().Be("The Hobbit");
        viewModel.VisibleBookCount.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_sets_loading_state_while_library_is_loading()
    {
        var repository = new BlockingBookRepository();
        var viewModel = CreateViewModel(
            [],
            repository: repository,
            currentLibrary: CreateActiveLibrary());

        var refresh = viewModel.RefreshAsync();
        await repository.ListStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.IsLoadingLibrary.Should().BeTrue();
        viewModel.EmptyStateMessage.Should().Be("Loading library...");

        repository.Release([]);
        await refresh;

        viewModel.IsLoadingLibrary.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_reports_paged_loading_progress_while_library_is_loading()
    {
        var books = Enumerable.Range(1, 1_200)
            .Select(index => CreateBook($"Book {index:0000}", ["Author"]))
            .ToList();
        var repository = new BlockingPagedBookRepository(books);
        var viewModel = CreateViewModel(
            [],
            repository: repository,
            currentLibrary: CreateActiveLibrary());

        var refresh = viewModel.RefreshAsync();
        await repository.FirstPageLoaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => viewModel.LoadedLibraryCount == 500);

        viewModel.IsLoadingLibrary.Should().BeTrue();
        viewModel.LoadingLibraryTotalCount.Should().Be(1_200);
        viewModel.LoadedLibraryCount.Should().Be(500);
        viewModel.LoadingLibraryProgressValue.Should().BeApproximately(41.67, 0.01);
        viewModel.LoadingLibraryProgressText.Should().Be("500 / 1200");

        repository.ReleaseRemainingPages();
        await refresh;

        viewModel.IsLoadingLibrary.Should().BeFalse();
        viewModel.VisibleBookCount.Should().Be(1_200);
    }


    [Fact]
    public async Task First_refresh_applies_default_view_from_settings()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with { DefaultView = nameof(LibraryView.Bookshelf) },
            default);
        var viewModel = CreateViewModel([], settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.SelectedView.Should().Be(LibraryView.Bookshelf);
    }

    [Fact]
    public async Task First_refresh_exposes_builtin_and_custom_view_definitions()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                LibraryViewDefinitions = new LibraryViewDefinitionSettings(
                [
                    new("thrillers", "Thrillers", "Detailed", "custom:thrillers")
                ])
            },
            default);
        var viewModel = CreateViewModel([], settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.ViewDefinitions.Select(view => view.Id)
            .Should()
            .Equal("Bookshelf", "Detailed", "List", "thrillers");
        viewModel.ViewDefinitions.Single(view => view.Id == "thrillers").Name.Should().Be("Thrillers");
        viewModel.ViewDefinitions.Single(view => view.Id == "thrillers").BaseView.Should().Be(LibraryView.Detailed);
        viewModel.ViewDefinitions.Single(view => view.Id == "thrillers").LayoutKey.Should().Be("custom:thrillers");
    }

    [Fact]
    public async Task Default_view_can_select_custom_view_definition()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                DefaultView = "thrillers",
                LibraryViewDefinitions = new LibraryViewDefinitionSettings(
                [
                    new("thrillers", "Thrillers", "Detailed", "custom:thrillers")
                ])
            },
            default);
        var viewModel = CreateViewModel([], settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.SelectedViewDefinitionId.Should().Be("thrillers");
        viewModel.SelectedView.Should().Be(LibraryView.Detailed);
        viewModel.ActiveViewLayoutKey.Should().Be("custom:thrillers");
    }

    [Fact]
    public async Task Custom_view_layout_changes_are_saved_separately_from_builtin_layout()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                DefaultView = "thrillers",
                LibraryViewDefinitions = new LibraryViewDefinitionSettings(
                [
                    new("thrillers", "Thrillers", "Detailed", "custom:thrillers")
                ]),
                LibraryViewLayouts = new LibraryViewLayoutSettings(
                    new Dictionary<string, LibraryViewLayoutSetting>(StringComparer.Ordinal)
                    {
                        ["Detailed"] = new(
                            Columns: ["Title", "Authors"],
                            Sort: "Title"),
                        ["custom:thrillers"] = new(
                            Columns: ["Title", "Series"],
                            Sort: "Series")
                    })
            },
            default);
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], series: "Series")], settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.SelectedViewDefinitionId.Should().Be("thrillers");
        viewModel.ActiveColumnOptions.Should().Equal(LibraryColumnOption.Title, LibraryColumnOption.Series);

        var formatChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Format);
        formatChoice.IsSelected = true;
        await viewModel.ToggleColumnCommand.ExecuteAsync(formatChoice);
        var seriesChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Series);
        seriesChoice.IsSelected = false;
        await viewModel.ToggleColumnCommand.ExecuteAsync(seriesChoice);

        settingsStore.Settings.LibraryViewLayouts!.Views!["custom:thrillers"].Columns
            .Should()
            .Equal("Title", "Format");
        settingsStore.Settings.LibraryViewLayouts.Views["Detailed"].Columns
            .Should()
            .Equal("Title", "Authors");

        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Title, LibraryColumnOption.Authors, LibraryColumnOption.Format]);

        settingsStore.Settings.LibraryViewLayouts.Views["custom:thrillers"].Columns
            .Should()
            .Equal("Title", "Authors", "Format");
        settingsStore.Settings.LibraryViewLayouts.Views["Detailed"].Columns
            .Should()
            .Equal("Title", "Authors");
    }

    [Fact]
    public async Task Custom_metadata_fields_can_be_selected_as_view_columns()
    {
        var book = CreateBook("Book", ["Author"]);
        var settingsStore = new InMemoryAppSettingsStore();
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Gelezen door", CustomMetadataFieldType.Text);
        customMetadataRepository.SetValue(new CustomMetadataValue(book.Id, field.Id, TextValue: "Henk"));
        var viewModel = CreateViewModel(
            [book],
            currentLibrary: CreateActiveLibrary(),
            settingsStore: settingsStore,
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();

        var customChoice = viewModel.ColumnChoices.Single(choice => choice.Key == LibraryColumnKey.FromCustom(field.Id));
        customChoice.DisplayName.Should().Be("Gelezen door");
        customChoice.IsSelected.Should().BeFalse();

        customChoice.IsSelected = true;
        await viewModel.ToggleColumnCommand.ExecuteAsync(customChoice);

        viewModel.ActiveColumnOptions.Should().Contain(LibraryColumnKey.FromCustom(field.Id));
        viewModel.VisibleBooks.Single().GetCustomMetadataValue(field.Id).Should().Be("Henk");
        settingsStore.Settings.LibraryViewLayouts!.Views!["Detailed"].Columns
            .Should()
            .Contain($"custom:{field.Id:D}");
    }

    [Fact]
    public async Task RefreshCustomMetadataColumnsAsync_refreshes_selected_book_details_options()
    {
        var book = CreateBook("Book", ["Author"]);
        var repository = new StaticBookRepository([book]);
        var bookService = new BookService(
            repository,
            new NoopLibraryFileStore(),
            new NoopMetadataAdapterResolver());
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Leesprioriteit", CustomMetadataFieldType.SingleSelect);
        var details = new BookDetailsViewModel(bookService, customMetadataRepository: customMetadataRepository);
        var viewModel = CreateViewModel(
            [book],
            currentLibrary: CreateActiveLibrary(),
            repository: repository,
            details: details,
            customMetadataRepository: customMetadataRepository);
        await viewModel.RefreshAsync();
        viewModel.SelectedBook = viewModel.VisibleBooks.Single();
        await Task.Delay(50);
        details.CustomMetadataValues.Single(value => value.FieldId == field.Id)
            .SingleSelectOptions
            .Should()
            .Equal([null]);

        await customMetadataRepository.UpdateDefinitionOptionsAsync(field.Id, ["Hoog", "Normaal"], default);
        await viewModel.RefreshCustomMetadataColumnsAsync();

        details.CustomMetadataValues.Single(value => value.FieldId == field.Id)
            .SingleSelectOptions
            .Should()
            .Equal(null, "Hoog", "Normaal");
    }

    [Fact]
    public async Task Search_matches_custom_metadata_values()
    {
        var first = CreateBook("Plain title", ["Author"]);
        var second = CreateBook("Other book", ["Author"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Leesclub", CustomMetadataFieldType.Text);
        customMetadataRepository.SetValue(new CustomMetadataValue(first.Id, field.Id, TextValue: "Avondgroep"));
        var viewModel = CreateViewModel(
            [first, second],
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        viewModel.SearchText = "avondgroep";

        viewModel.VisibleBooks.Should().ContainSingle(row => row.Id == first.Id);
    }

    [Fact]
    public async Task Custom_metadata_values_are_available_as_filters()
    {
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Leesclub", CustomMetadataFieldType.Text);
        customMetadataRepository.SetValue(new CustomMetadataValue(first.Id, field.Id, TextValue: "Avondgroep"));
        customMetadataRepository.SetValue(new CustomMetadataValue(second.Id, field.Id, TextValue: "Middaggroep"));
        var viewModel = CreateViewModel(
            [first, second],
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();

        var group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        group.Name.Should().Be("Leesclub");
        group.Filters.Select(filter => filter.Name).Should().Equal("Avondgroep", "Middaggroep");

        group.Filters.Single(filter => filter.Name == "Avondgroep").IsSelected = true;

        viewModel.VisibleBooks.Should().ContainSingle(row => row.Id == first.Id);
    }

    [Fact]
    public async Task Custom_metadata_filter_search_hides_nonmatching_values_and_preserves_selection()
    {
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        var third = CreateBook("Third", ["Author"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Leesclub", CustomMetadataFieldType.Text);
        customMetadataRepository.SetValue(new CustomMetadataValue(first.Id, field.Id, TextValue: "Avondgroep"));
        customMetadataRepository.SetValue(new CustomMetadataValue(second.Id, field.Id, TextValue: "Middaggroep"));
        customMetadataRepository.SetValue(new CustomMetadataValue(third.Id, field.Id, TextValue: "Weekendgroep"));
        var viewModel = CreateViewModel(
            [first, second, third],
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        var group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        group.Filters.Single(filter => filter.Name == "Weekendgroep").IsSelected = true;

        group.FilterSearchText = "middag";

        group.VisibleFilterCount.Should().Be(1);
        group.FilterSearchSummary.Should().Be("1 / 3");
        group.Filters.Single(filter => filter.Name == "Middaggroep").IsVisible.Should().BeTrue();
        group.Filters.Single(filter => filter.Name == "Weekendgroep").IsVisible.Should().BeFalse();
        group.Filters.Single(filter => filter.Name == "Weekendgroep").IsSelected.Should().BeTrue();
        viewModel.VisibleBooks.Should().ContainSingle(row => row.Id == third.Id);
    }

    [Fact]
    public async Task Custom_metadata_filter_search_row_is_only_available_for_long_lists_or_active_search_text()
    {
        var books = Enumerable.Range(1, 3)
            .Select(index => CreateBook($"Book {index}", ["Author"]))
            .ToArray();
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Leesclub", CustomMetadataFieldType.Text);
        foreach (var book in books.Select((book, index) => new { Book = book, Index = index + 1 }))
        {
            customMetadataRepository.SetValue(
                new CustomMetadataValue(book.Book.Id, field.Id, TextValue: $"Groep {book.Index}"));
        }

        var viewModel = CreateViewModel(
            books,
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        var group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;

        group.HasFilterSearch.Should().BeFalse();

        group.FilterSearchText = "groep";

        group.HasFilterSearch.Should().BeTrue();
    }

    [Fact]
    public async Task Multi_select_custom_metadata_values_are_available_as_separate_filters()
    {
        var book = CreateBook("First", ["Author"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Genres", CustomMetadataFieldType.MultiSelect);
        customMetadataRepository.SetValue(new CustomMetadataValue(book.Id, field.Id, TextValue: "Thriller; Fantasy"));
        var viewModel = CreateViewModel(
            [book],
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();

        var group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        group.Filters.Select(filter => filter.Name).Should().Equal("Fantasy", "Thriller");

        group.Filters.Single(filter => filter.Name == "Fantasy").IsSelected = true;
        viewModel.VisibleBooks.Should().ContainSingle(row => row.Id == book.Id);

        group.Filters.Single(filter => filter.Name == "Fantasy").IsSelected = false;
        group.Filters.Single(filter => filter.Name == "Thriller").IsSelected = true;
        viewModel.VisibleBooks.Should().ContainSingle(row => row.Id == book.Id);
    }

    [Fact]
    public void Metadata_quality_dashboard_counts_first_quality_signals()
    {
        var missingAuthor = CreateBook("Onbekend", ["Unknown"], language: "nl", coverBytes: [1]);
        var unknownLanguage = CreateBook("Dwaalspoor", ["Author"], language: "fictional", coverBytes: [1]);
        var missingCover = CreateBook("Zonder omslag", ["Author"], language: "en");
        var seriesNumberOnly = CreateBook("Serienummer", ["Author"], language: "en", seriesNumber: 2, coverBytes: [1]);
        var swapped = CreateBook("Karin Slaughter", ["Triptiek"], language: "nl", coverBytes: [1]);
        var messyTags = CreateBook("Tags", ["Author"], language: "en", tags: ["thriller, crime"], coverBytes: [1]);

        var dashboard = new MetadataQualityDashboardViewModel(
            [missingAuthor, unknownLanguage, missingCover, seriesNumberOnly, swapped, messyTags],
            key => key);

        dashboard.Issues.Single(issue => issue.Title == "MetadataQualityMissingAuthor").Count.Should().Be(1);
        dashboard.Issues.Single(issue => issue.Title == "MetadataQualityUnknownLanguage").Count.Should().Be(1);
        dashboard.Issues.Single(issue => issue.Title == "MetadataQualityMissingCover").Count.Should().Be(1);
        dashboard.Issues.Single(issue => issue.Title == "MetadataQualitySeriesNumberWithoutSeries").Count.Should().Be(1);
        dashboard.Issues.Single(issue => issue.Title == "MetadataQualityPossibleTitleAuthorSwap").Count.Should().Be(1);
        dashboard.Issues.Single(issue => issue.Title == "MetadataQualityMessyTags").Count.Should().Be(1);
    }

    [Fact]
    public async Task Show_metadata_quality_dashboard_opens_dashboard_for_current_library()
    {
        var book = CreateBook("Karin Slaughter", ["Triptiek"], language: "nl");
        var interaction = new ScriptedUserInteractionService();
        var viewModel = CreateViewModel([book], interaction, currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        await viewModel.ShowMetadataQualityDashboardCommand.ExecuteAsync(null);

        interaction.MetadataQualityDashboard.Should().NotBeNull();
        interaction.MetadataQualityDashboard!.TotalBookCount.Should().Be(1);
        interaction.MetadataQualityDashboard.Issues
            .Single(issue => issue.Title == "MetadataQualityPossibleTitleAuthorSwap")
            .Rows
            .Should()
            .ContainSingle(row => row.Title == "Karin Slaughter");
    }

    [Fact]
    public async Task Metadata_quality_navigation_clears_search_that_hides_selected_book()
    {
        var target = CreateBook("Doelboek", ["Auteur A"]);
        var other = CreateBook("Ander boek", ["Auteur B"]);
        var interaction = new ScriptedUserInteractionService { MetadataQualityDashboardResult = target.Id };
        var viewModel = CreateViewModel([target, other], interaction, currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        viewModel.SearchText = "Ander boek";

        await viewModel.ShowMetadataQualityDashboardCommand.ExecuteAsync(null);

        viewModel.SearchText.Should().BeEmpty();
        viewModel.SelectedBook!.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Metadata_quality_navigation_clears_selected_filters_that_hide_book()
    {
        var target = CreateBook("Doelboek", ["Auteur A"]);
        var other = CreateBook("Ander boek", ["Auteur B"]);
        var interaction = new ScriptedUserInteractionService { MetadataQualityDashboardResult = target.Id };
        var viewModel = CreateViewModel([target, other], interaction, currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        var blockingFilter = viewModel.AuthorFilters.Single(filter => filter.Name == "Auteur B");
        blockingFilter.IsSelected = true;

        await viewModel.ShowMetadataQualityDashboardCommand.ExecuteAsync(null);

        blockingFilter.IsSelected.Should().BeFalse();
        viewModel.SelectedBook!.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Metadata_quality_navigation_keeps_filter_that_already_allows_book()
    {
        var target = CreateBook("Doelboek", ["Auteur A"]);
        var other = CreateBook("Ander boek", ["Auteur B"]);
        var interaction = new ScriptedUserInteractionService { MetadataQualityDashboardResult = target.Id };
        var viewModel = CreateViewModel([target, other], interaction, currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        var matchingFilter = viewModel.AuthorFilters.Single(filter => filter.Name == "Auteur A");
        matchingFilter.IsSelected = true;

        await viewModel.ShowMetadataQualityDashboardCommand.ExecuteAsync(null);

        matchingFilter.IsSelected.Should().BeTrue();
        viewModel.SelectedBook!.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Metadata_quality_navigation_clears_custom_filter_that_hides_book()
    {
        var target = CreateBook("Doelboek", ["Auteur A"]);
        var other = CreateBook("Ander boek", ["Auteur B"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Leesclub", CustomMetadataFieldType.Text);
        customMetadataRepository.SetValue(new CustomMetadataValue(target.Id, field.Id, TextValue: "Middag"));
        customMetadataRepository.SetValue(new CustomMetadataValue(other.Id, field.Id, TextValue: "Avond"));
        var interaction = new ScriptedUserInteractionService { MetadataQualityDashboardResult = target.Id };
        var viewModel = CreateViewModel(
            [target, other],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        var group = viewModel.CustomMetadataFilterGroups.Single(item => item.FieldId == field.Id);
        var blockingFilter = group.Filters.Single(filter => filter.Name == "Avond");
        blockingFilter.IsSelected = true;

        await viewModel.ShowMetadataQualityDashboardCommand.ExecuteAsync(null);

        blockingFilter.IsSelected.Should().BeFalse();
        viewModel.SelectedBook!.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Closing_metadata_quality_dashboard_keeps_library_context_unchanged()
    {
        var first = CreateBook("Eerste", ["Auteur A"]);
        var second = CreateBook("Tweede", ["Auteur B"]);
        var viewModel = CreateViewModel(
            [first, second],
            new ScriptedUserInteractionService(),
            currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        viewModel.SearchText = "Eerste";
        var selectedBefore = viewModel.SelectedBook;

        await viewModel.ShowMetadataQualityDashboardCommand.ExecuteAsync(null);

        viewModel.SearchText.Should().Be("Eerste");
        viewModel.SelectedBook.Should().BeSameAs(selectedBefore);
    }

    [Fact]
    public async Task Custom_metadata_filter_value_can_be_renamed_for_matching_books()
    {
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Leesclub", CustomMetadataFieldType.Text);
        customMetadataRepository.SetValue(new CustomMetadataValue(first.Id, field.Id, TextValue: "Avondgroep"));
        customMetadataRepository.SetValue(new CustomMetadataValue(second.Id, field.Id, TextValue: "Middaggroep"));
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "Ochtendgroep" };
        var viewModel = CreateViewModel(
            [first, second],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        var group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        await viewModel.RenameCustomMetadataFilterCommand.ExecuteAsync(
            group.Filters.Single(filter => filter.Name == "Avondgroep"));

        customMetadataRepository.ValuesSnapshot.Single(value => value.BookId == first.Id).TextValue.Should().Be("Ochtendgroep");
        customMetadataRepository.ValuesSnapshot.Single(value => value.BookId == second.Id).TextValue.Should().Be("Middaggroep");
        group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        group.Filters.Select(filter => filter.Name).Should().Equal("Middaggroep", "Ochtendgroep");
        viewModel.VisibleBooks.Should().HaveCount(2);
    }

    [Fact]
    public async Task Multi_select_custom_metadata_filter_value_can_be_renamed_without_losing_other_values()
    {
        var book = CreateBook("First", ["Author"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Genres", CustomMetadataFieldType.MultiSelect);
        await customMetadataRepository.UpdateDefinitionOptionsAsync(field.Id, ["Thriller", "Fantasy"], default);
        customMetadataRepository.SetValue(new CustomMetadataValue(book.Id, field.Id, TextValue: "Thriller; Fantasy"));
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "Detective" };
        var viewModel = CreateViewModel(
            [book],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        var group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        await viewModel.RenameCustomMetadataFilterCommand.ExecuteAsync(
            group.Filters.Single(filter => filter.Name == "Thriller"));

        customMetadataRepository.ValuesSnapshot.Single().TextValue.Should().Be("Detective; Fantasy");
        (await customMetadataRepository.ListDefinitionsAsync(default))
            .Single(definition => definition.Id == field.Id)
            .Options
            .Should()
            .Equal("Detective", "Fantasy");
        group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        group.Filters.Select(filter => filter.Name).Should().Equal("Detective", "Fantasy");
    }

    [Fact]
    public async Task Custom_metadata_filter_value_can_be_removed_from_matching_books()
    {
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var field = customMetadataRepository.AddDefinition("Genres", CustomMetadataFieldType.MultiSelect);
        customMetadataRepository.SetValue(new CustomMetadataValue(first.Id, field.Id, TextValue: "Thriller; Fantasy"));
        customMetadataRepository.SetValue(new CustomMetadataValue(second.Id, field.Id, TextValue: "Thriller"));
        var interaction = new ScriptedUserInteractionService { ConfirmMetadataValueRemovalResult = true };
        var viewModel = CreateViewModel(
            [first, second],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        var group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        await viewModel.RemoveCustomMetadataFilterCommand.ExecuteAsync(
            group.Filters.Single(filter => filter.Name == "Thriller"));

        customMetadataRepository.ValuesSnapshot.Single().TextValue.Should().Be("Fantasy");
        customMetadataRepository.ValuesSnapshot.Single().BookId.Should().Be(first.Id);
        group = viewModel.CustomMetadataFilterGroups.Should().ContainSingle(item => item.FieldId == field.Id).Subject;
        group.Filters.Should().ContainSingle(filter => filter.Name == "Fantasy" && filter.Count == 1);
    }

    [Fact]
    public async Task Copy_current_view_creates_selected_custom_view_with_copied_layout()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "Mijn thrillers" };
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                LibraryViewLayouts = new LibraryViewLayoutSettings(
                    new Dictionary<string, LibraryViewLayoutSetting>(StringComparer.Ordinal)
                    {
                        ["Detailed"] = new(
                            Groupings: ["Author"],
                            Columns: ["Title", "Authors", "Series"],
                            ColumnWidths: new Dictionary<string, double>
                            {
                                ["Title"] = 330
                            })
                    })
            },
            default);
        var viewModel = CreateViewModel(
            [CreateBook("Book", ["Author"], series: "Series")],
            interaction,
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SelectedSortOption = LibrarySortOption.Author;
        await viewModel.WaitForPendingSortSettingsSaveAsync();
        await viewModel.CopyCurrentViewCommand.ExecuteAsync(null);

        viewModel.SelectedViewDefinitionId.Should().Be("mijn-thrillers");
        viewModel.ActiveViewLayoutKey.Should().Be("custom:mijn-thrillers");
        viewModel.SelectedView.Should().Be(LibraryView.Detailed);
        viewModel.ViewDefinitions.Should().Contain(view =>
            view.Id == "mijn-thrillers" &&
            view.Name == "Mijn thrillers" &&
            view.BaseView == LibraryView.Detailed &&
            view.LayoutKey == "custom:mijn-thrillers");
        settingsStore.Settings.LibraryViewDefinitions!.CustomViews.Should().ContainSingle(view =>
            view.Id == "mijn-thrillers" &&
            view.Name == "Mijn thrillers" &&
            view.BaseView == "Detailed" &&
            view.LayoutKey == "custom:mijn-thrillers");
        settingsStore.Settings.LibraryViewLayouts!.Views!["custom:mijn-thrillers"].Columns
            .Should()
            .Equal("Title", "Authors", "Series");
        settingsStore.Settings.LibraryViewLayouts.Views["custom:mijn-thrillers"].Sort.Should().Be("Author");
        settingsStore.Settings.LibraryViewLayouts.Views["custom:mijn-thrillers"].Groupings.Should().Equal("Author");
        settingsStore.Settings.LibraryViewLayouts.Views["custom:mijn-thrillers"].ColumnWidths.Should().Contain("Title", 330);
    }

    [Fact]
    public async Task Rename_current_custom_view_updates_name_without_changing_layout_key()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "Favoriete thrillers" };
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                DefaultView = "thrillers",
                LibraryViewDefinitions = new LibraryViewDefinitionSettings(
                [
                    new("thrillers", "Thrillers", "Detailed", "custom:thrillers")
                ]),
                LibraryViewLayouts = new LibraryViewLayoutSettings(
                    new Dictionary<string, LibraryViewLayoutSetting>(StringComparer.Ordinal)
                    {
                        ["custom:thrillers"] = new(
                            Groupings: ["Author"],
                            Columns: ["Title", "Authors"],
                            Sort: "Author")
                    })
            },
            default);
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], interaction, settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.RenameCurrentViewCommand.ExecuteAsync(null);

        viewModel.SelectedViewDefinitionId.Should().Be("thrillers");
        viewModel.ViewDefinitions.Single(view => view.Id == "thrillers").Name.Should().Be("Favoriete thrillers");
        settingsStore.Settings.LibraryViewDefinitions!.CustomViews.Should().ContainSingle(view =>
            view.Id == "thrillers" &&
            view.Name == "Favoriete thrillers" &&
            view.LayoutKey == "custom:thrillers");
        settingsStore.Settings.LibraryViewLayouts!.Views.Should().ContainKey("custom:thrillers");
    }

    [Fact]
    public async Task Selected_view_definition_exposes_custom_view_name_for_view_settings()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                DefaultView = "thrillers",
                LibraryViewDefinitions = new LibraryViewDefinitionSettings(
                [
                    new("thrillers", "Favoriete thrillers", "Detailed", "custom:thrillers")
                ])
            },
            default);
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.SelectedViewDefinition.Should().NotBeNull();
        viewModel.SelectedViewDefinition!.Name.Should().Be("Favoriete thrillers");
        viewModel.SelectedViewDefinition.IsBuiltIn.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_current_custom_view_removes_definition_and_selects_base_view()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                DefaultView = "thrillers",
                LibraryViewDefinitions = new LibraryViewDefinitionSettings(
                [
                    new("thrillers", "Thrillers", "Detailed", "custom:thrillers")
                ]),
                LibraryViewLayouts = new LibraryViewLayoutSettings(
                    new Dictionary<string, LibraryViewLayoutSetting>(StringComparer.Ordinal)
                    {
                        ["Detailed"] = new(Columns: ["Title", "Authors"]),
                        ["custom:thrillers"] = new(Columns: ["Title"])
                    })
            },
            default);
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.DeleteCurrentViewCommand.ExecuteAsync(null);

        viewModel.SelectedViewDefinitionId.Should().Be("Detailed");
        viewModel.ActiveViewLayoutKey.Should().Be("Detailed");
        viewModel.SelectedView.Should().Be(LibraryView.Detailed);
        viewModel.ViewDefinitions.Should().NotContain(view => view.Id == "thrillers");
        settingsStore.Settings.LibraryViewDefinitions!.CustomViews.Should().BeEmpty();
        settingsStore.Settings.LibraryViewLayouts!.Views.Should().NotContainKey("custom:thrillers");
    }

    [Fact]
    public async Task Delete_current_custom_view_keeps_definition_when_confirmation_is_declined()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var interaction = new ScriptedUserInteractionService { ConfirmDeleteViewResult = false };
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                DefaultView = "thrillers",
                LibraryViewDefinitions = new LibraryViewDefinitionSettings(
                [
                    new("thrillers", "Thrillers", "Detailed", "custom:thrillers")
                ]),
                LibraryViewLayouts = new LibraryViewLayoutSettings(
                    new Dictionary<string, LibraryViewLayoutSetting>(StringComparer.Ordinal)
                    {
                        ["custom:thrillers"] = new(Columns: ["Title"])
                    })
            },
            default);
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], interaction, settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.DeleteCurrentViewCommand.ExecuteAsync(null);

        viewModel.SelectedViewDefinitionId.Should().Be("thrillers");
        viewModel.ViewDefinitions.Should().Contain(view => view.Id == "thrillers");
        settingsStore.Settings.LibraryViewDefinitions!.CustomViews.Should().ContainSingle(view => view.Id == "thrillers");
        settingsStore.Settings.LibraryViewLayouts!.Views.Should().ContainKey("custom:thrillers");
    }

    [Fact]
    public async Task SearchText_is_exposed_on_visible_rows_for_highlighting()
    {
        var book = CreateBook("The Hobbit", ["Tolkien"]);
        var viewModel = CreateViewModel([book]);

        await viewModel.RefreshAsync();
        viewModel.SearchText = "hob";

        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.SearchText.Should().Be("hob");
    }

    [Fact]
    public async Task ShowDuplicateCandidates_opens_duplicate_candidate_overview()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"]);
        var second = CreateBook(" de hobbit ", ["J.R.R. Tolkien", "Alan Lee"]);
        var unrelated = CreateBook("Dune", ["Frank Herbert"]);
        var interaction = new ScriptedUserInteractionService();
        var viewModel = CreateViewModel(
            [first, second, unrelated],
            userInteraction: interaction,
            currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        await viewModel.ShowDuplicateCandidatesCommand.ExecuteAsync(null);

        interaction.DuplicateCandidates.Should().NotBeNull();
        interaction.DuplicateCandidates!.Groups.Should().ContainSingle()
            .Which.Books.Select(book => book.Id).Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task ShowDuplicateCandidates_applies_default_exact_match_preference()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"]);
        var second = CreateBook("De Hobbit", ["Unknown"]);
        var interaction = new ScriptedUserInteractionService();
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(settingsStore.Settings with
        {
            DuplicateExactMatchesOnly = false
        }, default);
        var viewModel = CreateViewModel(
            [first, second],
            userInteraction: interaction,
            currentLibrary: CreateActiveLibrary(),
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.ShowDuplicateCandidatesCommand.ExecuteAsync(null);

        interaction.DuplicateCandidates.Should().NotBeNull();
        interaction.DuplicateCandidates!.ExactMatchesOnly.Should().BeFalse();
        interaction.DuplicateCandidates.Rows.Should().Contain(row => row.Id == first.Id);
        interaction.DuplicateCandidates.Rows.Should().Contain(row => row.Id == second.Id);
    }

    [Fact]
    public async Task ShowDuplicateCandidates_applies_duplicate_merge_default_preferences()
    {
        var first = CreateBook("De Hobbit", ["J.R.R. Tolkien"], language: "nl");
        var second = CreateBook("De Hobbit", ["Unknown"], language: "en");
        var interaction = new ScriptedUserInteractionService();
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(settingsStore.Settings with
        {
            DuplicateExactMatchesOnly = false,
            DuplicateMergeDefaults = new DuplicateMergeDefaultSettings(
                Authors: DuplicateMergeDefaultAction.Merge,
                Language: DuplicateMergeDefaultAction.Copy)
        }, default);
        var viewModel = CreateViewModel(
            [first, second],
            userInteraction: interaction,
            currentLibrary: CreateActiveLibrary(),
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.ShowDuplicateCandidatesCommand.ExecuteAsync(null);

        var preview = interaction.DuplicateCandidates!.CreateMergePreview(interaction.DuplicateCandidates.Rows[0]);
        preview.Should().NotBeNull();
        preview!.Rows.Single(row => row.Label == "Authors").Action.Should().Be(DuplicateMergeFieldAction.Merge);
        preview.Rows.Single(row => row.Label == "Language").Action.Should().Be(DuplicateMergeFieldAction.Copy);
    }

    [Fact]
    public async Task Duplicate_candidate_merge_refreshes_duplicate_window_without_refreshing_main_library()
    {
        var sourceBookId = Guid.NewGuid();
        var targetBookId = Guid.NewGuid();
        var source = CreateBook("De Hobbit", ["J.R.R. Tolkien"], id: sourceBookId, formats: [EbookFormat.Pdf]);
        var targetBefore = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            language: "nl",
            series: "Midden-aarde",
            id: targetBookId,
            formats: [EbookFormat.Epub]);
        var targetAfter = targetBefore with { Formats = [EbookFormat.Epub, EbookFormat.Pdf] };
        var repository = new RefreshingBookRepository([source, targetBefore], [targetAfter]);
        var interaction = new ScriptedUserInteractionService();
        var viewModel = CreateViewModel(
            [source, targetBefore],
            interaction,
            repository: repository,
            currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        await viewModel.ShowDuplicateCandidatesCommand.ExecuteAsync(null);
        var sourceRow = interaction.DuplicateCandidates!.Rows.Single(row => row.Id == sourceBookId);

        await interaction.DuplicateCandidates.MergeCandidateAsync(sourceRow, CancellationToken.None);

        repository.AttachedSourceBookId.Should().Be(sourceBookId);
        repository.AttachedTargetBookId.Should().Be(targetBookId);
        repository.ListCalls.Should().Be(1);
        interaction.DuplicateCandidates.HasGroups.Should().BeFalse();
        VisibleBookTitles(viewModel).Should().Equal("De Hobbit", "De Hobbit");
        viewModel.VisibleBooks.Single(row => row.Id == targetBookId).Book.Formats
            .Should().BeEquivalentTo([EbookFormat.Epub]);
    }

    [Fact]
    public async Task Duplicate_candidate_window_refreshes_main_library_once_after_closing_with_changes()
    {
        var sourceBookId = Guid.NewGuid();
        var targetBookId = Guid.NewGuid();
        var source = CreateBook("De Hobbit", ["J.R.R. Tolkien"], id: sourceBookId, formats: [EbookFormat.Pdf]);
        var targetBefore = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            language: "nl",
            series: "Midden-aarde",
            id: targetBookId,
            formats: [EbookFormat.Epub]);
        var targetAfter = targetBefore with { Formats = [EbookFormat.Epub, EbookFormat.Pdf] };
        var repository = new RefreshingBookRepository([source, targetBefore], [targetAfter], [targetAfter]);
        var interaction = new ScriptedUserInteractionService
        {
            OnShowDuplicateCandidatesAsync = async (candidates, cancellationToken) =>
            {
                var sourceRow = candidates.Rows.Single(row => row.Id == sourceBookId);
                await candidates.MergeCandidateAsync(sourceRow, cancellationToken);
            }
        };
        var viewModel = CreateViewModel(
            [source, targetBefore],
            interaction,
            repository: repository,
            currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        await viewModel.ShowDuplicateCandidatesCommand.ExecuteAsync(null);

        repository.AttachedSourceBookId.Should().Be(sourceBookId);
        repository.AttachedTargetBookId.Should().Be(targetBookId);
        repository.ListCalls.Should().Be(2);
        VisibleBookTitles(viewModel).Should().Equal("De Hobbit");
        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Book.Formats.Should().BeEquivalentTo([EbookFormat.Epub, EbookFormat.Pdf]);
    }

    [Fact]
    public async Task Duplicate_candidate_merge_refreshes_when_candidate_no_longer_exists()
    {
        var source = CreateBook("De Hobbit", ["J.R.R. Tolkien"], formats: [EbookFormat.Pdf]);
        var target = CreateBook(
            "De Hobbit",
            ["J.R.R. Tolkien"],
            language: "nl",
            series: "Midden-aarde",
            formats: [EbookFormat.Epub]);
        var repository = new MissingBookOnAttachRepository([source, target], [target]);
        var interaction = new ScriptedUserInteractionService();
        var viewModel = CreateViewModel(
            [source, target],
            interaction,
            repository: repository,
            currentLibrary: CreateActiveLibrary());

        await viewModel.RefreshAsync();
        await viewModel.ShowDuplicateCandidatesCommand.ExecuteAsync(null);
        var sourceRow = interaction.DuplicateCandidates!.Rows.Single(row => row.Id == source.Id);

        var merge = () => interaction.DuplicateCandidates.MergeCandidateAsync(sourceRow, CancellationToken.None);

        await merge.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The duplicate list is outdated. Open the duplicate overview again.");
        repository.ListCalls.Should().Be(2);
        VisibleBookTitles(viewModel).Should().Equal("De Hobbit");
        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Author_filters_are_built_from_books_and_filter_visible_rows()
    {
        var hobbit = CreateBook("The Hobbit", ["Tolkien"]);
        var dune = CreateBook("Dune", ["Frank Herbert"]);
        var viewModel = CreateViewModel([hobbit, dune]);

        await viewModel.RefreshAsync();

        viewModel.AuthorFilters.Select(filter => filter.DisplayName)
            .Should().Equal("Frank Herbert (1)", "Tolkien (1)");

        viewModel.AuthorFilters.Should().OnlyContain(filter => !filter.IsSelected);
        viewModel.AuthorFilters.Single(filter => filter.Name == "Frank Herbert").IsSelected = true;

        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Title.Should().Be("Dune");
    }

    [Fact]
    public async Task Filter_search_hides_nonmatching_author_filters_without_changing_selection()
    {
        var dune = CreateBook("Dune", ["Frank Herbert"]);
        var hobbit = CreateBook("The Hobbit", ["J.R.R. Tolkien"]);
        var reacher = CreateBook("Reacher", ["Lee Child"]);
        var viewModel = CreateViewModel([dune, hobbit, reacher]);

        await viewModel.RefreshAsync();
        viewModel.AuthorFilters.Single(filter => filter.Name == "Lee Child").IsSelected = true;

        viewModel.AuthorFilterSearchText = "tol";

        viewModel.VisibleAuthorFilterCount.Should().Be(1);
        viewModel.AuthorFilterSearchSummary.Should().Be("1 / 3");
        viewModel.AuthorFilters.Single(filter => filter.Name == "J.R.R. Tolkien").IsVisible.Should().BeTrue();
        viewModel.AuthorFilters.Single(filter => filter.Name == "Lee Child").IsVisible.Should().BeFalse();
        viewModel.AuthorFilters.Single(filter => filter.Name == "Lee Child").IsSelected.Should().BeTrue();
        viewModel.VisibleBooks.Should().ContainSingle(row => row.Title == "Reacher");

        viewModel.AuthorFilterSearchText = string.Empty;

        viewModel.VisibleAuthorFilterCount.Should().Be(3);
        viewModel.AuthorFilterSearchSummary.Should().Be("3 / 3");
    }

    [Fact]
    public async Task Filter_search_row_is_only_available_for_long_lists_or_active_search_text()
    {
        var smallViewModel = CreateViewModel(
            [
                CreateBook("Book A", ["Author A"]),
                CreateBook("Book B", ["Author B"]),
                CreateBook("Book C", ["Author C"])
            ]);

        await smallViewModel.RefreshAsync();

        smallViewModel.HasAuthorFilterSearch.Should().BeFalse();

        smallViewModel.AuthorFilterSearchText = "author";

        smallViewModel.HasAuthorFilterSearch.Should().BeTrue();

        var largeViewModel = CreateViewModel(
            Enumerable.Range(1, 8)
                .Select(index => CreateBook($"Book {index}", [$"Author {index}"]))
                .ToArray());

        await largeViewModel.RefreshAsync();

        largeViewModel.HasAuthorFilterSearch.Should().BeTrue();
    }

    [Fact]
    public async Task Author_filter_order_uses_author_sort_strategy()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(settingsStore.Settings with
        {
            AuthorSortStrategy = AuthorSortStrategy.LastNameFirst
        }, default);
        var viewModel = CreateViewModel(
            [
                CreateBook("Book A", ["Karin Slaughter"]),
                CreateBook("Book B", ["Lee Child"]),
                CreateBook("Book C", ["J.R.R. Tolkien"])
            ],
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.AuthorFilters.Select(filter => filter.Name)
            .Should()
            .Equal("Lee Child", "Karin Slaughter", "J.R.R. Tolkien");
    }

    [Fact]
    public async Task Category_filters_are_built_from_tags_and_filter_visible_rows()
    {
        var fantasy = CreateBook("The Hobbit", ["Tolkien"], tags: ["Fantasy"]);
        var scienceFiction = CreateBook("Dune", ["Frank Herbert"], tags: ["Science fiction"]);
        var viewModel = CreateViewModel([fantasy, scienceFiction]);

        await viewModel.RefreshAsync();

        viewModel.CategoryFilters.Select(filter => filter.DisplayName)
            .Should().Equal("Fantasy (1)", "Science fiction (1)");

        viewModel.CategoryFilters.Should().OnlyContain(filter => !filter.IsSelected);
        viewModel.CategoryFilters.Single(filter => filter.Name == "Science fiction").IsSelected = true;

        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Title.Should().Be("Dune");
    }

    [Fact]
    public async Task Metadata_filters_are_built_from_series_status_ereader_and_language()
    {
        var fantasy = CreateBook(
            "The Hobbit",
            ["Tolkien"],
            language: "eng",
            series: "Middle-earth",
            readingStatus: ReadingStatus.Read);
        var scienceFiction = CreateBook(
            "Dune",
            ["Frank Herbert"],
            language: "nl-NL",
            series: "Dune",
            readingStatus: ReadingStatus.Unread);
        var viewModel = CreateViewModel([fantasy, scienceFiction]);

        await viewModel.RefreshAsync();

        viewModel.SeriesFilters.Select(filter => filter.DisplayName)
            .Should().Equal("Dune (1)", "Middle-earth (1)");
        viewModel.StatusFilters.Select(filter => filter.DisplayName)
            .Should().Equal("Unread (1)", "Read (1)");
        viewModel.EReaderFilters.Select(filter => filter.DisplayName)
            .Should().Equal("Unavailable (2)");
        viewModel.LanguageFilters.Select(filter => filter.DisplayName)
            .Should().Equal("Engels (1)", "Nederlands (1)");
        viewModel.FormatFilters.Select(filter => filter.DisplayName)
            .Should().BeEmpty();

        viewModel.SeriesFilters.Should().OnlyContain(filter => !filter.IsSelected);
        viewModel.SeriesFilters.Single(filter => filter.Name == "Dune").IsSelected = true;
        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Title.Should().Be("Dune");
    }

    [Fact]
    public async Task Refresh_localized_filter_display_names_updates_language_filters_for_current_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl-NL");
            var english = CreateBook("English Book", ["Author"], language: "en", tags: ["Nederlands"]);
            var dutch = CreateBook("Dutch Book", ["Author"], language: "nl", tags: ["User Tag"]);
            var viewModel = CreateViewModel([english, dutch]);

            await viewModel.RefreshAsync();
            viewModel.LanguageFilters.Select(filter => filter.DisplayName)
                .Should().Equal("Engels (1)", "Nederlands (1)");
            viewModel.CategoryFilters.Select(filter => filter.DisplayName)
                .Should().Contain("Nederlands (1)");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            viewModel.RefreshLocalizedFilterDisplayNames();

            viewModel.LanguageFilters.Select(filter => filter.DisplayName)
                .Should().Equal("English (1)", "Dutch (1)");
            viewModel.CategoryFilters.Select(filter => filter.DisplayName)
                .Should().Contain("Nederlands (1)");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task Latvian_language_filter_can_be_selected_without_crashing()
    {
        var latvian = CreateBook("Latvian Book", ["Author"], language: "lv");
        var dutch = CreateBook("Dutch Book", ["Author"], language: "nl");
        var viewModel = CreateViewModel([latvian, dutch]);

        await viewModel.RefreshAsync();

        var filter = viewModel.LanguageFilters.Single(filter => filter.Name == "lv");
        filter.IsSelected = true;

        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Title.Should().Be("Latvian Book");
    }

    [Fact]
    public async Task Format_filters_show_book_types_and_expand_results()
    {
        var epub = CreateBook("Epub Book", ["Author"], formats: [EbookFormat.Epub]);
        var pdf = CreateBook("Pdf Book", ["Author"], formats: [EbookFormat.Pdf]);
        var comic = CreateBook("Comic Book", ["Author"], formats: [EbookFormat.Cbr]);
        var viewModel = CreateViewModel([epub, pdf, comic]);

        await viewModel.RefreshAsync();

        viewModel.FormatFilters.Select(filter => filter.DisplayName)
            .Should().Equal("CBR (1)", "EPUB (1)", "PDF (1)");

        viewModel.FormatFilters.Single(filter => filter.Name == "Epub").IsSelected = true;
        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Title.Should().Be("Epub Book");

        viewModel.FormatFilters.Single(filter => filter.Name == "Pdf").IsSelected = true;
        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().BeEquivalentTo("Epub Book", "Pdf Book");
    }

    [Fact]
    public async Task Library_grouping_projects_multi_author_books_into_separate_author_nodes()
    {
        var sharedBook = CreateBook("Shared Book", ["Jan Wiersma", "Sonja de Leeuw"]);
        var soloBook = CreateBook("Solo Book", ["Jan Wiersma"]);
        var viewModel = CreateViewModel([sharedBook, soloBook]);

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions([LibraryGroupOption.Author]);

        viewModel.GroupedLibraryNodes.Select(group => (group.Header, group.BookCount))
            .Should()
            .BeEquivalentTo(
                [
                    ("Jan Wiersma", 2),
                    ("Sonja de Leeuw", 1)
                ]);
        viewModel.GroupedLibraryNodes.Single(group => group.Header == "Jan Wiersma").Books
            .Select(row => row.Title)
            .Should()
            .BeEquivalentTo("Shared Book", "Solo Book");
        viewModel.VisibleBookCount.Should().Be(2);
    }

    [Fact]
    public async Task Library_grouping_by_author_then_series_keeps_books_without_series_directly_under_author()
    {
        var looseBook = CreateBook("Loose Book", ["A.E. van Vogt"]);
        var firstSeriesBook = CreateBook("Apollo 1", ["A.E. van Vogt"], series: "Apollo");
        var secondSeriesBook = CreateBook("Apollo 2", ["A.E. van Vogt"], series: "Apollo");
        var viewModel = CreateViewModel([looseBook, firstSeriesBook, secondSeriesBook]);

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions([LibraryGroupOption.Author, LibraryGroupOption.Series]);

        var authorGroup = viewModel.GroupedLibraryNodes.Should().ContainSingle()
            .Which;
        authorGroup.Header.Should().Be("A.E. van Vogt");
        authorGroup.BookCount.Should().Be(3);
        authorGroup.Books.Should().ContainSingle()
            .Which.Title.Should().Be("Loose Book");
        authorGroup.Groups.Should().ContainSingle()
            .Which.Header.Should().Be("Apollo");
        authorGroup.Groups.Single().BookCount.Should().Be(2);
        authorGroup.Groups.Single().Books.Select(row => row.Title)
            .Should()
            .BeEquivalentTo("Apollo 1", "Apollo 2");
    }

    [Fact]
    public async Task Library_grouping_by_series_only_keeps_a_no_series_group()
    {
        var looseBook = CreateBook("Loose Book", ["Author"]);
        var seriesBook = CreateBook("Series Book", ["Author"], series: "Apollo");
        var viewModel = CreateViewModel([looseBook, seriesBook]);

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions([LibraryGroupOption.Series]);

        viewModel.GroupedLibraryNodes.Select(group => (group.Header, group.BookCount))
            .Should()
            .BeEquivalentTo(
                [
                    ("Apollo", 1),
                    ("No series", 1)
                ]);
    }

    [Fact]
    public async Task Library_grouping_supports_more_than_two_levels()
    {
        var epub = CreateBook("Apollo 1", ["A.E. van Vogt"], series: "Apollo", formats: [EbookFormat.Epub]);
        var pdf = CreateBook("Apollo 2", ["A.E. van Vogt"], series: "Apollo", formats: [EbookFormat.Pdf]);
        var viewModel = CreateViewModel([epub, pdf]);

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions(
            [
                LibraryGroupOption.Author,
                LibraryGroupOption.Series,
                LibraryGroupOption.Format
            ]);

        var authorGroup = viewModel.GroupedLibraryNodes.Should().ContainSingle().Which;
        authorGroup.Header.Should().Be("A.E. van Vogt");
        authorGroup.BookCount.Should().Be(2);
        var seriesGroup = authorGroup.Groups.Should().ContainSingle().Which;
        seriesGroup.Header.Should().Be("Apollo");
        seriesGroup.BookCount.Should().Be(2);
        seriesGroup.Groups.Select(group => (group.Header, group.BookCount))
            .Should()
            .BeEquivalentTo(
                [
                    ("EPUB", 1),
                    ("PDF", 1)
                ]);
    }

    [Fact]
    public void Library_group_node_uses_precomputed_book_count_when_available()
    {
        var child = new LibraryGroupNodeViewModel(
            "Child",
            [],
            [CreateRow("Child Book")],
            LibraryGroupOption.Series);

        var parent = new LibraryGroupNodeViewModel(
            "Parent",
            [child],
            [],
            LibraryGroupOption.Author,
            bookCount: 42);

        parent.BookCount.Should().Be(42);
    }

    [Fact]
    public async Task Grouping_commands_save_grouping_per_view()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel(
            [CreateBook("Book", ["Author"], series: "Series")],
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SelectedView.Should().Be(LibraryView.Detailed);
        viewModel.SelectedGroupOptionToAdd = LibraryGroupOption.Author;
        await viewModel.AddGroupingCommand.ExecuteAsync(null);
        viewModel.SelectedView = LibraryView.List;
        viewModel.SelectedGroupOptionToAdd = LibraryGroupOption.Series;
        await viewModel.AddGroupingCommand.ExecuteAsync(null);
        viewModel.SelectedView = LibraryView.Detailed;
        await viewModel.WaitForPendingGroupingSettingsSaveAsync();

        viewModel.ActiveGroupOptions.Should().Equal(LibraryGroupOption.Author);
        settingsStore.Settings.LibraryGroupings.Should().NotBeNull();
        settingsStore.Settings.LibraryGroupings!.Detailed.Should().Equal(nameof(LibraryGroupOption.Author));
        settingsStore.Settings.LibraryGroupings.List.Should().Equal(nameof(LibraryGroupOption.Series));
    }

    [Fact]
    public async Task Grouping_changes_do_not_rebuild_visible_books_or_reload_selected_book_details()
    {
        var first = CreateBook("First", ["Author"], tags: ["Tag"]);
        var second = CreateBook("Second", ["Author"], tags: ["Tag"]);
        var repository = new StaticBookRepository([first, second]);
        var viewModel = CreateViewModel([first, second], repository: repository);

        await viewModel.RefreshAsync();
        var visibleRows = viewModel.VisibleBooks.ToArray();
        viewModel.SelectedBook = viewModel.VisibleBooks.First();
        var getCallsAfterRefresh = repository.GetCalls;

        viewModel.SelectedGroupOptionToAdd = LibraryGroupOption.Author;
        await viewModel.AddGroupingCommand.ExecuteAsync(null);
        viewModel.SelectedGroupOptionToAdd = LibraryGroupOption.Tag;
        await viewModel.AddGroupingCommand.ExecuteAsync(null);

        viewModel.VisibleBooks.Should().Equal(visibleRows);
        repository.GetCalls.Should().Be(getCallsAfterRefresh);
        viewModel.GroupedLibraryNodes.Should().ContainSingle(group => group.Header == "Author");
    }

    [Fact]
    public async Task Only_active_library_view_exposes_item_sources()
    {
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], series: "Series")]);

        await viewModel.RefreshAsync();

        viewModel.SelectedView.Should().Be(LibraryView.Detailed);
        viewModel.DetailedVisibleBooksSource.Should().BeSameAs(viewModel.VisibleBooks);
        viewModel.ListVisibleBooksSource.Should().BeNull();
        viewModel.BookshelfVisibleBooksSource.Should().BeNull();

        viewModel.SelectedView = LibraryView.Bookshelf;

        viewModel.BookshelfVisibleBooksSource.Should().BeSameAs(viewModel.VisibleBooks);
        viewModel.DetailedVisibleBooksSource.Should().BeNull();
        viewModel.ListVisibleBooksSource.Should().BeNull();

        viewModel.SelectedGroupOptionToAdd = LibraryGroupOption.Author;
        await viewModel.AddGroupingCommand.ExecuteAsync(null);

        viewModel.BookshelfVisibleBooksSource.Should().BeNull();
        viewModel.BookshelfGroupedLibraryNodesSource.Should().BeSameAs(viewModel.GroupedLibraryNodes);
        viewModel.DetailedGroupedLibraryNodesSource.Should().BeNull();
        viewModel.ListGroupedLibraryNodesSource.Should().BeNull();
    }

    [Fact]
    public async Task Sorting_replaces_visible_books_in_bulk()
    {
        var viewModel = CreateViewModel(
            [
                CreateBook("C", ["Author"]),
                CreateBook("A", ["Author"]),
                CreateBook("B", ["Author"])
            ]);
        var resetCount = 0;
        viewModel.VisibleBooks.CollectionChanged += (_, args) =>
        {
            if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                resetCount++;
            }
        };

        await viewModel.RefreshAsync();
        resetCount = 0;

        viewModel.SelectedSortOption = LibrarySortOption.Title;

        viewModel.VisibleBooks.Select(row => row.Title).Should().Equal("A", "B", "C");
        resetCount.Should().Be(1);
    }

    [Fact]
    public async Task Sort_options_are_saved_per_view()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel(
            [
                CreateBook("C", ["Charlie"]),
                CreateBook("A", ["Alice"]),
                CreateBook("B", ["Bob"])
            ],
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SelectedSortOption = LibrarySortOption.Title;
        await viewModel.WaitForPendingSortSettingsSaveAsync();
        viewModel.SelectedView = LibraryView.Bookshelf;
        viewModel.SelectedSortOption = LibrarySortOption.Author;
        await viewModel.WaitForPendingSortSettingsSaveAsync();

        viewModel.SelectedView = LibraryView.Detailed;

        viewModel.SelectedSortOption.Should().Be(LibrarySortOption.Title);
        viewModel.VisibleBooks.Select(row => row.Title).Should().Equal("A", "B", "C");
        settingsStore.Settings.LibrarySorts.Should().NotBeNull();
        settingsStore.Settings.LibrarySorts!.Detailed.Should().Be(nameof(LibrarySortOption.Title));
        settingsStore.Settings.LibrarySorts.Bookshelf.Should().Be(nameof(LibrarySortOption.Author));
    }

    [Fact]
    public async Task Removing_grouping_preserves_remaining_grouping_chips()
    {
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], tags: ["Tag"])]);

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions([LibraryGroupOption.Author, LibraryGroupOption.Tag]);
        var optionsReference = viewModel.ActiveGroupOptions;

        await viewModel.RemoveGroupingCommand.ExecuteAsync(LibraryGroupOption.Tag);

        viewModel.ActiveGroupOptions.Should().BeSameAs(optionsReference);
        viewModel.ActiveGroupOptions.Should().Equal(LibraryGroupOption.Author);
    }

    [Fact]
    public async Task SetGroupingOptions_persists_grouping_settings()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions([LibraryGroupOption.Author, LibraryGroupOption.Tag]);
        await viewModel.WaitForPendingGroupingSettingsSaveAsync();

        settingsStore.Settings.LibraryGroupings.Should().NotBeNull();
        settingsStore.Settings.LibraryGroupings!.Detailed.Should().Equal(
            nameof(LibraryGroupOption.Author),
            nameof(LibraryGroupOption.Tag));
    }

    [Fact]
    public async Task Visible_columns_are_saved_per_grid_view()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Title, LibraryColumnOption.Authors, LibraryColumnOption.Series]);
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.List,
            [LibraryColumnOption.Title, LibraryColumnOption.Format]);

        viewModel.GetVisibleColumns(LibraryView.Detailed).Should().Equal(
            LibraryColumnOption.Title,
            LibraryColumnOption.Authors,
            LibraryColumnOption.Series);
        viewModel.SelectedView = LibraryView.List;
        viewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Title,
            LibraryColumnOption.Format);
        settingsStore.Settings.LibraryColumns.Should().NotBeNull();
        settingsStore.Settings.LibraryColumns!.Detailed.Should().Equal("Title", "Authors", "Series");
        settingsStore.Settings.LibraryColumns.List.Should().Equal("Title", "Format");

        var reloadedViewModel = CreateViewModel(
            [CreateBook("Book", ["Author"])],
            settingsStore: settingsStore);
        await reloadedViewModel.RefreshAsync();

        reloadedViewModel.GetVisibleColumns(LibraryView.Detailed).Should().Equal(
            LibraryColumnOption.Title,
            LibraryColumnOption.Authors,
            LibraryColumnOption.Series);
        reloadedViewModel.SelectedView = LibraryView.List;
        reloadedViewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Title,
            LibraryColumnOption.Format);
    }

    [Fact]
    public async Task Column_choices_can_reorder_visible_columns()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Title, LibraryColumnOption.Authors, LibraryColumnOption.Series]);

        var seriesChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Series);
        await viewModel.MoveColumnUpCommand.ExecuteAsync(seriesChoice);
        await viewModel.MoveColumnUpCommand.ExecuteAsync(seriesChoice);

        viewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Series,
            LibraryColumnOption.Title,
            LibraryColumnOption.Authors);
        settingsStore.Settings.LibraryColumns!.Detailed.Should().Equal("Series", "Title", "Authors");
        settingsStore.Settings.LibraryViewLayouts!.Views![nameof(LibraryView.Detailed)]
            .Columns.Should().Equal("Series", "Title", "Authors");
    }

    [Fact]
    public async Task Column_choices_can_be_reordered_by_drag_target()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Title, LibraryColumnOption.Authors, LibraryColumnOption.Series]);

        var seriesChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Series);
        var titleChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Title);
        await viewModel.ReorderColumnChoiceAsync(seriesChoice, titleChoice);

        viewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Series,
            LibraryColumnOption.Title,
            LibraryColumnOption.Authors);
        settingsStore.Settings.LibraryColumns!.Detailed.Should().Equal("Series", "Title", "Authors");
    }

    [Fact]
    public async Task Column_choices_can_be_dropped_after_inactive_columns_to_move_to_visible_end()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Title, LibraryColumnOption.Authors, LibraryColumnOption.Series]);

        var titleChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Title);
        var formatChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Format);
        formatChoice.IsSelected.Should().BeFalse();
        await viewModel.ReorderColumnChoiceAsync(titleChoice, formatChoice);

        viewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Authors,
            LibraryColumnOption.Series,
            LibraryColumnOption.Title);
        settingsStore.Settings.LibraryColumns!.Detailed.Should().Equal("Authors", "Series", "Title");
    }

    [Fact]
    public async Task Column_choices_can_be_dropped_on_empty_list_space_to_move_to_visible_end()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Title, LibraryColumnOption.Authors, LibraryColumnOption.Series]);

        var titleChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Title);
        await viewModel.ReorderColumnChoiceAsync(titleChoice, targetChoice: null);

        viewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Authors,
            LibraryColumnOption.Series,
            LibraryColumnOption.Title);
        settingsStore.Settings.LibraryColumns!.Detailed.Should().Equal("Authors", "Series", "Title");
    }

    [Fact]
    public async Task Hidden_column_choices_are_not_moved_into_visible_columns()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Title, LibraryColumnOption.Authors]);

        var seriesChoice = viewModel.ColumnChoices.Single(choice => choice.Option == LibraryColumnOption.Series);
        await viewModel.MoveColumnUpCommand.ExecuteAsync(seriesChoice);
        await viewModel.MoveColumnDownCommand.ExecuteAsync(seriesChoice);

        viewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Title,
            LibraryColumnOption.Authors);
    }

    [Fact]
    public async Task Column_widths_are_saved_per_grid_view()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetColumnWidthAsync(LibraryView.Detailed, LibraryColumnOption.Title, 345.678);
        await viewModel.SetColumnWidthAsync(LibraryView.List, LibraryColumnOption.Format, 98.25);

        viewModel.GetColumnWidth(LibraryView.Detailed, LibraryColumnOption.Title, 220).Should().Be(345.68);
        viewModel.GetColumnWidth(LibraryView.List, LibraryColumnOption.Format, 130).Should().Be(98.25);
        viewModel.GetColumnWidth(LibraryView.Detailed, LibraryColumnOption.Authors, 220).Should().Be(220);
        settingsStore.Settings.LibraryColumnWidths.Should().NotBeNull();
        settingsStore.Settings.LibraryColumnWidths!.Detailed.Should().Contain("Title", 345.68);
        settingsStore.Settings.LibraryColumnWidths.List.Should().Contain("Format", 98.25);

        var reloadedViewModel = CreateViewModel(
            [CreateBook("Book", ["Author"])],
            settingsStore: settingsStore);
        await reloadedViewModel.RefreshAsync();

        reloadedViewModel.GetColumnWidth(LibraryView.Detailed, LibraryColumnOption.Title, 220).Should().Be(345.68);
        reloadedViewModel.GetColumnWidth(LibraryView.List, LibraryColumnOption.Format, 130).Should().Be(98.25);
    }

    [Fact]
    public async Task Column_width_saves_preserve_duplicate_candidate_widths()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                LibraryColumnWidths = new LibraryColumnWidthSettings(
                    DuplicateCandidates: new Dictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["Title"] = 360,
                        ["Authors"] = 280
                    })
            },
            CancellationToken.None);
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        await viewModel.SetColumnWidthAsync(LibraryView.Detailed, LibraryColumnOption.Title, 345.678);

        settingsStore.Settings.LibraryColumnWidths.Should().NotBeNull();
        settingsStore.Settings.LibraryColumnWidths!.Detailed.Should().Contain("Title", 345.68);
        settingsStore.Settings.LibraryColumnWidths.DuplicateCandidates.Should().Contain("Title", 360);
        settingsStore.Settings.LibraryColumnWidths.DuplicateCandidates.Should().Contain("Authors", 280);
    }

    [Fact]
    public async Task Refresh_prefers_unified_view_layout_settings_when_available()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(
            settingsStore.Settings with
            {
                LibraryGroupings = new LibraryGroupingSettings(Detailed: ["Author"]),
                LibraryColumns = new LibraryColumnSettings(Detailed: ["Title", "Authors"]),
                LibraryColumnWidths = new LibraryColumnWidthSettings(
                    Detailed: new Dictionary<string, double>
                    {
                        ["Title"] = 111
                    }),
                LibrarySorts = new LibrarySortSettings(Detailed: "Author"),
                LibraryViewLayouts = new LibraryViewLayoutSettings(
                    new Dictionary<string, LibraryViewLayoutSetting>(StringComparer.Ordinal)
                    {
                        [nameof(LibraryView.Detailed)] = new(
                            Groupings: ["Tag"],
                            Columns: ["Cover", "Title", "Series"],
                            ColumnWidths: new Dictionary<string, double>
                            {
                                ["Title"] = 333
                            },
                            Sort: "Title")
                    })
            },
            CancellationToken.None);
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], tags: ["Tag"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.SelectedView.Should().Be(LibraryView.Detailed);
        viewModel.ActiveGroupOptions.Should().Equal(LibraryGroupOption.Tag);
        viewModel.ActiveColumnOptions.Should().Equal(
            LibraryColumnOption.Cover,
            LibraryColumnOption.Title,
            LibraryColumnOption.Series);
        viewModel.GetColumnWidth(LibraryView.Detailed, LibraryColumnOption.Title, 220).Should().Be(333);
        viewModel.SelectedSortOption.Should().Be(LibrarySortOption.Title);
    }

    [Fact]
    public async Task View_customization_saves_unified_view_layout_settings()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], tags: ["Tag"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SelectedSortOption = LibrarySortOption.Title;
        viewModel.SetGroupingOptions([LibraryGroupOption.Tag]);
        await viewModel.SetVisibleColumnsAsync(
            LibraryView.Detailed,
            [LibraryColumnOption.Cover, LibraryColumnOption.Title, LibraryColumnOption.Series]);
        await viewModel.SetColumnWidthAsync(LibraryView.Detailed, LibraryColumnOption.Title, 333);
        await viewModel.WaitForPendingGroupingSettingsSaveAsync();
        await viewModel.WaitForPendingSortSettingsSaveAsync();

        settingsStore.Settings.LibraryViewLayouts.Should().NotBeNull();
        var detailed = settingsStore.Settings.LibraryViewLayouts!.Views![nameof(LibraryView.Detailed)];
        detailed.Groupings.Should().Equal(nameof(LibraryGroupOption.Tag));
        detailed.Columns.Should().Equal("Cover", "Title", "Series");
        detailed.ColumnWidths.Should().Contain("Title", 333);
        detailed.Sort.Should().Be(nameof(LibrarySortOption.Title));
    }

    [Fact]
    public async Task Reset_current_view_layout_restores_defaults_without_changing_other_views()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], tags: ["Tag"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SelectedSortOption = LibrarySortOption.Title;
        viewModel.SetGroupingOptions([LibraryGroupOption.Author]);
        await viewModel.SetVisibleColumnsAsync(LibraryView.Detailed, [LibraryColumnOption.Title, LibraryColumnOption.Authors]);
        await viewModel.SetColumnWidthAsync(LibraryView.Detailed, LibraryColumnOption.Title, 360);
        viewModel.SelectedView = LibraryView.List;
        viewModel.SelectedSortOption = LibrarySortOption.Category;
        viewModel.SetGroupingOptions([LibraryGroupOption.Tag]);
        await viewModel.WaitForPendingGroupingSettingsSaveAsync();
        await viewModel.WaitForPendingSortSettingsSaveAsync();

        viewModel.SelectedView = LibraryView.Detailed;
        await viewModel.ResetCurrentViewLayoutCommand.ExecuteAsync(null);

        viewModel.SelectedSortOption.Should().Be(LibrarySortOption.None);
        viewModel.ActiveGroupOptions.Should().BeEmpty();
        viewModel.GetVisibleColumns(LibraryView.Detailed).Should().Equal(
            DefaultDetailedColumnOptions().Select(LibraryColumnKey.FromStandard));
        viewModel.GetColumnWidth(LibraryView.Detailed, LibraryColumnOption.Title, 220).Should().Be(220);
        viewModel.SelectedView = LibraryView.List;
        viewModel.SelectedSortOption.Should().Be(LibrarySortOption.Category);
        viewModel.ActiveGroupOptions.Should().Equal(LibraryGroupOption.Tag);
        settingsStore.Settings.LibrarySorts!.Detailed.Should().Be(nameof(LibrarySortOption.None));
        settingsStore.Settings.LibrarySorts.List.Should().Be(nameof(LibrarySortOption.Category));
        settingsStore.Settings.LibraryGroupings!.Detailed.Should().BeEmpty();
        settingsStore.Settings.LibraryGroupings.List.Should().Equal(nameof(LibraryGroupOption.Tag));
        settingsStore.Settings.LibraryColumnWidths!.Detailed.Should().BeEmpty();
    }

    [Fact]
    public async Task Reset_current_view_layout_invalidates_pending_grouping_settings_save()
    {
        var settingsStore = new BlockingAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], tags: ["Tag"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        settingsStore.BlockNextLoad();
        viewModel.SetGroupingOptions([LibraryGroupOption.Author]);
        await settingsStore.BlockedLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var resetTask = viewModel.ResetCurrentViewLayoutCommand.ExecuteAsync(null);
        settingsStore.ReleaseBlockedLoad();

        await resetTask.WaitAsync(TimeSpan.FromSeconds(5));
        await viewModel.WaitForPendingGroupingSettingsSaveAsync().WaitAsync(TimeSpan.FromSeconds(5));

        settingsStore.Settings.LibraryGroupings.Should().NotBeNull();
        settingsStore.Settings.LibraryGroupings!.Detailed.Should().BeEmpty();
    }

    [Fact]
    public async Task Reset_current_view_layout_can_clear_bookshelf_sorting_and_grouping()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel([CreateBook("Book", ["Author"], tags: ["Tag"])], settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SelectedView = LibraryView.Bookshelf;
        viewModel.SelectedSortOption = LibrarySortOption.Author;
        viewModel.SetGroupingOptions([LibraryGroupOption.Tag]);
        await viewModel.WaitForPendingGroupingSettingsSaveAsync();
        await viewModel.WaitForPendingSortSettingsSaveAsync();

        await viewModel.ResetCurrentViewLayoutCommand.ExecuteAsync(null);

        viewModel.SelectedSortOption.Should().Be(LibrarySortOption.None);
        viewModel.ActiveGroupOptions.Should().BeEmpty();
        settingsStore.Settings.LibrarySorts!.Bookshelf.Should().Be(nameof(LibrarySortOption.None));
        settingsStore.Settings.LibraryGroupings!.Bookshelf.Should().BeEmpty();
    }

    [Fact]
    public async Task Grouping_refresh_preserves_expanded_group_nodes()
    {
        var book = CreateBook("Book", ["Author"], series: "Series");
        var viewModel = CreateViewModel([book]);

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions([LibraryGroupOption.Author, LibraryGroupOption.Series]);
        var authorGroup = viewModel.GroupedLibraryNodes.Should().ContainSingle().Which;
        authorGroup.IsExpanded = true;
        authorGroup.Groups.Should().ContainSingle().Which.IsExpanded = true;

        viewModel.SearchText = "Book";

        authorGroup = viewModel.GroupedLibraryNodes.Should().ContainSingle().Which;
        authorGroup.IsExpanded.Should().BeTrue();
        authorGroup.Groups.Should().ContainSingle().Which.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public async Task Grouping_headers_use_localized_fallbacks_and_status_values()
    {
        var book = CreateBook("Book", [], formats: []);
        var viewModel = CreateViewModel(
            [book],
            localize: key => key switch
            {
                "GroupUnknownAuthor" => "Localized author",
                "Unread" => "Localized unread",
                _ => key
            });

        await viewModel.RefreshAsync();
        viewModel.SetGroupingOptions([LibraryGroupOption.Author]);
        viewModel.GroupedLibraryNodes.Should().ContainSingle()
            .Which.Header.Should().Be("Localized author");

        viewModel.SetGroupingOptions([LibraryGroupOption.Status]);
        viewModel.GroupedLibraryNodes.Should().ContainSingle()
            .Which.Header.Should().Be("Localized unread");
    }


    [Fact]
    public async Task Selected_filters_expand_results_across_facets()
    {
        var art = CreateBook("Art Book", ["Art Jefferson"]);
        var kim = CreateBook("Kim Book", ["Kim Maurits"]);
        var arendsoog = CreateBook("Arendsoog", ["N. Nowee"], series: "Arendsoog");
        var unrelated = CreateBook("Other", ["Other Author"], series: "Other Series");
        var viewModel = CreateViewModel([art, kim, arendsoog, unrelated]);

        await viewModel.RefreshAsync();

        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().BeEquivalentTo("Art Book", "Kim Book", "Arendsoog", "Other");

        viewModel.AuthorFilters.Single(filter => filter.Name == "Art Jefferson").IsSelected = true;
        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().Equal("Art Book");

        viewModel.AuthorFilters.Single(filter => filter.Name == "Kim Maurits").IsSelected = true;
        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().BeEquivalentTo("Art Book", "Kim Book");

        viewModel.SeriesFilters.Single(filter => filter.Name == "Arendsoog").IsSelected = true;
        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().BeEquivalentTo("Art Book", "Kim Book", "Arendsoog");
    }

    [Fact]
    public async Task Rename_author_filter_updates_all_matching_books_and_refreshes_filters()
    {
        var first = CreateBook("First", ["Ake Edwardson"]);
        var second = CreateBook("Second", ["Ake Edwardson", "Other"]);
        var repository = new StaticBookRepository([first, second]);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "Åke Edwardson" };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RenameAuthorFilterCommand.ExecuteAsync(
            viewModel.AuthorFilters.Single(filter => filter.Name == "Ake Edwardson"));

        repository.BooksSnapshot.SelectMany(book => book.Metadata.Authors)
            .Should().Contain("Åke Edwardson")
            .And.NotContain("Ake Edwardson");
        viewModel.AuthorFilters.Should().ContainSingle(filter => filter.Name == "Åke Edwardson" && filter.Count == 2);
        viewModel.VisibleBooks.Should().HaveCount(2);
    }

    [Fact]
    public async Task Rename_author_filter_uses_bulk_list_metadata_update_when_available()
    {
        var first = CreateBook("First", ["Ake Edwardson"]);
        var second = CreateBook("Second", ["Ake Edwardson", "Other"]);
        var repository = new BulkScalarMetadataRepository([first, second]);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "Åke Edwardson" };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RenameAuthorFilterCommand.ExecuteAsync(
            viewModel.AuthorFilters.Single(filter => filter.Name == "Ake Edwardson"));

        repository.BulkListUpdateCalls.Should().Be(1);
        repository.UpdateCalls.Should().Be(0);
        repository.BooksSnapshot.SelectMany(book => book.Metadata.Authors)
            .Should().Contain("Åke Edwardson")
            .And.NotContain("Ake Edwardson");
    }

    [Fact]
    public async Task Remove_tag_filter_removes_value_from_all_matching_books_and_refreshes_filters()
    {
        var first = CreateBook("First", ["Author"], tags: ["Keep", "RemoveMe"]);
        var second = CreateBook("Second", ["Author"], tags: ["RemoveMe"]);
        var repository = new StaticBookRepository([first, second]);
        var interaction = new ScriptedUserInteractionService { ConfirmMetadataValueRemovalResult = true };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RemoveTagFilterCommand.ExecuteAsync(
            viewModel.CategoryFilters.Single(filter => filter.Name == "RemoveMe"));

        repository.BooksSnapshot.SelectMany(book => book.Metadata.Tags ?? [])
            .Should().NotContain("RemoveMe");
        repository.BooksSnapshot.Single(book => book.Metadata.Title == "First").Metadata.Tags
            .Should().Equal("Keep");
        repository.BooksSnapshot.Single(book => book.Metadata.Title == "Second").Metadata.Tags
            .Should().BeNull();
        viewModel.CategoryFilters.Should().NotContain(filter => filter.Name == "RemoveMe");
    }

    [Fact]
    public async Task Remove_tag_filter_uses_bulk_list_metadata_update_when_available()
    {
        var first = CreateBook("First", ["Author"], tags: ["Keep", "RemoveMe"]);
        var second = CreateBook("Second", ["Author"], tags: ["RemoveMe"]);
        var repository = new BulkScalarMetadataRepository([first, second]);
        var interaction = new ScriptedUserInteractionService { ConfirmMetadataValueRemovalResult = true };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RemoveTagFilterCommand.ExecuteAsync(
            viewModel.CategoryFilters.Single(filter => filter.Name == "RemoveMe"));

        repository.BulkListUpdateCalls.Should().Be(1);
        repository.UpdateCalls.Should().Be(0);
        repository.BooksSnapshot.SelectMany(book => book.Metadata.Tags ?? [])
            .Should().NotContain("RemoveMe");
    }

    [Fact]
    public async Task Rename_series_filter_updates_matching_books()
    {
        var first = CreateBook("First", ["Author"], series: "Old Series");
        var second = CreateBook("Second", ["Author"], series: "Other Series");
        var repository = new StaticBookRepository([first, second]);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "New Series" };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RenameSeriesFilterCommand.ExecuteAsync(
            viewModel.SeriesFilters.Single(filter => filter.Name == "Old Series"));

        repository.BooksSnapshot.Single(book => book.Metadata.Title == "First").Metadata.Series
            .Should().Be("New Series");
        repository.BooksSnapshot.Single(book => book.Metadata.Title == "Second").Metadata.Series
            .Should().Be("Other Series");
        viewModel.SeriesFilters.Should().ContainSingle(filter => filter.Name == "New Series");
    }

    [Fact]
    public async Task Rename_language_filter_updates_all_values_in_the_same_language_group()
    {
        var first = CreateBook("First", ["Author"], language: "eng");
        var second = CreateBook("Second", ["Author"], language: "en-US");
        var repository = new BulkScalarMetadataRepository([first, second]);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "en" };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RenameLanguageFilterCommand.ExecuteAsync(
            viewModel.LanguageFilters.Single(filter => filter.Name == "en"));

        repository.BulkUpdateCalls.Should().Be(1);
        repository.UpdateCalls.Should().Be(0);
        repository.BooksSnapshot.Select(book => book.Metadata.Language)
            .Should().Equal("en", "en");
        viewModel.LanguageFilters.Should().ContainSingle(filter => filter.Name == "en" && filter.Count == 2);
        viewModel.IsCleaningMetadata.Should().BeFalse();
    }

    [Fact]
    public async Task Normalize_language_metadata_updates_known_codes_after_confirmation()
    {
        var englishLegacy = CreateBook("Legacy English", ["Author"], language: "eng");
        var englishRegional = CreateBook("Regional English", ["Author"], language: "en-US");
        var dutchRegional = CreateBook("Regional Dutch", ["Author"], language: "nl-NL");
        var dutchName = CreateBook("Named Dutch", ["Author"], language: "Nederlands");
        var unknown = CreateBook("Unknown", ["Author"], language: "fictional-language");
        var latin = CreateBook("Latin", ["Author"], language: "Latin");
        var repository = new BulkScalarMetadataRepository([englishLegacy, englishRegional, dutchRegional, dutchName, unknown, latin]);
        var interaction = new ScriptedUserInteractionService { ConfirmLanguageNormalizationResult = true };
        var viewModel = CreateViewModel(
            [englishLegacy, englishRegional, dutchRegional, dutchName, unknown, latin],
            interaction,
            repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.NormalizeLanguageMetadataCommand.ExecuteAsync(null);

        interaction.ConfirmLanguageNormalizationAffectedCount.Should().Be(4);
        repository.BulkUpdateCalls.Should().Be(2);
        repository.UpdateCalls.Should().Be(0);
        repository.BooksSnapshot.Select(book => book.Metadata.Language)
            .Should().Equal("en", "en", "nl", "nl", "fictional-language", "Latin");
        viewModel.LanguageFilters.Select(filter => filter.Name)
            .Should().Contain(["en", "nl", "fictional-language", "Latin"]);
        viewModel.IsCleaningMetadata.Should().BeFalse();
    }

    [Fact]
    public async Task Normalize_language_metadata_does_not_update_without_confirmation()
    {
        var book = CreateBook("Legacy English", ["Author"], language: "eng");
        var repository = new BulkScalarMetadataRepository([book]);
        var interaction = new ScriptedUserInteractionService { ConfirmLanguageNormalizationResult = false };
        var viewModel = CreateViewModel([book], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.NormalizeLanguageMetadataCommand.ExecuteAsync(null);

        repository.BulkUpdateCalls.Should().Be(0);
        repository.BooksSnapshot.Single().Metadata.Language.Should().Be("eng");
    }

    [Fact]
    public async Task Rename_filter_shows_metadata_cleanup_busy_state_until_update_completes()
    {
        var first = CreateBook("First", ["Author"], language: "eng");
        var repository = new BlockingBulkScalarMetadataRepository([first]);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "nl" };
        var viewModel = CreateViewModel([first], interaction, repository: repository);

        await viewModel.RefreshAsync();
        var rename = viewModel.RenameLanguageFilterCommand.ExecuteAsync(
            viewModel.LanguageFilters.Single(filter => filter.Name == "en"));
        await repository.BeforeBulkUpdate.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.IsCleaningMetadata.Should().BeTrue();
        viewModel.MetadataCleanupStatusText.Should().Be("Updating metadata...");
        repository.ReleaseBeforeBulkUpdate();
        await repository.BulkUpdateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        repository.ReleaseBulkUpdate();
        await rename;

        viewModel.IsCleaningMetadata.Should().BeFalse();
    }

    [Fact]
    public async Task Rename_filter_skips_books_that_conflict_with_existing_metadata()
    {
        var first = CreateBook("Same Title", ["Old Author"]);
        var second = CreateBook("Other Title", ["Old Author"]);
        var repository = new ConflictingBookRepository([first, second], first.Id);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "New Author" };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RenameAuthorFilterCommand.ExecuteAsync(
            viewModel.AuthorFilters.Single(filter => filter.Name == "Old Author"));

        repository.BooksSnapshot.Single(book => book.Id == first.Id).Metadata.Authors
            .Should().Equal("Old Author");
        repository.BooksSnapshot.Single(book => book.Id == second.Id).Metadata.Authors
            .Should().Equal("New Author");
    }

    [Fact]
    public async Task Rename_filter_falls_back_to_per_book_updates_when_bulk_list_update_conflicts()
    {
        var first = CreateBook("Same Title", ["Old Author"]);
        var second = CreateBook("Other Title", ["Old Author"]);
        var repository = new ConflictingBulkListMetadataRepository([first, second], first.Id);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "New Author" };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RenameAuthorFilterCommand.ExecuteAsync(
            viewModel.AuthorFilters.Single(filter => filter.Name == "Old Author"));

        repository.BulkListUpdateCalls.Should().Be(1);
        repository.UpdateCalls.Should().Be(1);
        repository.BooksSnapshot.Single(book => book.Id == first.Id).Metadata.Authors
            .Should().Equal("Old Author");
        repository.BooksSnapshot.Single(book => book.Id == second.Id).Metadata.Authors
            .Should().Equal("New Author");
    }

    [Fact]
    public async Task Rename_filter_conflict_fallback_preserves_cover_bytes_from_full_book()
    {
        var fullBook = CreateBook("Same Title", ["Old Author"], coverBytes: [1, 2, 3]);
        var listBook = fullBook with
        {
            Metadata = new BookMetadata(
                fullBook.Metadata.Title,
                fullBook.Metadata.Authors,
                fullBook.Metadata.Description,
                fullBook.Metadata.Language,
                fullBook.Metadata.Publisher,
                fullBook.Metadata.PublicationDate,
                fullBook.Metadata.Tags,
                fullBook.Metadata.Series,
                fullBook.Metadata.SeriesNumber,
                fullBook.Metadata.Isbn)
        };
        var repository = new FullBookOnGetConflictingBulkListMetadataRepository([listBook], fullBook);
        var interaction = new ScriptedUserInteractionService { PromptTextResult = "New Author" };
        var viewModel = CreateViewModel([listBook], interaction, repository: repository);

        await viewModel.RefreshAsync();
        await viewModel.RenameAuthorFilterCommand.ExecuteAsync(
            viewModel.AuthorFilters.Single(filter => filter.Name == "Old Author"));

        repository.UpdatedBook.Should().NotBeNull();
        repository.UpdatedBook!.Metadata.Authors.Should().Equal("New Author");
        repository.UpdatedBook.Metadata.CoverBytes.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Metadata_multi_edit_updates_only_selected_books_and_refreshes_filters()
    {
        var first = CreateBook("First", ["Old Author"], tags: ["Old"], series: "Old Series", language: "en");
        var second = CreateBook("Second", ["Old Author"], tags: ["Old"], series: "Old Series", language: "en");
        var third = CreateBook("Third", ["Old Author"], tags: ["Old"], series: "Old Series", language: "en");
        var repository = new StaticBookRepository([first, second, third]);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: true,
                AuthorsText: "New Author; Second Author",
                UpdateSeries: true,
                SeriesText: "New Series",
                UpdateTags: true,
                TagsText: "New; Cleanup",
                UpdateLanguage: true,
                LanguageText: "nl",
                UpdateStatus: true,
                Status: ReadingStatus.Read)
        };
        var viewModel = CreateViewModel([first, second, third], interaction, repository: repository);

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks.Take(2));
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        interaction.MetadataMultiEditSelectedBookCount.Should().Be(2);
        repository.UpdateCalls.Should().Be(2);
        repository.BooksSnapshot.Where(book => book.Id != third.Id).Should().AllSatisfy(book =>
        {
            book.Metadata.Authors.Should().Equal("New Author", "Second Author");
            book.Metadata.Series.Should().Be("New Series");
            book.Metadata.Tags.Should().Equal("New", "Cleanup");
            book.Metadata.Language.Should().Be("nl");
            book.ReadingStatus.Should().Be(ReadingStatus.Read);
        });
        repository.BooksSnapshot.Single(book => book.Id == third.Id).Metadata.Series.Should().Be("Old Series");
        viewModel.SeriesFilters.Should().ContainSingle(filter => filter.Name == "New Series" && filter.Count == 2);
        viewModel.CategoryFilters.Should().Contain(filter => filter.Name == "Cleanup" && filter.Count == 2);
    }

    [Fact]
    public async Task Metadata_multi_edit_adds_new_series_filter_when_selected_books_had_no_series()
    {
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        var repository = new StaticBookRepository([first, second]);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: false,
                AuthorsText: string.Empty,
                UpdateSeries: true,
                SeriesText: "Brand New Series",
                UpdateTags: false,
                TagsText: string.Empty,
                UpdateLanguage: false,
                LanguageText: string.Empty,
                UpdateStatus: false,
                Status: ReadingStatus.Unread)
        };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        viewModel.SeriesFilters.Should().BeEmpty();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks);
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.BooksSnapshot.Should().AllSatisfy(book => book.Metadata.Series.Should().Be("Brand New Series"));
        viewModel.SeriesFilters.Should().ContainSingle(filter => filter.Name == "Brand New Series" && filter.Count == 2);
    }

    [Fact]
    public async Task Metadata_multi_edit_can_add_tags_without_replacing_existing_tags()
    {
        var first = CreateBook("First", ["Author"], tags: ["Old", "Keep"]);
        var second = CreateBook("Second", ["Author"], tags: ["Keep"]);
        var repository = new StaticBookRepository([first, second]);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: false,
                AuthorsText: string.Empty,
                UpdateSeries: false,
                SeriesText: string.Empty,
                UpdateTags: true,
                TagsText: "New; old",
                UpdateLanguage: false,
                LanguageText: string.Empty,
                UpdateStatus: false,
                Status: ReadingStatus.Unread,
                TagAction: MetadataMultiEditTagAction.Add)
        };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks);
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.BooksSnapshot.Single(book => book.Id == first.Id).Metadata.Tags.Should().Equal("Old", "Keep", "New");
        repository.BooksSnapshot.Single(book => book.Id == second.Id).Metadata.Tags.Should().Equal("Keep", "New", "old");
        viewModel.CategoryFilters.Should().Contain(filter => filter.Name == "New" && filter.Count == 2);
    }

    [Fact]
    public async Task Metadata_multi_edit_can_remove_tags_without_clearing_other_tags()
    {
        var first = CreateBook("First", ["Author"], tags: ["Old", "Keep"]);
        var second = CreateBook("Second", ["Author"], tags: ["Old"]);
        var repository = new StaticBookRepository([first, second]);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: false,
                AuthorsText: string.Empty,
                UpdateSeries: false,
                SeriesText: string.Empty,
                UpdateTags: true,
                TagsText: "old",
                UpdateLanguage: false,
                LanguageText: string.Empty,
                UpdateStatus: false,
                Status: ReadingStatus.Unread,
                TagAction: MetadataMultiEditTagAction.Remove)
        };
        var viewModel = CreateViewModel([first, second], interaction, repository: repository);

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks);
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.BooksSnapshot.Single(book => book.Id == first.Id).Metadata.Tags.Should().Equal("Keep");
        repository.BooksSnapshot.Single(book => book.Id == second.Id).Metadata.Tags.Should().BeNull();
        viewModel.CategoryFilters.Should().ContainSingle(filter => filter.Name == "Keep" && filter.Count == 1);
        viewModel.CategoryFilters.Should().NotContain(filter => filter.Name == "Old");
    }

    [Fact]
    public async Task Metadata_multi_edit_swaps_title_and_author_for_selected_books()
    {
        var first = CreateBook("Karin Slaughter", ["Triptiek"], language: "nl");
        var second = CreateBook("Lee Child", ["Spervuur"], language: "nl");
        var coauthored = CreateBook("Shared Title", ["Author One", "Author Two"], language: "en");
        var untouched = CreateBook("The Hobbit", ["J.R.R. Tolkien"], language: "en");
        var repository = new StaticBookRepository([first, second, coauthored, untouched]);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: false,
                AuthorsText: string.Empty,
                UpdateSeries: false,
                SeriesText: string.Empty,
                UpdateTags: false,
                TagsText: string.Empty,
                UpdateLanguage: false,
                LanguageText: string.Empty,
                UpdateStatus: false,
                Status: ReadingStatus.Unread,
                SwapTitleAndAuthors: true)
        };
        var viewModel = CreateViewModel([first, second, coauthored, untouched], interaction, repository: repository);

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks.Where(book => book.Id != untouched.Id));
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.UpdateCalls.Should().Be(3);
        repository.BooksSnapshot.Single(book => book.Id == first.Id).Metadata.Title.Should().Be("Triptiek");
        repository.BooksSnapshot.Single(book => book.Id == first.Id).Metadata.Authors.Should().Equal("Karin Slaughter");
        repository.BooksSnapshot.Single(book => book.Id == second.Id).Metadata.Title.Should().Be("Spervuur");
        repository.BooksSnapshot.Single(book => book.Id == second.Id).Metadata.Authors.Should().Equal("Lee Child");
        repository.BooksSnapshot.Single(book => book.Id == coauthored.Id).Metadata.Title.Should().Be("Author One, Author Two");
        repository.BooksSnapshot.Single(book => book.Id == coauthored.Id).Metadata.Authors.Should().Equal("Shared Title");
        repository.BooksSnapshot.Single(book => book.Id == untouched.Id).Metadata.Title.Should().Be("The Hobbit");
        viewModel.AuthorFilters.Should().ContainSingle(filter => filter.Name == "Karin Slaughter" && filter.Count == 1);
        viewModel.VisibleBooks.Select(book => book.Title).Should().Contain(["Triptiek", "Spervuur", "Author One, Author Two", "The Hobbit"]);
    }

    [Fact]
    public async Task Metadata_multi_edit_updates_custom_metadata_fields_and_refreshes_filters()
    {
        var first = CreateBook("First", ["Author"]);
        var second = CreateBook("Second", ["Author"]);
        var repository = new StaticBookRepository([first, second]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var textField = customMetadataRepository.AddDefinition("Leesclub", CustomMetadataFieldType.Text);
        var numberField = customMetadataRepository.AddDefinition("Cijfer", CustomMetadataFieldType.Number);
        var dateField = customMetadataRepository.AddDefinition("Leesdatum", CustomMetadataFieldType.Date);
        var booleanField = customMetadataRepository.AddDefinition("Gesigneerd", CustomMetadataFieldType.Boolean);
        var singleSelectField = customMetadataRepository.AddDefinition("Prioriteit", CustomMetadataFieldType.SingleSelect);
        var multiSelectField = customMetadataRepository.AddDefinition("Genres", CustomMetadataFieldType.MultiSelect);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: false,
                AuthorsText: string.Empty,
                UpdateSeries: false,
                SeriesText: string.Empty,
                UpdateTags: false,
                TagsText: string.Empty,
                UpdateLanguage: false,
                LanguageText: string.Empty,
                UpdateStatus: false,
                Status: ReadingStatus.Unread,
                CustomFields:
                [
                    new MetadataMultiEditCustomFieldResult(textField.Id, textField.Name, textField.Type, "Avondgroep"),
                    new MetadataMultiEditCustomFieldResult(numberField.Id, numberField.Name, numberField.Type, "8.5"),
                    new MetadataMultiEditCustomFieldResult(dateField.Id, dateField.Name, dateField.Type, "2026-08-14"),
                    new MetadataMultiEditCustomFieldResult(booleanField.Id, booleanField.Name, booleanField.Type, "True"),
                    new MetadataMultiEditCustomFieldResult(singleSelectField.Id, singleSelectField.Name, singleSelectField.Type, "Hoog"),
                    new MetadataMultiEditCustomFieldResult(multiSelectField.Id, multiSelectField.Name, multiSelectField.Type, "Thriller; Fantasy")
                ])
        };
        var viewModel = CreateViewModel(
            [first, second],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            repository: repository,
            customMetadataRepository: customMetadataRepository);

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks);
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        interaction.MetadataMultiEditCustomFieldNames.Should().Equal(
            "Leesclub",
            "Cijfer",
            "Leesdatum",
            "Gesigneerd",
            "Prioriteit",
            "Genres");
        repository.UpdateCalls.Should().Be(0);
        customMetadataRepository.ValuesSnapshot
            .Where(value => value.FieldId == textField.Id)
            .Should()
            .HaveCount(2)
            .And
            .AllSatisfy(value => value.TextValue.Should().Be("Avondgroep"));
        customMetadataRepository.ValuesSnapshot
            .Where(value => value.FieldId == booleanField.Id)
            .Should()
            .HaveCount(2)
            .And
            .AllSatisfy(value => value.BooleanValue.Should().BeTrue());
        customMetadataRepository.ValuesSnapshot
            .Where(value => value.FieldId == numberField.Id)
            .Should()
            .HaveCount(2)
            .And
            .AllSatisfy(value => value.NumberValue.Should().Be(8.5m));
        customMetadataRepository.ValuesSnapshot
            .Where(value => value.FieldId == dateField.Id)
            .Should()
            .HaveCount(2)
            .And
            .AllSatisfy(value => value.DateValue.Should().Be(new DateOnly(2026, 8, 14)));
        customMetadataRepository.ValuesSnapshot
            .Where(value => value.FieldId == singleSelectField.Id)
            .Should()
            .HaveCount(2)
            .And
            .AllSatisfy(value => value.TextValue.Should().Be("Hoog"));
        viewModel.CustomMetadataFilterGroups
            .Single(group => group.FieldId == textField.Id)
            .Filters
            .Should()
            .ContainSingle(filter => filter.Name == "Avondgroep" && filter.Count == 2);
        viewModel.CustomMetadataFilterGroups
            .Single(group => group.FieldId == multiSelectField.Id)
            .Filters
            .Select(filter => filter.Name)
            .Should()
            .Equal("Fantasy", "Thriller");
    }

    [Fact]
    public async Task Metadata_multi_edit_shows_validation_message_for_invalid_custom_value()
    {
        var book = CreateBook("First", ["Author"]);
        var repository = new StaticBookRepository([book]);
        var customMetadataRepository = new InMemoryCustomMetadataRepository();
        var numberField = customMetadataRepository.AddDefinition("Cijfer", CustomMetadataFieldType.Number);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: false,
                AuthorsText: string.Empty,
                UpdateSeries: false,
                SeriesText: string.Empty,
                UpdateTags: false,
                TagsText: string.Empty,
                UpdateLanguage: false,
                LanguageText: string.Empty,
                UpdateStatus: false,
                Status: ReadingStatus.Unread,
                CustomFields:
                [
                    new MetadataMultiEditCustomFieldResult(numberField.Id, numberField.Name, numberField.Type, "not a number")
                ])
        };
        var viewModel = CreateViewModel(
            [book],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            repository: repository,
            customMetadataRepository: customMetadataRepository,
            localize: key => key switch
            {
                "CustomMetadataValidationNumber" => "{0} must be a number.",
                "MetadataMultiEditTitle" => "Multi-edit metadata",
                _ => key
            });

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks);
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        customMetadataRepository.ValuesSnapshot.Should().BeEmpty();
        interaction.LastMessageTitle.Should().Be("Multi-edit metadata");
        interaction.LastMessageText.Should().Be("Cijfer must be a number.");
    }

    [Fact]
    public async Task Metadata_multi_edit_clearing_series_also_clears_series_number()
    {
        var book = CreateBook("First", ["Author"], series: "Old Series", seriesNumber: 3);
        var repository = new StaticBookRepository([book]);
        var interaction = new ScriptedUserInteractionService
        {
            MetadataMultiEditResult = new MetadataMultiEditResult(
                UpdateAuthors: false,
                AuthorsText: string.Empty,
                UpdateSeries: true,
                SeriesText: string.Empty,
                UpdateTags: false,
                TagsText: string.Empty,
                UpdateLanguage: false,
                LanguageText: string.Empty,
                UpdateStatus: false,
                Status: ReadingStatus.Unread)
        };
        var viewModel = CreateViewModel([book], interaction, repository: repository);

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks);
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        var updated = repository.BooksSnapshot.Single();
        updated.Metadata.Series.Should().BeNull();
        updated.Metadata.SeriesNumber.Should().BeNull();
    }

    [Fact]
    public async Task Metadata_multi_edit_cancel_does_not_update_books()
    {
        var book = CreateBook("First", ["Author"], series: "Old Series");
        var repository = new StaticBookRepository([book]);
        var interaction = new ScriptedUserInteractionService { MetadataMultiEditResult = null };
        var viewModel = CreateViewModel([book], interaction, repository: repository);

        await viewModel.RefreshAsync();
        viewModel.SetSelectedBooks(viewModel.VisibleBooks);
        await viewModel.ShowMetadataMultiEditCommand.ExecuteAsync(null);

        repository.UpdateCalls.Should().Be(0);
        repository.BooksSnapshot.Single().Metadata.Series.Should().Be("Old Series");
    }

    [Fact]
    public async Task Saved_details_refresh_visible_rows_and_filters()
    {
        var book = CreateBook("Original", ["Author"]);
        var repository = new StaticBookRepository([book]);
        var details = new BookDetailsViewModel(new BookService(
            repository,
            new NoopLibraryFileStore(),
            new NoopMetadataAdapterResolver()));
        var viewModel = CreateViewModel([book], repository: repository, details: details);

        await viewModel.RefreshAsync();
        viewModel.SelectedBook = viewModel.VisibleBooks.Single();
        viewModel.Details.Title = "Updated";
        viewModel.Details.Series = "New Series";

        await viewModel.Details.SaveCommand.ExecuteAsync(null);

        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Title.Should().Be("Updated");
        viewModel.SeriesFilters.Should().ContainSingle(filter => filter.Name == "New Series");
    }

    [Fact]
    public async Task Sort_option_orders_visible_rows_by_metadata()
    {
        var dune = CreateBook("Dune", ["Frank Herbert"], tags: ["Science fiction"]);
        var hobbit = CreateBook("The Hobbit", ["Tolkien"], tags: ["Fantasy"]);
        var alpha = CreateBook("Alpha", ["Zed"], tags: ["Mystery"]);
        var viewModel = CreateViewModel([dune, hobbit, alpha]);

        await viewModel.RefreshAsync();

        viewModel.SelectedSortOption = LibrarySortOption.Title;
        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().Equal("Alpha", "Dune", "The Hobbit");

        viewModel.SelectedSortOption = LibrarySortOption.Author;
        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().Equal("Dune", "The Hobbit", "Alpha");

        viewModel.SelectedSortOption = LibrarySortOption.Category;
        viewModel.VisibleBooks.Select(book => book.Title)
            .Should().Equal("The Hobbit", "Alpha", "Dune");
    }

    [Fact]
    public async Task Author_sort_option_uses_author_sort_strategy()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        await settingsStore.SaveAsync(settingsStore.Settings with
        {
            AuthorSortStrategy = AuthorSortStrategy.LastNameFirst
        }, default);
        var viewModel = CreateViewModel(
            [
                CreateBook("C", ["J.R.R. Tolkien"]),
                CreateBook("A", ["Karin Slaughter"]),
                CreateBook("B", ["Lee Child"])
            ],
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();

        viewModel.SelectedSortOption = LibrarySortOption.Author;

        viewModel.VisibleBooks.Select(row => row.Title).Should().Equal("B", "A", "C");
    }

    [Fact]
    public async Task Refresh_settings_dependent_display_applies_changed_author_sort_strategy_without_restart()
    {
        var settingsStore = new InMemoryAppSettingsStore();
        var viewModel = CreateViewModel(
            [
                CreateBook("C", ["J.R.R. Tolkien"]),
                CreateBook("A", ["Karin Slaughter"]),
                CreateBook("B", ["Lee Child"])
            ],
            settingsStore: settingsStore);

        await viewModel.RefreshAsync();
        viewModel.SelectedSortOption = LibrarySortOption.Author;
        viewModel.VisibleBooks.Select(row => row.Title).Should().Equal("C", "A", "B");

        await settingsStore.SaveAsync(settingsStore.Settings with
        {
            AuthorSortStrategy = AuthorSortStrategy.LastNameFirst
        }, default);
        await viewModel.RefreshSettingsDependentDisplayAsync();

        viewModel.VisibleBooks.Select(row => row.Title).Should().Equal("B", "A", "C");
        viewModel.AuthorFilters.Select(filter => filter.Name).Should().Equal("Lee Child", "Karin Slaughter", "J.R.R. Tolkien");
    }

    [Fact]
    public async Task Selecting_a_book_loads_details()
    {
        var book = CreateBook("Selected", ["Author"]);
        var viewModel = CreateViewModel([book]);

        await viewModel.RefreshAsync();
        viewModel.SelectedBook = viewModel.VisibleBooks.Single();

        viewModel.Details.BookId.Should().Be(book.Id);
        viewModel.Details.Title.Should().Be("Selected");
    }

    [Theory]
    [InlineData(LibraryView.Bookshelf)]
    [InlineData(LibraryView.Detailed)]
    [InlineData(LibraryView.List)]
    public void SelectedView_switches_between_supported_views(LibraryView selectedView)
    {
        var viewModel = CreateViewModel([]);

        viewModel.SelectedView = selectedView;

        viewModel.SelectedView.Should().Be(selectedView);
    }

    [Fact]
    public void ApplyDefaultViewPreference_switches_view_when_value_is_valid()
    {
        var viewModel = CreateViewModel([]);

        var applied = viewModel.ApplyDefaultViewPreference("Bookshelf");

        applied.Should().BeTrue();
        viewModel.SelectedView.Should().Be(LibraryView.Bookshelf);
    }

    [Fact]
    public void ApplyDefaultViewPreference_ignores_unknown_values()
    {
        var viewModel = CreateViewModel([]);
        viewModel.SelectedView = LibraryView.List;

        var applied = viewModel.ApplyDefaultViewPreference("Unknown");

        applied.Should().BeFalse();
        viewModel.SelectedView.Should().Be(LibraryView.List);
    }

    [Fact]
    public void ApplyDefaultViewPreference_ignores_undefined_numeric_values()
    {
        var viewModel = CreateViewModel([]);
        viewModel.SelectedView = LibraryView.List;

        var applied = viewModel.ApplyDefaultViewPreference("99");

        applied.Should().BeFalse();
        viewModel.SelectedView.Should().Be(LibraryView.List);
    }

    [Fact]
    public async Task CreateLibraryCommand_creates_default_elibrary_sets_current_library_and_refreshes()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var selectedParent = temporaryDirectory.CreateSubdirectory("Selected").FullName;
        var settingsStore = new InMemoryAppSettingsStore();
        var currentLibrary = new CurrentLibrary();
        var initializer = new RecordingLibraryDatabaseInitializer();
        var interaction = new ScriptedUserInteractionService { LibraryDirectory = selectedParent };
        var viewModel = CreateViewModel(
            [],
            interaction,
            new LibraryService(settingsStore),
            currentLibrary,
            initializer);

        await viewModel.CreateLibraryCommand.ExecuteAsync(null);

        var expectedLibraryPath = Path.Combine(selectedParent, "ELibrary");
        currentLibrary.Current.Should().NotBeNull();
        currentLibrary.Current!.DirectoryPath.Should().Be(Path.GetFullPath(expectedLibraryPath));
        Directory.Exists(Path.Combine(expectedLibraryPath, "books")).Should().BeTrue();
        initializer.InitializedLibraries.Should().ContainSingle()
            .Which.DirectoryPath.Should().Be(Path.GetFullPath(expectedLibraryPath));
        viewModel.CurrentLibraryName.Should().Be("ELibrary");
        viewModel.HasActiveLibrary.Should().BeTrue();
    }

    [Fact]
    public async Task OpenLibraryCommand_opens_existing_library_sets_current_library_and_refreshes()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var libraryPath = temporaryDirectory.CreateSubdirectory("MyLibrary").FullName;
        var settingsStore = new InMemoryAppSettingsStore();
        var currentLibrary = new CurrentLibrary();
        var initializer = new RecordingLibraryDatabaseInitializer();
        var interaction = new ScriptedUserInteractionService { LibraryDirectory = libraryPath };
        var viewModel = CreateViewModel(
            [],
            interaction,
            new LibraryService(settingsStore),
            currentLibrary,
            initializer);

        await viewModel.OpenLibraryCommand.ExecuteAsync(null);

        currentLibrary.Current.Should().NotBeNull();
        currentLibrary.Current!.Name.Should().Be("MyLibrary");
        currentLibrary.Current.DirectoryPath.Should().Be(Path.GetFullPath(libraryPath));
        initializer.InitializedLibraries.Should().ContainSingle()
            .Which.DirectoryPath.Should().Be(Path.GetFullPath(libraryPath));
        viewModel.CurrentLibraryName.Should().Be("MyLibrary");
    }

    [Fact]
    public async Task Refresh_clears_active_library_when_library_folder_was_deleted_outside_the_app()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var libraryPath = temporaryDirectory.CreateSubdirectory("DeletedLibrary").FullName;
        var currentLibrary = CreateActiveLibrary(libraryPath);
        var repository = new ThrowingBookRepository();
        var viewModel = CreateViewModel(
            [CreateBook("Ghost", ["Author"])],
            repository: repository,
            currentLibrary: currentLibrary);

        Directory.Delete(libraryPath, recursive: true);

        await viewModel.RefreshAsync();

        currentLibrary.Current.Should().BeNull();
        viewModel.HasActiveLibrary.Should().BeFalse();
        viewModel.CurrentLibraryPath.Should().BeNull();
        viewModel.CurrentLibraryName.Should().BeNull();
        viewModel.VisibleBooks.Should().BeEmpty();
        viewModel.EmptyStateMessage.Should().Be(
            "The active library folder no longer exists. Create or open a library to continue.");
    }

    [Fact]
    public async Task ScanFolderCommand_does_not_prompt_when_active_library_folder_was_deleted_outside_the_app()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var libraryPath = temporaryDirectory.CreateSubdirectory("DeletedLibrary").FullName;
        var currentLibrary = CreateActiveLibrary(libraryPath);
        var interaction = new ScriptedUserInteractionService { ScanFolder = temporaryDirectory.DirectoryPath };
        var viewModel = CreateViewModel(
            [],
            interaction,
            currentLibrary: currentLibrary,
            directoryScanner: new DirectoryScanner(),
            settingsStore: new InMemoryAppSettingsStore());

        Directory.Delete(libraryPath, recursive: true);

        await viewModel.ScanFolderCommand.ExecuteAsync(null);

        interaction.PickScanFolderCalls.Should().Be(0);
        currentLibrary.Current.Should().BeNull();
        viewModel.EmptyStateMessage.Should().Be(
            "The active library folder no longer exists. Create or open a library to continue.");
    }

    [Fact]
    public async Task AddBooksCommand_without_active_library_updates_empty_state_without_prompting_for_files()
    {
        var interaction = new ScriptedUserInteractionService();
        var viewModel = CreateViewModel([], interaction);

        await viewModel.AddBooksCommand.ExecuteAsync(null);

        interaction.PickBookFilesCalls.Should().Be(0);
        viewModel.EmptyStateMessage.Should().Be("Create or open a library before adding books.");
    }

    [Fact]
    public async Task ScanFolderCommand_without_active_library_updates_empty_state_without_prompting_for_folder()
    {
        var interaction = new ScriptedUserInteractionService();
        var viewModel = CreateViewModel([], interaction);

        await viewModel.ScanFolderCommand.ExecuteAsync(null);

        interaction.PickScanFolderCalls.Should().Be(0);
        viewModel.EmptyStateMessage.Should().Be("Create or open a library before scanning folders.");
    }

    [Fact]
    public async Task ScanFolderCommand_starts_import_with_directory_scan_context()
    {
        using var directory = new TemporaryDirectory();
        var source = Path.Combine(directory.DirectoryPath, "book.epub");
        File.WriteAllText(source, "book");
        var interaction = new ScriptedUserInteractionService { ScanFolder = directory.DirectoryPath };
        var agent = new ScriptedImportAgent();
        var viewModel = CreateViewModel(
            [],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            importAgent: agent,
            directoryScanner: new DirectoryScanner(),
            settingsStore: new InMemoryAppSettingsStore());

        await viewModel.ScanFolderCommand.ExecuteAsync(null);

        agent.StartScanningCalled.Should().BeTrue();
        agent.StartedSourcePaths.Should().Equal(source);
        agent.ImportContext.Should().Be(new ImportRunContext(
            ImportRunKind.DirectoryScan,
            directory.DirectoryPath,
            IncludeSubdirectories: true));
    }

    [Fact]
    public async Task ImportFilesAsync_starts_background_import_without_refreshing_during_progress()
    {
        var initial = CreateBook("Existing", ["Author"]);
        var imported = CreateBook("Imported", ["Author"]);
        var repository = new RefreshingBookRepository([initial], [initial, imported]);
        var agent = new ScriptedImportAgent();
        var viewModel = CreateViewModel(
            [initial],
            repository: repository,
            currentLibrary: CreateActiveLibrary(),
            importAgent: agent);

        await viewModel.RefreshAsync();
        await viewModel.ImportFilesAsync(["book.epub"]);
        agent.IsActive.Should().BeTrue();
        await agent.ReportProgressAsync(25);

        repository.ListCalls.Should().Be(1);
        viewModel.VisibleBooks.Select(book => book.Title).Should().NotContain("Imported");
    }

    [Fact]
    public async Task Import_completion_updates_last_result_and_refreshes_library()
    {
        var initial = CreateBook("Existing", ["Author"]);
        var imported = CreateBook("Imported", ["Author"]);
        var repository = new RefreshingBookRepository([initial], [initial, imported]);
        var agent = new ScriptedImportAgent();
        var viewModel = CreateViewModel(
            [initial],
            repository: repository,
            currentLibrary: CreateActiveLibrary(),
            importAgent: agent);
        var result = new ImportBatchResult(Guid.NewGuid(), [new ImportItemResult("book.epub", ImportOutcome.Added, "added")]);

        await viewModel.RefreshAsync();
        await viewModel.ImportFilesAsync(["book.epub"]);
        await agent.CompleteAsync(result);
        await WaitUntilAsync(() => VisibleBookTitles(viewModel).Contains("Imported", StringComparer.Ordinal));

        viewModel.LastImportResult.Should().NotBeNull();
        viewModel.LastImportResult!.TotalCount.Should().Be(1);
        VisibleBookTitles(viewModel).Should().Contain("Imported");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                condition().Should().BeTrue();
                return;
            }

            await Task.Delay(25);
        }
    }

    private static IReadOnlyList<string> VisibleBookTitles(LibraryViewModel viewModel)
    {
        try
        {
            return viewModel.VisibleBooks.Select(book => book.Title).ToArray();
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }

    [Fact]
    public async Task Import_history_command_opens_selected_run_details()
    {
        var runId = Guid.NewGuid();
        var interaction = new ScriptedUserInteractionService { SelectedImportRunId = runId };
        var importRepository = new StaticImportRepository(
            [
                new ImportRunSummary(
                    runId,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    TotalCount: 2,
                    AddedCount: 1,
                    ExactDuplicateCount: 1,
                    PossibleDuplicateCount: 0,
                    FailedCount: 0)
            ],
            new ImportRunResult(
                runId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                [
                    new ImportItemResult("added.epub", ImportOutcome.Added, "added"),
                    new ImportItemResult("duplicate.epub", ImportOutcome.ExactDuplicate, "duplicate")
                ]));
        var viewModel = CreateViewModel(
            [],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            importRepository: importRepository);

        await viewModel.ShowImportHistoryCommand.ExecuteAsync(null);

        interaction.ImportHistory.Should().NotBeNull();
        interaction.ShownImportResult.Should().NotBeNull();
        interaction.ShownImportResult!.RunId.Should().Be(runId);
        interaction.ShownImportResult.TotalCount.Should().Be(2);
        viewModel.LastImportResult.Should().BeSameAs(interaction.ShownImportResult);
    }

    [Fact]
    public async Task Import_result_retry_command_starts_import_for_retryable_failed_items()
    {
        var runId = Guid.NewGuid();
        var failedPath = Path.GetTempFileName();
        var agent = new ScriptedImportAgent();
        var interaction = new ScriptedUserInteractionService { SelectedImportRunId = runId };
        var importRepository = new StaticImportRepository(
            [
                new ImportRunSummary(
                    runId,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    TotalCount: 1,
                    AddedCount: 0,
                    ExactDuplicateCount: 0,
                    PossibleDuplicateCount: 0,
                    FailedCount: 1)
            ],
            new ImportRunResult(
                runId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                [
                    new ImportItemResult(failedPath, ImportOutcome.Failed, "source unreadable")
                ]));
        var viewModel = CreateViewModel(
            [],
            interaction,
            currentLibrary: CreateActiveLibrary(),
            importAgent: agent,
            importRepository: importRepository);

        try
        {
            await viewModel.ShowImportHistoryCommand.ExecuteAsync(null);

            await interaction.ShownImportResult!.RetryFailedCommand.ExecuteAsync(null);

            agent.StartedSourcePaths.Should().Equal(failedPath);
            agent.ImportContext.Should().Be(ImportRunContext.FileImport);
        }
        finally
        {
            File.Delete(failedPath);
        }
    }

    [Fact]
    public async Task Import_result_link_suggestion_command_attaches_files_and_refreshes_library()
    {
        var runId = Guid.NewGuid();
        var importedBookId = Guid.NewGuid();
        var targetBookId = Guid.NewGuid();
        var targetBefore = CreateBook("Pro Git", ["Scott Chacon"], id: targetBookId);
        var targetAfter = targetBefore with { Formats = [EbookFormat.Epub, EbookFormat.Pdf] };
        var importedBook = CreateBook("Pro Git", ["Unknown"], id: importedBookId);
        var repository = new RefreshingBookRepository([targetBefore, importedBook], [targetAfter]);
        var interaction = new ScriptedUserInteractionService { SelectedImportRunId = runId };
        var importRepository = new StaticImportRepository(
            [
                new ImportRunSummary(
                    runId,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    TotalCount: 1,
                    AddedCount: 1,
                    ExactDuplicateCount: 0,
                    PossibleDuplicateCount: 0,
                    FailedCount: 0)
            ],
            new ImportRunResult(
                runId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                [
                    new ImportItemResult(
                        "Pro Git.pdf",
                        ImportOutcome.Added,
                        "added; possible title match: Pro Git",
                        importedBookId,
                        Suggestion: new ImportItemSuggestion(
                            ImportItemSuggestionKind.TitleMatch,
                            targetBookId,
                            "Pro Git",
                            "Scott Chacon"))
                ]));
        var viewModel = CreateViewModel(
            [targetBefore, importedBook],
            interaction,
            repository: repository,
            currentLibrary: CreateActiveLibrary(),
            importRepository: importRepository);

        await viewModel.RefreshAsync();
        await viewModel.ShowImportHistoryCommand.ExecuteAsync(null);
        var item = interaction.ShownImportResult!.Items.Should().ContainSingle().Which;

        await item.LinkSuggestionCommand.ExecuteAsync(null);

        repository.AttachedSourceBookId.Should().Be(importedBookId);
        repository.AttachedTargetBookId.Should().Be(targetBookId);
        VisibleBookTitles(viewModel).Should().Equal("Pro Git");
        viewModel.VisibleBooks.Should().ContainSingle()
            .Which.Book.Formats.Should().BeEquivalentTo([EbookFormat.Epub, EbookFormat.Pdf]);
    }

    [Fact]
    public async Task Refresh_reports_library_view_performance_phases()
    {
        var reporter = new CapturingLibraryPerformanceReporter();
        var first = CreateBook("Dune", ["Frank Herbert"]);
        var second = CreateBook("The Hobbit", ["J.R.R. Tolkien"]);
        var viewModel = CreateViewModel([first, second], performanceReporter: reporter);

        await viewModel.RefreshAsync();

        var snapshot = reporter.Snapshots.Should()
            .ContainSingle(item => item.Operation == "ApplyFilter")
            .Which;
        snapshot.BookCount.Should().Be(2);
        snapshot.VisibleBookCount.Should().Be(2);
        snapshot.Phases.Keys.Should().Contain(["filter", "materialize-sort", "visible-reset", "grouping", "selection"]);
    }

    [Fact]
    public async Task Add_grouping_reports_grouping_performance_phases()
    {
        var reporter = new CapturingLibraryPerformanceReporter();
        var first = CreateBook("Dune", ["Frank Herbert"]);
        var second = CreateBook("Children of Dune", ["Frank Herbert"]);
        var viewModel = CreateViewModel([first, second], performanceReporter: reporter);
        await viewModel.RefreshAsync();
        reporter.Snapshots.Clear();

        viewModel.SelectedGroupOptionToAdd = LibraryGroupOption.Author;
        await viewModel.AddGroupingCommand.ExecuteAsync(null);

        var snapshot = reporter.Snapshots.Should()
            .ContainSingle(item => item.Operation == "AddGrouping")
            .Which;
        snapshot.BookCount.Should().Be(2);
        snapshot.VisibleBookCount.Should().Be(2);
        snapshot.Groupings.Should().Equal(LibraryGroupOption.Author);
        snapshot.Phases.Keys.Should().Contain(["active-groups", "snapshot", "grouping", "settings-schedule"]);
    }

    private static LibraryViewModel CreateViewModel(
        IReadOnlyList<Book> books,
        IUserInteractionService? userInteraction = null,
        LibraryService? libraryService = null,
        CurrentLibrary? currentLibrary = null,
        ILibraryDatabaseInitializer? databaseInitializer = null,
        IAppSettingsStore? settingsStore = null,
        IBookRepository? repository = null,
        BookDetailsViewModel? details = null,
        IImportAgent? importAgent = null,
        IImportRepository? importRepository = null,
        ICustomMetadataRepository? customMetadataRepository = null,
        IDuplicateExclusionRepository? duplicateExclusionRepository = null,
        DirectoryScanner? directoryScanner = null,
        ILibraryPerformanceReporter? performanceReporter = null,
        Func<string, string>? localize = null)
    {
        repository ??= new StaticBookRepository(books);
        var bookService = new BookService(
            repository,
            new NoopLibraryFileStore(),
            new NoopMetadataAdapterResolver());
        details ??= new BookDetailsViewModel(bookService);
        return new LibraryViewModel(
            repository,
            new BookSearchService(),
            details,
            userInteraction ?? new ScriptedUserInteractionService(),
            bookService: bookService,
            libraryService: libraryService,
            currentLibrary: currentLibrary,
            databaseInitializer: databaseInitializer,
            directoryScanner: directoryScanner,
            settingsStore: settingsStore,
            importAgent: importAgent,
            importRepository: importRepository,
            customMetadataRepository: customMetadataRepository,
            duplicateExclusionRepository: duplicateExclusionRepository,
            performanceReporter: performanceReporter,
            localize: localize);
    }

    private static Book CreateBook(
        string title,
        IReadOnlyList<string> authors,
        IReadOnlyList<string>? tags = null,
        string? language = null,
        string? series = null,
        ReadingStatus readingStatus = ReadingStatus.Unread,
        IReadOnlyList<EbookFormat>? formats = null,
        Guid? id = null,
        byte[]? coverBytes = null,
        decimal? seriesNumber = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Book(
            id ?? Guid.NewGuid(),
            new BookMetadata(title, authors, Language: language, Tags: tags, Series: series, SeriesNumber: seriesNumber, CoverBytes: coverBytes),
            readingStatus,
            null,
            now,
            now)
        {
            Formats = formats ?? []
        };
    }

    private static BookRowViewModel CreateRow(string title) =>
        new(CreateBook(title, ["Author"]));

    private static IReadOnlyList<LibraryColumnOption> DefaultDetailedColumnOptions() =>
    [
        LibraryColumnOption.Cover,
        LibraryColumnOption.Title,
        LibraryColumnOption.Authors,
        LibraryColumnOption.Format,
        LibraryColumnOption.Series,
        LibraryColumnOption.SeriesNumber,
        LibraryColumnOption.Status,
        LibraryColumnOption.Language,
        LibraryColumnOption.Publisher,
        LibraryColumnOption.PublicationDate,
        LibraryColumnOption.Tags,
        LibraryColumnOption.Isbn,
        LibraryColumnOption.Description,
        LibraryColumnOption.DateAdded,
        LibraryColumnOption.LastModified,
        LibraryColumnOption.EReader
    ];

    private sealed class InMemoryCustomMetadataRepository : ICustomMetadataRepository
    {
        private readonly List<CustomMetadataFieldDefinition> definitions = [];
        private readonly Dictionary<(Guid BookId, Guid FieldId), CustomMetadataValue> values = [];

        public int GetValuesCalls { get; private set; }
        public int GetValuesForBooksCalls { get; private set; }
        public IReadOnlyList<CustomMetadataValue> ValuesSnapshot => values.Values.ToArray();

        public void ResetCallCounts()
        {
            GetValuesCalls = 0;
            GetValuesForBooksCalls = 0;
        }

        public CustomMetadataFieldDefinition AddDefinition(string name, CustomMetadataFieldType type)
        {
            var now = DateTimeOffset.UtcNow;
            var definition = new CustomMetadataFieldDefinition(
                Guid.NewGuid(),
                name.ToLowerInvariant().Replace(' ', '-'),
                name,
                type,
                [],
                definitions.Count,
                now,
                now);
            definitions.Add(definition);
            return definition;
        }

        public void SetValue(CustomMetadataValue value) =>
            values[(value.BookId, value.FieldId)] = value;

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
            CancellationToken cancellationToken)
        {
            var index = definitions.FindIndex(definition => definition.Id == fieldId);
            if (index >= 0)
            {
                definitions[index] = definitions[index] with { Options = options.ToArray() };
            }

            return Task.CompletedTask;
        }

        public Task DeleteDefinitionAsync(Guid fieldId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<CustomMetadataValue>> GetValuesAsync(Guid bookId, CancellationToken cancellationToken)
        {
            GetValuesCalls++;
            return Task.FromResult<IReadOnlyList<CustomMetadataValue>>(
                values.Values.Where(value => value.BookId == bookId).ToList());
        }

        public Task<IReadOnlyList<CustomMetadataValue>> GetValuesForBooksAsync(
            IReadOnlyCollection<Guid> bookIds,
            CancellationToken cancellationToken)
        {
            GetValuesForBooksCalls++;
            return Task.FromResult<IReadOnlyList<CustomMetadataValue>>(
                values.Values.Where(value => bookIds.Contains(value.BookId)).ToList());
        }

        public Task SetValueAsync(CustomMetadataValue value, CancellationToken cancellationToken)
        {
            SetValue(value);
            return Task.CompletedTask;
        }

        public Task DeleteValueAsync(Guid bookId, Guid fieldId, CancellationToken cancellationToken)
        {
            values.Remove((bookId, fieldId));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> CleanupFilterValueAsync(
            Guid fieldId,
            string oldValue,
            string? replacementValue,
            bool remove,
            CancellationToken cancellationToken)
        {
            var definitionIndex = definitions.FindIndex(definition => definition.Id == fieldId);
            if (definitionIndex < 0)
            {
                return Task.FromResult<IReadOnlyList<Guid>>([]);
            }

            var definition = definitions[definitionIndex];
            if (definition.Type is not (CustomMetadataFieldType.Text or CustomMetadataFieldType.SingleSelect or CustomMetadataFieldType.MultiSelect))
            {
                return Task.FromResult<IReadOnlyList<Guid>>([]);
            }

            var changedBookIds = new List<Guid>();
            foreach (var value in values.Values.Where(value => value.FieldId == fieldId).ToArray())
            {
                var updatedText = definition.Type == CustomMetadataFieldType.MultiSelect
                    ? EditListValue(value.TextValue, oldValue, replacementValue, remove)
                    : EditScalarValue(value.TextValue, oldValue, replacementValue, remove);
                if (string.Equals(value.TextValue, updatedText, StringComparison.Ordinal))
                {
                    continue;
                }

                changedBookIds.Add(value.BookId);
                if (string.IsNullOrWhiteSpace(updatedText))
                {
                    values.Remove((value.BookId, value.FieldId));
                    continue;
                }

                values[(value.BookId, value.FieldId)] = value with
                {
                    TextValue = updatedText,
                    UpdatedUtc = DateTimeOffset.UtcNow
                };
            }

            if (!remove &&
                !string.IsNullOrWhiteSpace(replacementValue) &&
                definition.Type is CustomMetadataFieldType.SingleSelect or CustomMetadataFieldType.MultiSelect)
            {
                definitions[definitionIndex] = definition with
                {
                    Options = RenameOption(definition.Options, oldValue, replacementValue.Trim())
                };
            }

            return Task.FromResult<IReadOnlyList<Guid>>(changedBookIds.Distinct().ToArray());
        }

        private static string? EditScalarValue(
            string? value,
            string oldValue,
            string? replacementValue,
            bool remove) =>
            string.Equals(value?.Trim(), oldValue.Trim(), StringComparison.OrdinalIgnoreCase)
                ? remove ? null : replacementValue?.Trim()
                : value;

        private static string? EditListValue(
            string? value,
            string oldValue,
            string? replacementValue,
            bool remove)
        {
            var changed = false;
            var values = new List<string>();
            foreach (var item in SplitList(value))
            {
                if (string.Equals(item, oldValue, StringComparison.OrdinalIgnoreCase))
                {
                    changed = true;
                    if (!remove && !string.IsNullOrWhiteSpace(replacementValue))
                    {
                        values.Add(replacementValue.Trim());
                    }
                }
                else
                {
                    values.Add(item);
                }
            }

            if (!changed)
            {
                return value;
            }

            var distinctValues = values
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return distinctValues.Length == 0
                ? null
                : string.Join("; ", distinctValues);
        }

        private static IReadOnlyList<string> RenameOption(
            IReadOnlyList<string> options,
            string oldValue,
            string replacementValue)
        {
            var changed = false;
            var updated = new List<string>();
            foreach (var option in options)
            {
                if (string.Equals(option, oldValue, StringComparison.OrdinalIgnoreCase))
                {
                    changed = true;
                    updated.Add(replacementValue);
                }
                else
                {
                    updated.Add(option);
                }
            }

            if (!changed && !updated.Contains(replacementValue, StringComparer.OrdinalIgnoreCase))
            {
                updated.Add(replacementValue);
            }

            return updated
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Select(option => option.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<string> SplitList(string? value) =>
            (value ?? string.Empty).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private sealed class CapturingLibraryPerformanceReporter : ILibraryPerformanceReporter
    {
        public List<LibraryPerformanceSnapshot> Snapshots { get; } = [];

        public void Report(LibraryPerformanceSnapshot snapshot) => Snapshots.Add(snapshot);
    }

    private static CurrentLibrary CreateActiveLibrary()
    {
        var currentLibrary = new CurrentLibrary();
        currentLibrary.Set(new LibraryDescriptor("Test", Path.GetTempPath(), DateTimeOffset.UtcNow));
        return currentLibrary;
    }

    private static CurrentLibrary CreateActiveLibrary(string directoryPath)
    {
        var currentLibrary = new CurrentLibrary();
        currentLibrary.Set(new LibraryDescriptor(
            Path.GetFileName(Path.TrimEndingDirectorySeparator(directoryPath)),
            directoryPath,
            DateTimeOffset.UtcNow));
        return currentLibrary;
    }

    private class StaticBookRepository(IReadOnlyList<Book> books) : IBookRepository
    {
        protected readonly List<Book> Books = [.. books];

        public IReadOnlyList<Book> BooksSnapshot => [.. Books];
        public int UpdateCalls { get; private set; }
        public int GetCalls { get; protected set; }

        public virtual Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([.. Books]);
        public virtual Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(Books.SingleOrDefault(book => book.Id == id));
        }
        public Task<bool> HasHashAsync(string sha256, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> HasNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<Book?> FindByNormalizedTitleAndAuthorAsync(string title, IReadOnlyList<string> authors, CancellationToken cancellationToken) => Task.FromResult<Book?>(null);
        public Task<IReadOnlyList<Book>> FindByNormalizedTitleAsync(string title, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Book>>([]);
        public Task AddAsync(Book book, BookFile file, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddFileAsync(BookFile file, CancellationToken cancellationToken) => Task.CompletedTask;
        public virtual Task AttachFilesToBookAsync(Guid sourceBookId, Guid targetBookId, CancellationToken cancellationToken) => Task.CompletedTask;
        public virtual Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            var index = Books.FindIndex(existing => existing.Id == book.Id);
            if (index >= 0)
            {
                Books[index] = book;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Books.RemoveAll(book => book.Id == id);
            return Task.CompletedTask;
        }
        public Task<BookFileDeleteRepositoryResult> DeleteFileAsync(Guid bookId, Guid fileId, CancellationToken cancellationToken) =>
            Task.FromResult(new BookFileDeleteRepositoryResult(BookFileDeleteRepositoryStatus.NotFound));
        public Task<IReadOnlyList<BookFile>> ListFilesAsync(Guid bookId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<BookFile>>([]);
        public Task UpdateFileWriteBackAsync(Guid fileId, MetadataWriteResult result, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class BulkScalarMetadataRepository(IReadOnlyList<Book> books)
        : StaticBookRepository(books), IBookBulkMetadataRepository
    {
        public int BulkUpdateCalls { get; private set; }
        public int BulkListUpdateCalls { get; protected set; }

        public virtual Task<int> UpdateScalarMetadataAsync(
            IReadOnlyCollection<Guid> bookIds,
            BookScalarMetadataField field,
            string? value,
            CancellationToken cancellationToken)
        {
            BulkUpdateCalls++;
            var idSet = bookIds.ToHashSet();
            var updated = 0;
            for (var index = 0; index < Books.Count; index++)
            {
                var book = Books[index];
                if (!idSet.Contains(book.Id))
                {
                    continue;
                }

                Books[index] = book with
                {
                    Metadata = new BookMetadata(
                        book.Metadata.Title,
                        book.Metadata.Authors,
                        book.Metadata.Description,
                        field == BookScalarMetadataField.Language ? value : book.Metadata.Language,
                        book.Metadata.Publisher,
                        book.Metadata.PublicationDate,
                        book.Metadata.Tags,
                        field == BookScalarMetadataField.Series ? value : book.Metadata.Series,
                        book.Metadata.SeriesNumber,
                        book.Metadata.Isbn,
                        book.Metadata.CoverBytes)
                };
                updated++;
            }

            return Task.FromResult(updated);
        }

        public virtual Task<int> UpdateListMetadataAsync(
            IReadOnlyCollection<Book> books,
            BookListMetadataField field,
            CancellationToken cancellationToken)
        {
            BulkListUpdateCalls++;
            var updatesById = books.ToDictionary(book => book.Id);
            var updated = 0;
            for (var index = 0; index < Books.Count; index++)
            {
                if (!updatesById.TryGetValue(Books[index].Id, out var updatedBook))
                {
                    continue;
                }

                Books[index] = updatedBook;
                updated++;
            }

            return Task.FromResult(updated);
        }
    }

    private sealed class BlockingBulkScalarMetadataRepository(IReadOnlyList<Book> books)
        : BulkScalarMetadataRepository(books)
    {
        private readonly TaskCompletionSource releaseBeforeBulkUpdate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource BeforeBulkUpdate { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource BulkUpdateStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task<int> UpdateScalarMetadataAsync(
            IReadOnlyCollection<Guid> bookIds,
            BookScalarMetadataField field,
            string? value,
            CancellationToken cancellationToken)
        {
            BeforeBulkUpdate.TrySetResult();
            await releaseBeforeBulkUpdate.Task.WaitAsync(cancellationToken);
            BulkUpdateStarted.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return await base.UpdateScalarMetadataAsync(bookIds, field, value, cancellationToken);
        }

        public void ReleaseBeforeBulkUpdate() => releaseBeforeBulkUpdate.TrySetResult();
        public void ReleaseBulkUpdate() => release.TrySetResult();
    }

    private class RefreshingBookRepository : StaticBookRepository
    {
        private readonly IReadOnlyList<IReadOnlyList<Book>> refreshBooks;
        public int ListCalls { get; private set; }
        public Guid? AttachedSourceBookId { get; private set; }
        public Guid? AttachedTargetBookId { get; private set; }

        public RefreshingBookRepository(
            IReadOnlyList<Book> firstRefreshBooks,
            IReadOnlyList<Book> laterRefreshBooks,
            params IReadOnlyList<Book>[] remainingRefreshBooks) : base(firstRefreshBooks)
        {
            refreshBooks = new[] { firstRefreshBooks, laterRefreshBooks }
                .Concat(remainingRefreshBooks)
                .ToList()
                .AsReadOnly();
        }

        public override Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken)
        {
            ListCalls++;
            var index = Math.Min(ListCalls - 1, refreshBooks.Count - 1);
            return Task.FromResult(refreshBooks[index]);
        }

        public override Task AttachFilesToBookAsync(
            Guid sourceBookId,
            Guid targetBookId,
            CancellationToken cancellationToken)
        {
            AttachedSourceBookId = sourceBookId;
            AttachedTargetBookId = targetBookId;
            return Task.CompletedTask;
        }
    }

    private sealed class MissingBookOnAttachRepository(
        IReadOnlyList<Book> firstRefreshBooks,
        IReadOnlyList<Book> laterRefreshBooks,
        params IReadOnlyList<Book>[] remainingRefreshBooks)
        : RefreshingBookRepository(firstRefreshBooks, laterRefreshBooks, remainingRefreshBooks)
    {
        public override Task AttachFilesToBookAsync(
            Guid sourceBookId,
            Guid targetBookId,
            CancellationToken cancellationToken) =>
            throw new KeyNotFoundException($"Source book '{sourceBookId}' does not exist.");
    }

    private sealed class ConflictingBookRepository(
        IReadOnlyList<Book> books,
        Guid conflictingBookId) : StaticBookRepository(books)
    {
        public override Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            if (book.Id == conflictingBookId)
            {
                throw new BookConflictException();
            }

            return base.UpdateAsync(book, cancellationToken);
        }
    }

    private sealed class ConflictingBulkListMetadataRepository(
        IReadOnlyList<Book> books,
        Guid conflictingBookId) : BulkScalarMetadataRepository(books)
    {
        private bool bulkConflictThrown;

        public override Task<int> UpdateListMetadataAsync(
            IReadOnlyCollection<Book> books,
            BookListMetadataField field,
            CancellationToken cancellationToken)
        {
            BulkListUpdateCalls++;
            if (!bulkConflictThrown)
            {
                bulkConflictThrown = true;
                throw new BookConflictException();
            }

            return base.UpdateListMetadataAsync(books, field, cancellationToken);
        }

        public override Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            if (book.Id == conflictingBookId)
            {
                throw new BookConflictException();
            }

            return base.UpdateAsync(book, cancellationToken);
        }
    }

    private sealed class FullBookOnGetConflictingBulkListMetadataRepository(
        IReadOnlyList<Book> listBooks,
        Book fullBook) : BulkScalarMetadataRepository(listBooks)
    {
        public Book? UpdatedBook { get; private set; }

        public override Task<int> UpdateListMetadataAsync(
            IReadOnlyCollection<Book> books,
            BookListMetadataField field,
            CancellationToken cancellationToken)
        {
            BulkListUpdateCalls++;
            throw new BookConflictException();
        }

        public override Task<Book?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(id == fullBook.Id ? fullBook : null);
        }

        public override Task UpdateAsync(Book book, CancellationToken cancellationToken)
        {
            UpdatedBook = book;
            return base.UpdateAsync(book, cancellationToken);
        }
    }

    private sealed class BlockingBookRepository : StaticBookRepository
    {
        private readonly TaskCompletionSource<IReadOnlyList<Book>> release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingBookRepository() : base([])
        {
        }

        public TaskCompletionSource ListStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken)
        {
            ListStarted.TrySetResult();
            return release.Task;
        }

        public void Release(IReadOnlyList<Book> books) => release.TrySetResult(books);
    }

    private sealed class ThrowingBookRepository : StaticBookRepository
    {
        public ThrowingBookRepository() : base([])
        {
        }

        public override Task<IReadOnlyList<Book>> ListAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The repository should not be called when the library folder is missing.");
    }

    private sealed class BlockingPagedBookRepository(IReadOnlyList<Book> books)
        : StaticBookRepository(books), IBookPagedRepository
    {
        private readonly TaskCompletionSource releaseRemainingPages =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int pageCalls;

        public TaskCompletionSource FirstPageLoaded { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<int> CountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Books.Count);

        public async Task<IReadOnlyList<Book>> ListPageAsync(
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var page = Books
                .OrderBy(book => book.Metadata.Title)
                .ThenBy(book => book.Id)
                .Skip(skip)
                .Take(take)
                .ToList();
            if (Interlocked.Increment(ref pageCalls) == 1)
            {
                FirstPageLoaded.TrySetResult();
                return page;
            }

            await releaseRemainingPages.Task.WaitAsync(cancellationToken);
            return page;
        }

        public void ReleaseRemainingPages() => releaseRemainingPages.TrySetResult();
    }

    private sealed class BlockingAppSettingsStore : IAppSettingsStore
    {
        private readonly object syncRoot = new();
        private TaskCompletionSource? blockedLoadRelease;
        private bool blockNextLoad;

        public AppSettings Settings { get; private set; } = new(
            null,
            "en-US",
            "Light",
            "Detailed",
            true,
            true,
            AuthorSortStrategy.DisplayName,
            true,
            true,
            new DuplicateMergeDefaultSettings(),
            null,
            null,
            null,
            null);

        public List<LibraryDescriptor> Libraries { get; private set; } = [];

        public TaskCompletionSource BlockedLoadStarted { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void BlockNextLoad()
        {
            lock (syncRoot)
            {
                blockNextLoad = true;
                blockedLoadRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                BlockedLoadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public void ReleaseBlockedLoad()
        {
            TaskCompletionSource? release;
            lock (syncRoot)
            {
                release = blockedLoadRelease;
            }

            release?.TrySetResult();
        }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource? release = null;
            lock (syncRoot)
            {
                if (blockNextLoad)
                {
                    blockNextLoad = false;
                    release = blockedLoadRelease;
                }
            }

            return release is null
                ? Task.FromResult(Settings)
                : LoadAfterReleaseAsync(release, cancellationToken);
        }

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Settings = settings;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LibraryDescriptor>> ListLibrariesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<LibraryDescriptor>>(Libraries);
        }

        public Task SaveLibrariesAsync(IReadOnlyList<LibraryDescriptor> libraries, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Libraries = [.. libraries];
            return Task.CompletedTask;
        }

        private async Task<AppSettings> LoadAfterReleaseAsync(
            TaskCompletionSource release,
            CancellationToken cancellationToken)
        {
            BlockedLoadStarted.TrySetResult();
            await release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return Settings;
        }
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

    private sealed class ScriptedUserInteractionService : IUserInteractionService
    {
        public string? LibraryDirectory { get; init; }
        public string? ScanFolder { get; init; }
        public string? PromptTextResult { get; init; }
        public bool ConfirmDeleteViewResult { get; init; } = true;
        public bool ConfirmMetadataValueRemovalResult { get; init; }
        public bool ConfirmLanguageNormalizationResult { get; init; }
        public int? ConfirmLanguageNormalizationAffectedCount { get; private set; }
        public Guid? SelectedImportRunId { get; init; }
        public MetadataMultiEditResult? MetadataMultiEditResult { get; init; }
        public Guid? MetadataQualityDashboardResult { get; init; }
        public IReadOnlyList<string> MetadataMultiEditCustomFieldNames { get; private set; } = [];
        public string? LastMessageTitle { get; private set; }
        public string? LastMessageText { get; private set; }
        public Func<DuplicateCandidatesViewModel, CancellationToken, Task>? OnShowDuplicateCandidatesAsync { get; init; }
        public int PickBookFilesCalls { get; private set; }
        public int PickScanFolderCalls { get; private set; }
        public int? MetadataMultiEditSelectedBookCount { get; private set; }
        public DuplicateCandidatesViewModel? DuplicateCandidates { get; private set; }
        public DuplicateExclusionsViewModel? DuplicateExclusions { get; private set; }
        public ImportHistoryViewModel? ImportHistory { get; private set; }
        public ImportResultViewModel? ShownImportResult { get; private set; }
        public MetadataQualityDashboardViewModel? MetadataQualityDashboard { get; private set; }

        public Task<IReadOnlyList<string>> PickBookFilesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(RecordPickBookFiles());

        public Task<string?> PickScanFolderAsync(CancellationToken cancellationToken)
        {
            PickScanFolderCalls++;
            return Task.FromResult(ScanFolder);
        }
        public Task<string?> PickLibraryDirectoryAsync(string title, CancellationToken cancellationToken) =>
            Task.FromResult(LibraryDirectory);

        public Task<bool> ConfirmDeleteAsync(string title, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> ConfirmDeleteViewAsync(string viewName, CancellationToken cancellationToken) =>
            Task.FromResult(ConfirmDeleteViewResult);
        public Task<string?> PromptTextAsync(
            string title,
            string message,
            string initialValue,
            CancellationToken cancellationToken) =>
            Task.FromResult(PromptTextResult);

        public Task<bool> ConfirmMetadataValueRemovalAsync(
            string value,
            int affectedBookCount,
            CancellationToken cancellationToken) =>
            Task.FromResult(ConfirmMetadataValueRemovalResult);

        public Task<bool> ConfirmLanguageNormalizationAsync(
            int affectedBookCount,
            CancellationToken cancellationToken)
        {
            ConfirmLanguageNormalizationAffectedCount = affectedBookCount;
            return Task.FromResult(ConfirmLanguageNormalizationResult);
        }

        public Task ShowMessageAsync(
            string title,
            string message,
            CancellationToken cancellationToken)
        {
            LastMessageTitle = title;
            LastMessageText = message;
            return Task.CompletedTask;
        }

        public Task ShowImportResultAsync(ImportResultViewModel result, CancellationToken cancellationToken)
        {
            ShownImportResult = result;
            return Task.CompletedTask;
        }

        public Task<Guid?> PickImportRunAsync(ImportHistoryViewModel history, CancellationToken cancellationToken)
        {
            ImportHistory = history;
            return Task.FromResult(SelectedImportRunId);
        }

        public async Task ShowDuplicateCandidatesAsync(DuplicateCandidatesViewModel candidates, CancellationToken cancellationToken)
        {
            DuplicateCandidates = candidates;
            if (OnShowDuplicateCandidatesAsync is not null)
            {
                await OnShowDuplicateCandidatesAsync(candidates, cancellationToken);
            }
        }

        public Task ShowDuplicateExclusionsAsync(DuplicateExclusionsViewModel exclusions, CancellationToken cancellationToken)
        {
            DuplicateExclusions = exclusions;
            return Task.CompletedTask;
        }

        public Task<Guid?> ShowMetadataQualityDashboardAsync(
            MetadataQualityDashboardViewModel dashboard,
            CancellationToken cancellationToken)
        {
            MetadataQualityDashboard = dashboard;
            return Task.FromResult(MetadataQualityDashboardResult);
        }

        public Task<MetadataMultiEditResult?> ShowMetadataMultiEditAsync(
            MetadataMultiEditViewModel edit,
            CancellationToken cancellationToken)
        {
            MetadataMultiEditSelectedBookCount = edit.SelectedBookCount;
            MetadataMultiEditCustomFieldNames = edit.CustomFields.Select(field => field.Name).ToArray();
            return Task.FromResult(MetadataMultiEditResult);
        }

        private IReadOnlyList<string> RecordPickBookFiles()
        {
            PickBookFilesCalls++;
            return [];
        }
    }

    private sealed class StaticImportRepository(
        IReadOnlyList<ImportRunSummary> summaries,
        ImportRunResult? run = null) : IImportRepository
    {
        public Task<Guid> StartRunAsync(DateTimeOffset startedUtc, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task<Guid> StartRunAsync(
            DateTimeOffset startedUtc,
            ImportRunContext? context,
            CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task RecordItemAsync(
            Guid runId,
            int sequence,
            string sourceDisplayName,
            ImportOutcome outcome,
            string message,
            Guid? bookId,
            CancellationToken cancellationToken,
            ImportItemDiagnostics? diagnostics = null,
            ImportItemSuggestion? suggestion = null) =>
            Task.CompletedTask;

        public Task CompleteRunAsync(Guid runId, DateTimeOffset completedUtc, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<ImportRunResult?> GetAsync(Guid runId, CancellationToken cancellationToken) =>
            Task.FromResult(run?.Id == runId ? run : null);

        public Task<IReadOnlyList<ImportRunSummary>> ListRecentAsync(int maxCount, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ImportRunSummary>>(summaries.Take(maxCount).ToList());
    }

    private sealed class RecordingLibraryDatabaseInitializer : ILibraryDatabaseInitializer
    {
        public List<LibraryDescriptor> InitializedLibraries { get; } = [];

        public Task InitializeAsync(LibraryDescriptor library, CancellationToken cancellationToken)
        {
            InitializedLibraries.Add(library);
            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedImportAgent : IImportAgent
    {
        private Func<ImportProgress, Task>? onProgress;

        public event EventHandler<ImportBatchResult>? Completed;

        public ImportJobViewModel Job { get; } = new();

        public bool IsActive { get; private set; }

        public bool StartScanningCalled { get; private set; }
        public IReadOnlyList<string> StartedSourcePaths { get; private set; } = [];
        public ImportRunContext? ImportContext { get; private set; }

        public void StartScanning()
        {
            StartScanningCalled = true;
            Job.StartScanning();
        }

        public Task StartImportAsync(
            IReadOnlyList<string> sourcePaths,
            Func<ImportProgress, Task> onProgress,
            CancellationToken cancellationToken,
            ImportRunContext? context = null)
        {
            IsActive = true;
            StartedSourcePaths = sourcePaths;
            ImportContext = context;
            this.onProgress = onProgress;
            Job.StartImport(Guid.NewGuid(), sourcePaths.Count);
            return Task.CompletedTask;
        }

        public void CancelActiveJob() => IsActive = false;

        public async Task ReportProgressAsync(int processedCount)
        {
            var progress = new ImportProgress(
                Guid.NewGuid(),
                Math.Max(processedCount, 1),
                processedCount,
                processedCount,
                0,
                0,
                0,
                new ImportItemResult("book.epub", ImportOutcome.Added, "added"));
            Job.ApplyProgress(progress);
            if (onProgress is not null)
            {
                await onProgress(progress);
            }
        }

        public async Task CompleteAsync(ImportBatchResult result)
        {
            IsActive = false;
            Job.Complete(result);
            Completed?.Invoke(this, result);
            await Task.CompletedTask;
        }
    }
}
