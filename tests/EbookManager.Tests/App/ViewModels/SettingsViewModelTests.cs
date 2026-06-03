using EbookManager.Domain.Abstractions;
using EbookManager.Presentation.ViewModels;
using EbookManager.Tests.TestSupport;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task Save_preserves_last_library_path_while_updating_preferences()
    {
        var store = new InMemoryAppSettingsStore();
        await store.SaveAsync(new AppSettings("C:\\ELibrary", "en-US", "Light", "Detailed", true), default);
        var viewModel = new SettingsViewModel(store);
        await viewModel.LoadAsync();
        viewModel.Culture = "nl-NL";
        viewModel.Theme = "Dark";
        viewModel.DefaultView = "List";
        viewModel.ConfirmDelete = false;

        await viewModel.SaveAsync();

        var settings = await store.LoadAsync(default);
        settings.Should().Be(new AppSettings("C:\\ELibrary", "nl-NL", "Dark", "List", false));
    }
}
