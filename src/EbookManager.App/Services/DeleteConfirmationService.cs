using System.Globalization;
using EbookManager.Domain.Abstractions;

namespace EbookManager.App.Services;

public sealed record DeleteConfirmationResult(bool Confirmed, bool RememberAnswer);

public sealed class DeleteConfirmationService(
    IAppSettingsStore settingsStore,
    LocalizationService localizationService)
{
    private readonly IAppSettingsStore settingsStore = settingsStore;
    private readonly LocalizationService localizationService = localizationService;

    public async Task<bool> ConfirmAsync(string title, CancellationToken cancellationToken)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        if (!settings.ConfirmDelete)
        {
            return true;
        }

        var result = ShowDialog(title);
        if (result.Confirmed && result.RememberAnswer)
        {
            await settingsStore.SaveAsync(settings with { ConfirmDelete = false }, cancellationToken);
        }

        return result.Confirmed;
    }

    private DeleteConfirmationResult ShowDialog(string title)
    {
        var window = new System.Windows.Window
        {
            Title = localizationService.GetString("DeleteConfirmationTitle"),
            Width = 420,
            Height = 210,
            ResizeMode = System.Windows.ResizeMode.NoResize,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        };

        var panel = new System.Windows.Controls.StackPanel
        {
            Margin = new System.Windows.Thickness(20)
        };
        var message = string.Format(
            CultureInfo.InvariantCulture,
            localizationService.GetString("DeleteConfirmationMessage"),
            title);
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = message,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            Margin = new System.Windows.Thickness(0, 0, 0, 16)
        });

        var remember = new System.Windows.Controls.CheckBox
        {
            Content = localizationService.GetString("RememberAnswer"),
            Margin = new System.Windows.Thickness(0, 0, 0, 16)
        };
        panel.Children.Add(remember);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        var deleteButton = new System.Windows.Controls.Button
        {
            Content = localizationService.GetString("Delete"),
            MinWidth = 90,
            Margin = new System.Windows.Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        var cancelButton = new System.Windows.Controls.Button
        {
            Content = localizationService.GetString("Cancel"),
            MinWidth = 90,
            IsCancel = true
        };
        deleteButton.Click += (_, _) => window.DialogResult = true;
        buttons.Children.Add(deleteButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(buttons);

        window.Content = panel;
        if (System.Windows.Application.Current?.MainWindow is { } owner && !ReferenceEquals(owner, window))
        {
            window.Owner = owner;
        }

        var confirmed = window.ShowDialog() == true;
        return new DeleteConfirmationResult(confirmed, remember.IsChecked == true);
    }
}
