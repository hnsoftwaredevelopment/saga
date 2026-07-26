using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace EbookManager.App.Controls;

public sealed class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
{
    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
        nameof(ItemWidth),
        typeof(double),
        typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(170.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
        nameof(ItemHeight),
        typeof(double),
        typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(270.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    private Size extent;
    private Size viewport;
    private Point offset;

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public bool CanHorizontallyScroll { get; set; }

    public bool CanVerticallyScroll { get; set; }

    public double ExtentWidth => extent.Width;

    public double ExtentHeight => extent.Height;

    public double ViewportWidth => viewport.Width;

    public double ViewportHeight => viewport.Height;

    public double HorizontalOffset => offset.X;

    public double VerticalOffset => offset.Y;

    public ScrollViewer? ScrollOwner { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var itemsOwner = ItemsControl.GetItemsOwner(this);
        if (itemsOwner is null || ItemContainerGenerator is null)
        {
            ClearRealizedChildren();
            return CoerceFiniteSize(availableSize);
        }

        var itemCount = itemsOwner.Items.Count;
        var safeItemWidth = Math.Max(1, ItemWidth);
        var safeItemHeight = Math.Max(1, ItemHeight);
        var availableWidth = double.IsInfinity(availableSize.Width) ? safeItemWidth : Math.Max(1, availableSize.Width);
        var columns = Math.Max(1, (int)Math.Floor(availableWidth / safeItemWidth));
        var rows = itemCount == 0 ? 0 : (int)Math.Ceiling(itemCount / (double)columns);

        var availableHeight = double.IsInfinity(availableSize.Height)
            ? safeItemHeight
            : Math.Max(1, availableSize.Height);
        viewport = new Size(availableWidth, availableHeight);
        extent = new Size(columns * safeItemWidth, rows * safeItemHeight);
        offset = new Point(
            Math.Clamp(offset.X, 0, Math.Max(0, extent.Width - viewport.Width)),
            Math.Clamp(offset.Y, 0, Math.Max(0, extent.Height - viewport.Height)));
        ScrollOwner?.InvalidateScrollInfo();

        var generator = ItemContainerGenerator;
        if (itemCount == 0)
        {
            ClearRealizedChildren();
            return new Size(availableWidth, 0);
        }

        var firstVisibleRow = Math.Max(0, (int)Math.Floor(offset.Y / safeItemHeight));
        var lastVisibleRow = Math.Min(rows - 1, (int)Math.Ceiling((offset.Y + viewport.Height) / safeItemHeight));
        var firstIndex = Math.Min(itemCount - 1, firstVisibleRow * columns);
        var lastIndex = Math.Min(itemCount - 1, ((lastVisibleRow + 1) * columns) - 1);

        CleanupItems(firstIndex, lastIndex);

        var startPosition = generator.GeneratorPositionFromIndex(firstIndex);
        var childIndex = startPosition.Offset == 0 ? startPosition.Index : startPosition.Index + 1;
        using (generator.StartAt(startPosition, GeneratorDirection.Forward, allowStartAtRealizedItem: true))
        {
            for (var itemIndex = firstIndex; itemIndex <= lastIndex; itemIndex++, childIndex++)
            {
                if (generator.GenerateNext(out var newlyRealized) is not UIElement child)
                {
                    continue;
                }

                if (newlyRealized)
                {
                    if (childIndex >= InternalChildren.Count)
                    {
                        AddInternalChild(child);
                    }
                    else
                    {
                        InsertInternalChild(childIndex, child);
                    }

                    generator.PrepareItemContainer(child);
                }

                child.Measure(new Size(safeItemWidth, safeItemHeight));
            }
        }

        return viewport;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemsOwner = ItemsControl.GetItemsOwner(this);
        if (itemsOwner is null || ItemContainerGenerator is null || itemsOwner.Items.Count == 0)
        {
            return finalSize;
        }

        var safeItemWidth = Math.Max(1, ItemWidth);
        var safeItemHeight = Math.Max(1, ItemHeight);
        var columns = Math.Max(1, (int)Math.Floor(Math.Max(1, finalSize.Width) / safeItemWidth));

        foreach (UIElement child in InternalChildren)
        {
            var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(
                new GeneratorPosition(InternalChildren.IndexOf(child), 0));
            if (itemIndex < 0)
            {
                continue;
            }

            var row = itemIndex / columns;
            var column = itemIndex % columns;
            var rect = new Rect(
                (column * safeItemWidth) - offset.X,
                (row * safeItemHeight) - offset.Y,
                safeItemWidth,
                safeItemHeight);
            child.Arrange(rect);
        }

        return finalSize;
    }

    public void LineUp() => SetVerticalOffset(VerticalOffset - 16);

    public void LineDown() => SetVerticalOffset(VerticalOffset + 16);

    public void LineLeft() => SetHorizontalOffset(HorizontalOffset - 16);

    public void LineRight() => SetHorizontalOffset(HorizontalOffset + 16);

    public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);

    public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);

