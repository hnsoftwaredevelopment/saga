using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using EbookManager.App.Converters;
using EbookManager.App.Localization;
using EbookManager.Presentation.ViewModels;

namespace EbookManager.App.Controls;

public sealed class LibraryColumnRowGrid : Grid
{
    private const double GripWidth = 8;
    private const double MinimumColumnWidth = 40;
    private static readonly FilePathToImageSourceConverter CoverConverter = new();

    public static readonly DependencyProperty LayoutSnapshotProperty =
        DependencyProperty.Register(
            nameof(LayoutSnapshot),
            typeof(LibraryColumnLayoutSnapshot),
            typeof(LibraryColumnRowGrid),
            new PropertyMetadata(null, OnLayoutChanged));

    public static readonly DependencyProperty BookRowProperty =
        DependencyProperty.Register(
            nameof(BookRow),
            typeof(BookRowViewModel),
            typeof(LibraryColumnRowGrid),
            new PropertyMetadata(null, OnLayoutChanged));

    public static readonly DependencyProperty IsHeaderProperty =
        DependencyProperty.Register(
            nameof(IsHeader),
            typeof(bool),
            typeof(LibraryColumnRowGrid),
            new PropertyMetadata(false, OnLayoutChanged));

    public static readonly DependencyProperty ViewProperty =
        DependencyProperty.Register(
            nameof(View),
            typeof(LibraryView),
            typeof(LibraryColumnRowGrid),
            new PropertyMetadata(LibraryView.Detailed));

    public LibraryColumnLayoutSnapshot? LayoutSnapshot
    {
        get => (LibraryColumnLayoutSnapshot?)GetValue(LayoutSnapshotProperty);
        set => SetValue(LayoutSnapshotProperty, value);
    }

    public BookRowViewModel? BookRow
    {
        get => (BookRowViewModel?)GetValue(BookRowProperty);
        set => SetValue(BookRowProperty, value);
    }

    public bool IsHeader
    {
        get => (bool)GetValue(IsHeaderProperty);
        set => SetValue(IsHeaderProperty, value);
    }

    public LibraryView View
    {
        get => (LibraryView)GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Rebuild();
    }

    private static void OnLayoutChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((LibraryColumnRowGrid)dependencyObject).Rebuild();

    private void Rebuild()
    {
        ColumnDefinitions.Clear();
        Children.Clear();

        if (LayoutSnapshot is null || (!IsHeader && BookRow is null))
        {
            return;
        }

        var columns = LayoutSnapshot.VisibleColumns.ToArray();
        for (var index = 0; index < columns.Length; index++)
        {
            var key = columns[index];
            ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(GetColumnWidth(key))
            });

            var element = IsHeader ? CreateHeaderCell(key) : CreateBookCell(key);
            SetColumn(element, index);
            Children.Add(element);

