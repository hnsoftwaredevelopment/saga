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

        await viewModel.SaveAsync();

        var settings = await store.LoadAsync(default);
        settings.Should().Be(new AppSettings("C:\\ELibrary", "nl-NL", "Dark", "List", false, false));
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
}
