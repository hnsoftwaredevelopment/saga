using EbookManager.Presentation.ViewModels;
using System.Windows;

namespace EbookManager.App.Views;

public partial class TagAssignmentWindow : Window
{
    private readonly TagAssignmentDialogViewModel viewModel;
    public TagAssignmentWindow(TagAssignmentDialogViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) =>
        {
            if (viewModel.HasTargetName) { NewNameBox.Focus(); NewNameBox.SelectAll(); }
            else ApplyButton.Focus();
        };
    }
    private void ApplyClicked(object sender, RoutedEventArgs e) { if (viewModel.CanApply) DialogResult = true; }
}
