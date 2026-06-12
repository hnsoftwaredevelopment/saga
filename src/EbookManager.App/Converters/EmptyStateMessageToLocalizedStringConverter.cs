using System.Globalization;
using System.Windows.Data;
using EbookManager.App.Localization;

namespace EbookManager.App.Converters;

public sealed class EmptyStateMessageToLocalizedStringConverter : IMultiValueConverter
{
    private static readonly IReadOnlyDictionary<string, string> MessageKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Loading library..."] = "EmptyStateLoadingLibrary",
            ["Create or open a library to get started."] = "EmptyStateCreateOrOpenLibrary",
            ["Create or open a library before adding books."] = "EmptyStateCreateOrOpenBeforeAdding",
            ["Create or open a library before scanning folders."] = "EmptyStateCreateOrOpenBeforeScanning",
            ["This library is empty. Add books or scan a folder to begin."] = "EmptyStateLibraryEmpty",
            ["The active library folder no longer exists. Create or open a library to continue."] = "EmptyStateLibraryFolderMissing"
        };

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not string message || string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return MessageKeys.TryGetValue(message, out var key)
            ? LocalizedStrings.Current[key]
            : message;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        targetTypes.Select(_ => Binding.DoNothing).ToArray();
}
