namespace EbookManager.App.Views;

public partial class BookDetailsView : System.Windows.Controls.UserControl
{
    public BookDetailsView()
    {
        InitializeComponent();
    }

    private void NumericCustomMetadataPreviewTextInput(
        object sender,
        System.Windows.Input.TextCompositionEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        var proposed = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, e.Text);
        e.Handled = !IsPotentialNumber(proposed);
    }

    private void NumericCustomMetadataPasting(object sender, System.Windows.DataObjectPastingEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox ||
            !e.DataObject.GetDataPresent(System.Windows.DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pasted = e.DataObject.GetData(System.Windows.DataFormats.Text) as string ?? string.Empty;
        var proposed = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, pasted);
        if (!IsPotentialNumber(proposed))
        {
            e.CancelCommand();
        }
    }

    private static bool IsPotentialNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "-" or "+" or "." or ",")
        {
            return true;
        }

        var decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (!string.IsNullOrEmpty(decimalSeparator) &&
            value.EndsWith(decimalSeparator, StringComparison.Ordinal) &&
            decimal.TryParse(
                value[..^decimalSeparator.Length],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out _))
        {
            return true;
        }

        return decimal.TryParse(
            value,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.CurrentCulture,
            out _);
    }
}
