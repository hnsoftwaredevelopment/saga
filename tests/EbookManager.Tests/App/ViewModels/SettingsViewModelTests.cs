using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Settings;
using EbookManager.Presentation.ViewModels;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void SelectableThemes_include_milestone_2_themes()
    {
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore());

        viewModel.SelectableThemes.Should().Equal("Light", "Dark", "Sepia", "Blue", "Red");
    }

    [Fact]
    public void SelectableCultures_include_all_supported_application_languages()
    {
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore());

        viewModel.SelectableCultures
            .Select(culture => culture.Value)
            .Should()
            .Equal("en-US", "nl-NL", "de-DE", "fr-FR", "es-ES", "it-IT");
    }

    [Fact]
    public void SelectableAuthorSortStrategies_include_display_and_last_name_options()
    {
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore());

        viewModel.SelectableAuthorSortStrategies
            .Select(option => option.Value)
            .Should()
            .Equal(AuthorSortStrategy.DisplayName, AuthorSortStrategy.LastNameFirst, AuthorSortStrategy.LastNameFirstDutchPrefixes);
    }

    [Fact]
    public async Task Save_preserves_last_library_path_while_updating_preferences()
    {
        var store = new InMemoryAppSettingsStore();
        await store.SaveAsync(new AppSettings("C:\\ELibrary", "en-US", "Light", "Detailed", true, true), default);
        var viewModel = new SettingsViewModel(store);
        await viewModel.LoadAsync();
        viewModel.Culture = "nl-NL";
        viewModel.Theme = "Dark";
        viewModel.DefaultView = "List";
        viewModel.ConfirmDelete = false;
        viewModel.IncludeScanSubdirectories = false;
        viewModel.DuplicateExactMatchesOnly = false;
        viewModel.EnableDiagnosticDetails = false;

        await viewModel.SaveAsync();

        var settings = await store.LoadAsync(default);
        settings.Should().Be(new AppSettings(
            "C:\\ELibrary",
            "nl-NL",
            "Dark",
            "List",
            false,
            false,
            AuthorSortStrategy.DisplayName,
            false,
            false,
            new DuplicateMergeDefaultSettings()));
    }

    [Fact]
    public async Task Save_preserves_last_library_path_while_updating_author_sort_strategy()
    {
        var store = new InMemoryAppSettingsStore();
        await store.SaveAsync(new AppSettings(
            "C:\\ELibrary",
            "en-US",
            "Light",
            "Detailed",
            true,
            true,
            AuthorSortStrategy.DisplayName), default);
        var viewModel = new SettingsViewModel(store);
        await viewModel.LoadAsync();

        viewModel.AuthorSortStrategy = AuthorSortStrategy.LastNameFirst;

        await viewModel.SaveAsync();

        var settings = await store.LoadAsync(default);
        settings.AuthorSortStrategy.Should().Be(AuthorSortStrategy.LastNameFirst);
        settings.LastLibraryPath.Should().Be("C:\\ELibrary");
    }

    [Fact]
    public async Task Load_exposes_duplicate_and_diagnostic_preferences()
    {
        var store = new InMemoryAppSettingsStore();
        await store.SaveAsync(store.Settings with
        {
            DuplicateExactMatchesOnly = false,
            EnableDiagnosticDetails = false
        }, default);
        var viewModel = new SettingsViewModel(store);

        await viewModel.LoadAsync();

        viewModel.DuplicateExactMatchesOnly.Should().BeFalse();
        viewModel.EnableDiagnosticDetails.Should().BeFalse();
    }

    [Fact]
    public async Task Save_persists_duplicate_merge_defaults()
    {
        var store = new InMemoryAppSettingsStore();
        var viewModel = new SettingsViewModel(store);
        await viewModel.LoadAsync();
        viewModel.MergeDefaultCover = DuplicateMergeDefaultAction.Copy;
        viewModel.MergeDefaultAuthors = DuplicateMergeDefaultAction.Merge;
        viewModel.MergeDefaultTags = DuplicateMergeDefaultAction.NoAction;
        viewModel.MergeDefaultDescription = DuplicateMergeDefaultAction.Merge;
        viewModel.MergeDefaultPublisher = DuplicateMergeDefaultAction.Copy;
        viewModel.MergeDefaultLanguage = DuplicateMergeDefaultAction.Copy;

        await viewModel.SaveAsync();

        var settings = await store.LoadAsync(default);
        settings.DuplicateMergeDefaults.Should().Be(new DuplicateMergeDefaultSettings(
            Cover: DuplicateMergeDefaultAction.Copy,
            Authors: DuplicateMergeDefaultAction.Merge,
            Tags: DuplicateMergeDefaultAction.NoAction,
            Description: DuplicateMergeDefaultAction.Merge,
            Publisher: DuplicateMergeDefaultAction.Copy,
            Language: DuplicateMergeDefaultAction.Copy));
    }
}
