using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EbookManager.App.Controls;

public static class MouseWheelScrollAssist
{
    public static readonly DependencyProperty BubbleToParentProperty =
        DependencyProperty.RegisterAttached(
            "BubbleToParent",
            typeof(bool),
            typeof(MouseWheelScrollAssist),
            new PropertyMetadata(false, OnBubbleToParentChanged));

    public static bool GetBubbleToParent(DependencyObject element) =>
        (bool)element.GetValue(BubbleToParentProperty);

    public static void SetBubbleToParent(DependencyObject element, bool value) =>
        element.SetValue(BubbleToParentProperty, value);

    private static void OnBubbleToParentChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not UIElement element)
        {
            return;
        }

        if ((bool)eventArgs.NewValue)
        {
            element.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            element.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs eventArgs)
    {
        if (sender is not DependencyObject source)
        {
            return;
        }

        var parentScrollViewer = FindParentScrollViewer(source);
        if (parentScrollViewer is null)
        {
            return;
        }

        eventArgs.Handled = true;
        parentScrollViewer.RaiseEvent(new MouseWheelEventArgs(eventArgs.MouseDevice, eventArgs.Timestamp, eventArgs.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        });
    }

    private static ScrollViewer? FindParentScrollViewer(DependencyObject source)
    {
        var current = VisualTreeHelper.GetParent(source);
        while (current is not null)
        {
            if (current is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