    public void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);

    public void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

    public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - 48);

    public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + 48);

    public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - 48);

    public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + 48);

    public void SetHorizontalOffset(double horizontalOffset)
    {
        offset.X = Math.Clamp(horizontalOffset, 0, Math.Max(0, ExtentWidth - ViewportWidth));
        InvalidateMeasure();
    }

    public void SetVerticalOffset(double verticalOffset)
    {
        offset.Y = Math.Clamp(verticalOffset, 0, Math.Max(0, ExtentHeight - ViewportHeight));
        InvalidateMeasure();
    }

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        var childIndex = InternalChildren.IndexOf(visual as UIElement);
        if (childIndex < 0 || ItemContainerGenerator is null)
        {
            return Rect.Empty;
        }

        var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(childIndex, 0));
        if (itemIndex < 0)
        {
            return Rect.Empty;
        }

        var safeItemWidth = Math.Max(1, ItemWidth);
        var safeItemHeight = Math.Max(1, ItemHeight);
        var columns = Math.Max(1, (int)Math.Floor(Math.Max(1, viewport.Width) / safeItemWidth));
        var row = itemIndex / columns;
        var column = itemIndex % columns;
        var itemRect = new Rect(column * safeItemWidth, row * safeItemHeight, safeItemWidth, safeItemHeight);
        var targetRect = rectangle == Rect.Empty
            ? itemRect
            : new Rect(
                itemRect.X + rectangle.X,
                itemRect.Y + rectangle.Y,
                Math.Min(rectangle.Width, itemRect.Width),
                Math.Min(rectangle.Height, itemRect.Height));

        if (targetRect.Left < HorizontalOffset)
        {
            SetHorizontalOffset(targetRect.Left);
        }
        else if (targetRect.Right > HorizontalOffset + ViewportWidth)
        {
            SetHorizontalOffset(targetRect.Right - ViewportWidth);
        }

        if (targetRect.Top < VerticalOffset)
        {
            SetVerticalOffset(targetRect.Top);
        }
        else if (targetRect.Bottom > VerticalOffset + ViewportHeight)
        {
            SetVerticalOffset(targetRect.Bottom - ViewportHeight);
        }

        return new Rect(
            targetRect.X - HorizontalOffset,
            targetRect.Y - VerticalOffset,
            targetRect.Width,
            targetRect.Height);
    }

    private static Size CoerceFiniteSize(Size size) =>
        new(
            double.IsInfinity(size.Width) ? 0 : Math.Max(0, size.Width),
            double.IsInfinity(size.Height) ? 0 : Math.Max(0, size.Height));

    private void CleanupItems(int firstIndex, int lastIndex)
    {
        var generator = ItemContainerGenerator;
        if (generator is null)
        {
            ClearRealizedChildren();
            return;
        }

        for (var childIndex = InternalChildren.Count - 1; childIndex >= 0; childIndex--)
        {
            var generatorPosition = new GeneratorPosition(childIndex, 0);
            var itemIndex = generator.IndexFromGeneratorPosition(generatorPosition);
            if (itemIndex < firstIndex || itemIndex > lastIndex)
            {
                generator.Remove(generatorPosition, 1);
                RemoveInternalChildRange(childIndex, 1);
            }
        }
    }

    private void ClearRealizedChildren()
    {
        if (InternalChildren.Count > 0)
        {
            RemoveInternalChildRange(0, InternalChildren.Count);
        }
    }
}