            if (IsHeader && index < columns.Length - 1)
            {
                Children.Add(CreateResizeThumb(index, key));
            }
        }
    }

    private double GetColumnWidth(LibraryColumnKey key) =>
        LayoutSnapshot?.ColumnWidths.TryGetValue(key, out var width) == true &&
        double.IsFinite(width) &&
        width > 0
            ? width
            : GetDefaultWidth(key);

    private FrameworkElement CreateHeaderCell(LibraryColumnKey key)
    {
        var textBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(8, 0, 8, 0)
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        if (key.CustomFieldId is { } fieldId)
        {
            textBlock.Text = LayoutSnapshot?.CustomMetadataFields.TryGetValue(fieldId, out var field) == true
                ? field.Name
                : string.Empty;
        }
        else
        {
            BindingOperations.SetBinding(
                textBlock,
                TextBlock.TextProperty,
                new Binding($"[{GetResourceKey(key.StandardOption ?? LibraryColumnOption.Title)}]")
                {
                    Source = LocalizedStrings.Current,
                    Mode = BindingMode.OneWay
                });
        }

        return textBlock;
    }

    private FrameworkElement CreateBookCell(LibraryColumnKey key)
    {
        var option = key.StandardOption;
        if (option == LibraryColumnOption.Cover)
        {
            var border = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            border.Background = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            var image = new Image
            {
                Stretch = Stretch.UniformToFill
            };
            BindingOperations.SetBinding(
                image,
                Image.SourceProperty,
                new Binding(nameof(BookRowViewModel.CoverPath))
                {
                    Source = BookRow,
                    Converter = CoverConverter,
                    ConverterParameter = "48"
                });
            border.Child = image;
            return border;
        }

        var textBlock = CreateTextElement(key);
        textBlock.VerticalAlignment = VerticalAlignment.Center;
        textBlock.Margin = new Thickness(8, 0, 8, 0);
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, GetForegroundResource(option));
        return textBlock;
    }

    private TextBlock CreateTextElement(LibraryColumnKey key)
    {
        var option = key.StandardOption;
        if (key.CustomFieldId is { } fieldId)
        {
            var highlighted = new HighlightedTextBlock();
            highlighted.SetResourceReference(HighlightedTextBlock.HighlightBrushProperty, "SearchHighlightBrush");
            highlighted.HighlightedText = BookRow!.GetCustomMetadataValue(fieldId);
            BindingOperations.SetBinding(
                highlighted,
                HighlightedTextBlock.SearchTextProperty,
                new Binding(nameof(BookRowViewModel.SearchText)) { Source = BookRow });
            return highlighted;
        }

        if (option is LibraryColumnOption.Title or LibraryColumnOption.Authors or LibraryColumnOption.Series or LibraryColumnOption.Format)
        {
            var highlighted = new HighlightedTextBlock();
            highlighted.SetResourceReference(HighlightedTextBlock.HighlightBrushProperty, "SearchHighlightBrush");
            BindingOperations.SetBinding(
                highlighted,
                HighlightedTextBlock.HighlightedTextProperty,
                new Binding(GetRowProperty(option.Value)) { Source = BookRow });
            BindingOperations.SetBinding(
                highlighted,
                HighlightedTextBlock.SearchTextProperty,
                new Binding(nameof(BookRowViewModel.SearchText)) { Source = BookRow });
            return highlighted;
        }

        var textBlock = new TextBlock();
        BindingOperations.SetBinding(
            textBlock,
            TextBlock.TextProperty,
            new Binding(GetRowProperty(option ?? LibraryColumnOption.Title)) { Source = BookRow });
        if (option == LibraryColumnOption.Description)
        {
            textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        }

        return textBlock;
    }

    private Thumb CreateResizeThumb(int columnIndex, LibraryColumnKey key)
    {
        var thumb = new Thumb
        {
            Width = GripWidth,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch,
            Cursor = Cursors.SizeWE,
            Background = Brushes.Transparent,
            Tag = key
        };
        thumb.Template = CreateResizeThumbTemplate();
        SetColumn(thumb, columnIndex);
        Panel.SetZIndex(thumb, 10);
        thumb.DragDelta += HeaderResizeDragDelta;
        thumb.DragCompleted += HeaderResizeDragCompleted;
        return thumb;
    }

    private static ControlTemplate CreateResizeThumbTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.BorderBrushProperty, new DynamicResourceExtension("AccentBrush"));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 1, 0));
        return new ControlTemplate(typeof(Thumb))
        {
            VisualTree = border
        };
    }

    private void HeaderResizeDragDelta(object sender, DragDeltaEventArgs e)
    {
        var columnIndex = sender is Thumb sourceThumb ? GetColumn(sourceThumb) : -1;
        if (columnIndex < 0 || columnIndex >= ColumnDefinitions.Count)
        {
            return;
        }

        var column = ColumnDefinitions[columnIndex];
        var currentWidth = column.ActualWidth > 0 ? column.ActualWidth : column.Width.Value;
        column.Width = new GridLength(Math.Max(MinimumColumnWidth, currentWidth + e.HorizontalChange));
    }

    private async void HeaderResizeDragCompleted(object sender, DragCompletedEventArgs e)
    {
        var columnIndex = sender is Thumb sourceThumb ? GetColumn(sourceThumb) : -1;
        if (DataContext is not LibraryViewModel viewModel ||
            sender is not Thumb { Tag: LibraryColumnKey key } ||
            columnIndex < 0 ||
            columnIndex >= ColumnDefinitions.Count)
        {
            return;
        }

        try
        {
            await viewModel.SetColumnWidthAsync(View, key, ColumnDefinitions[columnIndex].ActualWidth, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
    }

    private static string GetRowProperty(LibraryColumnOption option) =>
        option switch
        {
            LibraryColumnOption.Title => nameof(BookRowViewModel.Title),
            LibraryColumnOption.Authors => nameof(BookRowViewModel.Authors),
            LibraryColumnOption.Format => nameof(BookRowViewModel.Formats),
            LibraryColumnOption.Series => nameof(BookRowViewModel.Series),
            LibraryColumnOption.SeriesNumber => nameof(BookRowViewModel.SeriesNumberText),
            LibraryColumnOption.Status => nameof(BookRowViewModel.ReadingStatus),
            LibraryColumnOption.Language => nameof(BookRowViewModel.Language),
            LibraryColumnOption.Publisher => nameof(BookRowViewModel.Publisher),
            LibraryColumnOption.PublicationDate => nameof(BookRowViewModel.PublicationDate),
            LibraryColumnOption.Tags => nameof(BookRowViewModel.Tags),
            LibraryColumnOption.Isbn => nameof(BookRowViewModel.Isbn),
            LibraryColumnOption.Description => nameof(BookRowViewModel.Description),
            LibraryColumnOption.DateAdded => nameof(BookRowViewModel.DateAdded),
            LibraryColumnOption.LastModified => nameof(BookRowViewModel.LastModified),
            LibraryColumnOption.EReader => nameof(BookRowViewModel.EReader),
            _ => nameof(BookRowViewModel.Title)
        };

    private static string GetResourceKey(LibraryColumnOption option) =>
        option switch
        {
            LibraryColumnOption.Cover => "Cover",
            LibraryColumnOption.Title => "Title",
            LibraryColumnOption.Authors => "Authors",
            LibraryColumnOption.Format => "Type",
            LibraryColumnOption.Series => "Series",
            LibraryColumnOption.SeriesNumber => "SeriesNumber",
            LibraryColumnOption.Status => "Status",
            LibraryColumnOption.Language => "Language",
            LibraryColumnOption.Publisher => "Publisher",
            LibraryColumnOption.PublicationDate => "PublicationDate",
            LibraryColumnOption.Tags => "Tags",
            LibraryColumnOption.Isbn => "Isbn",
            LibraryColumnOption.Description => "Description",
            LibraryColumnOption.DateAdded => "DateAdded",
            LibraryColumnOption.LastModified => "LastModified",
            LibraryColumnOption.EReader => "EReader",
            _ => "Title"
        };

    private static string GetForegroundResource(LibraryColumnOption? option) =>
        option is LibraryColumnOption.Title or LibraryColumnOption.Authors
            ? "TextPrimaryBrush"
            : "TextSecondaryBrush";

    private static double GetDefaultWidth(LibraryColumnKey key) =>
        key.StandardOption switch
        {
            LibraryColumnOption.Cover => 80,
            LibraryColumnOption.Title => 220,
            LibraryColumnOption.Authors => 220,
            LibraryColumnOption.Format => 120,
            LibraryColumnOption.Series => 170,
            LibraryColumnOption.SeriesNumber => 110,
            LibraryColumnOption.Status => 120,
            LibraryColumnOption.Language => 130,
            LibraryColumnOption.Publisher => 170,
            LibraryColumnOption.PublicationDate => 130,
            LibraryColumnOption.Tags => 220,
            LibraryColumnOption.Isbn => 140,
            LibraryColumnOption.Description => 320,
            LibraryColumnOption.DateAdded => 150,
            LibraryColumnOption.LastModified => 150,
            LibraryColumnOption.EReader => 120,
            _ => 160
        };
}
