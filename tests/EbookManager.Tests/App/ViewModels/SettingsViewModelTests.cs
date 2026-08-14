using EbookManager.Domain.Abstractions;
using EbookManager.Domain.CustomMetadata;
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
    public void SelectableDuplicateMergeDefaultActions_include_supported_merge_actions()
    {
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore());

        viewModel.SelectableDuplicateMergeDefaultActions
            .Select(option => option.Value)
            .Should()
            .Equal(DuplicateMergeDefaultAction.NoAction, DuplicateMergeDefaultAction.Copy, DuplicateMergeDefaultAction.Merge);
    }

    [Fact]
    public void SelectableCustomMetadataFieldTypes_include_supported_foundation_types()
    {
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore());

        viewModel.SelectableCustomMetadataFieldTypes
            .Select(option => option.Value)
            .Should()
            .Equal(
                CustomMetadataFieldType.Text,
                CustomMetadataFieldType.Number,
                CustomMetadataFieldType.Date,
                CustomMetadataFieldType.Boolean,
                CustomMetadataFieldType.SingleSelect,
                CustomMetadataFieldType.MultiSelect);
    }

    [Fact]
    public async Task Load_exposes_custom_metadata_field_definitions()
    {
        var repository = new InMemoryCustomMetadataRepository();
        await repository.AddDefinitionAsync("Eigen rating", CustomMetadataFieldType.Number, default);
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore(), repository);

        await viewModel.LoadAsync();

        viewModel.CustomMetadataFields.Should().ContainSingle(field =>
            field.Name == "Eigen rating" &&
            field.Key == "eigen-rating" &&
            field.Type == CustomMetadataFieldType.Number);
    }

    [Fact]
    public async Task AddRenameAndDeleteCustomMetadataField_update_visible_definitions()
    {
        var repository = new InMemoryCustomMetadataRepository();
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore(), repository);
        await viewModel.LoadAsync();

        viewModel.NewCustomMetadataFieldName = "Leesprioriteit";
        viewModel.NewCustomMetadataFieldType = CustomMetadataFieldType.SingleSelect;
        await viewModel.AddCustomMetadataFieldCommand.ExecuteAsync(null);

        viewModel.CustomMetadataFields.Should().ContainSingle(field =>
            field.Name == "Leesprioriteit" &&
            field.Type == CustomMetadataFieldType.SingleSelect);
        viewModel.CustomMetadataStatusMessage.Should().Be("CustomMetadataFieldAdded");

        viewModel.CustomMetadataFieldName = "Prioriteit";
        await viewModel.RenameCustomMetadataFieldCommand.ExecuteAsync(null);

        viewModel.CustomMetadataFields.Should().ContainSingle(field => field.Name == "Prioriteit");
        viewModel.CustomMetadataStatusMessage.Should().Be("CustomMetadataFieldRenamed");

        await viewModel.DeleteCustomMetadataFieldCommand.ExecuteAsync(null);

        viewModel.CustomMetadataFields.Should().BeEmpty();
        viewModel.CustomMetadataStatusMessage.Should().Be("CustomMetadataFieldDeleted");
    }

    [Fact]
    public async Task SaveCustomMetadataOptions_updates_selected_select_field_options()
    {
        var repository = new InMemoryCustomMetadataRepository();
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore(), repository);
        await viewModel.LoadAsync();
        viewModel.NewCustomMetadataFieldName = "Leesprioriteit";
        viewModel.NewCustomMetadataFieldType = CustomMetadataFieldType.SingleSelect;
        await viewModel.AddCustomMetadataFieldCommand.ExecuteAsync(null);

        viewModel.CustomMetadataOptionsText = "Hoog\r\nNormaal\r\n\r\nhoog\r\nLaag";
        await viewModel.SaveCustomMetadataOptionsCommand.ExecuteAsync(null);

        viewModel.SelectedCustomMetadataField.Should().NotBeNull();
        viewModel.SelectedCustomMetadataField!.Options.Should().Equal("Hoog", "Normaal", "Laag");
        viewModel.CustomMetadataStatusMessage.Should().Be("CustomMetadataOptionsSaved");
    }

    [Fact]
    public async Task SaveCustomMetadataOptions_rejects_semicolon_options()
    {
        var repository = new InMemoryCustomMetadataRepository();
        var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore(), repository);
        await viewModel.LoadAsync();
        viewModel.NewCustomMetadataFieldName = "Genres";
        viewModel.NewCustomMetadataFieldType = CustomMetadataFieldType.MultiSelect;
        await viewModel.AddCustomMetadataFieldCommand.ExecuteAsync(null);

        viewModel.CustomMetadataOptionsText = "Deel 1; deel 2";
        await viewModel.SaveCustomMetadataOptionsCommand.ExecuteAsync(null);

        viewModel.SelectedCustomMetadataField.Should().NotBeNull();
        viewModel.SelectedCustomMetadataField!.Options.Should().BeEmpty();
        viewModel.CustomMetadataStatusMessage.Should().Be("CustomMetadataOptionsSemicolonNotAllowed");
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
    public async Task Save_can_persist_custom_view_id_as_default_view()
    {
        var store = new InMemoryAppSettingsStore();
        var viewModel = new SettingsViewModel(store);
        await viewModel.LoadAsync();

        viewModel.DefaultView = "favoriete-thrillers";
        await viewModel.SaveAsync();

        var settings = await store.LoadAsync(default);
        settings.DefaultView.Should().Be("favoriete-thrillers");
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

    [Fact]
    public async Task Save_preserves_unedited_duplicate_merge_defaults()
    {
        var store = new InMemoryAppSettingsStore();
        await store.SaveAsync(store.Settings with
        {
            DuplicateMergeDefaults = new DuplicateMergeDefaultSettings(
                Title: DuplicateMergeDefaultAction.Copy,
                Series: DuplicateMergeDefaultAction.Merge,
                SeriesNumber: DuplicateMergeDefaultAction.Copy,
                PublicationDate: DuplicateMergeDefaultAction.Copy,
                Isbn: DuplicateMergeDefaultAction.Copy)
        }, default);
        var viewModel = new SettingsViewModel(store);
        await viewModel.LoadAsync();

        viewModel.MergeDefaultAuthors = DuplicateMergeDefaultAction.Copy;

        await viewModel.SaveAsync();

        var settings = await store.LoadAsync(default);
        settings.DuplicateMergeDefaults.Should().Be(new DuplicateMergeDefaultSettings(
            Title: DuplicateMergeDefaultAction.Copy,
            Authors: DuplicateMergeDefaultAction.Copy,
            Series: DuplicateMergeDefaultAction.Merge,
            SeriesNumber: DuplicateMergeDefaultAction.Copy,
            PublicationDate: DuplicateMergeDefaultAction.Copy,
            Isbn: DuplicateMergeDefaultAction.Copy));
    }

    private sealed class InMemoryCustomMetadataRepository : ICustomMetadataRepository
    {
        private readonly List<CustomMetadataFieldDefinition> definitions = [];

        public Task<IReadOnlyList<CustomMetadataFieldDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomMetadataFieldDefinition>>(
                definitions.OrderBy(definition => definition.SortOrder).ThenBy(definition => definition.Name).ToList());

        public Task<CustomMetadataFieldDefinition> AddDefinitionAsync(
            string name,
            CustomMetadataFieldType type,
            CancellationToken cancellationToken)
        {
            var normalized = name.Trim();
            if (definitions.Any(definition => definition.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Duplicate field name.");
            }

            var now = DateTimeOffset.UtcNow;
            var definition = new CustomMetadataFieldDefinition(
                Guid.NewGuid(),
                normalized.ToLowerInvariant().Replace(' ', '-'),
                normalized,
                type,
                [],
                definitions.Count,
                now,
                now);
            definitions.Add(definition);
            return Task.FromResult(definition);
        }

        public Task RenameDefinitionAsync(Guid fieldId, string name, CancellationToken cancellationToken)
        {
            var index = definitions.FindIndex(definition => definition.Id == fieldId);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }

            var current = definitions[index];
            definitions[index] = current with { Name = name.Trim(), UpdatedUtc = DateTimeOffset.UtcNow };
            return Task.CompletedTask;
        }

        public Task UpdateDefinitionOptionsAsync(
            Guid fieldId,
            IReadOnlyList<string> options,
            CancellationToken cancellationToken)
        {
            var index = definitions.FindIndex(definition => definition.Id == fieldId);
            if (index < 0)
            {
                throw new KeyNotFoundException();
            }

            var current = definitions[index];
            if (!current.Options.SequenceEqual(options))
            {
                definitions[index] = current with { Options = options.ToArray(), UpdatedUtc = DateTimeOffset.UtcNow };
            }

            return Task.CompletedTask;
        }

        public Task DeleteDefinitionAsync(Guid fieldId, CancellationToken cancellationToken)
        {
            definitions.RemoveAll(definition => definition.Id == fieldId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CustomMetadataValue>> GetValuesAsync(Guid bookId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomMetadataValue>>([]);

        public Task<IReadOnlyList<CustomMetadataValue>> GetValuesForBooksAsync(
            IReadOnlyCollection<Guid> bookIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomMetadataValue>>([]);

        public Task SetValueAsync(CustomMetadataValue value, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteValueAsync(Guid bookId, Guid fieldId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
