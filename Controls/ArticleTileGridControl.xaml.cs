using System.Collections;
using System.Windows.Input;

namespace FestKasse.Controls;

public partial class ArticleTileGridControl : ContentView
{
    public static readonly BindableProperty CategoryGroupsProperty =
        BindableProperty.Create(nameof(CategoryGroups), typeof(IEnumerable), typeof(ArticleTileGridControl));

    public static readonly BindableProperty ShowGroupHeadersProperty =
        BindableProperty.Create(nameof(ShowGroupHeaders), typeof(bool), typeof(ArticleTileGridControl), true);

    public static readonly BindableProperty TileWidthProperty =
        BindableProperty.Create(nameof(TileWidth), typeof(double), typeof(ArticleTileGridControl), 120.0);

    public static readonly BindableProperty TileHeightProperty =
        BindableProperty.Create(nameof(TileHeight), typeof(double), typeof(ArticleTileGridControl), 110.0);

    public static readonly BindableProperty TileFontSizeDescriptionProperty =
        BindableProperty.Create(nameof(TileFontSizeDescription), typeof(double), typeof(ArticleTileGridControl), 13.0);

    public static readonly BindableProperty TileFontSizeSmallProperty =
        BindableProperty.Create(nameof(TileFontSizeSmall), typeof(double), typeof(ArticleTileGridControl), 12.0);

    public static readonly BindableProperty PlusCommandProperty =
        BindableProperty.Create(nameof(PlusCommand), typeof(ICommand), typeof(ArticleTileGridControl));

    public static readonly BindableProperty MinusCommandProperty =
        BindableProperty.Create(nameof(MinusCommand), typeof(ICommand), typeof(ArticleTileGridControl));

    public IEnumerable? CategoryGroups
    {
        get => (IEnumerable?)GetValue(CategoryGroupsProperty);
        set => SetValue(CategoryGroupsProperty, value);
    }

    public bool ShowGroupHeaders
    {
        get => (bool)GetValue(ShowGroupHeadersProperty);
        set => SetValue(ShowGroupHeadersProperty, value);
    }

    public double TileWidth
    {
        get => (double)GetValue(TileWidthProperty);
        set => SetValue(TileWidthProperty, value);
    }

    public double TileHeight
    {
        get => (double)GetValue(TileHeightProperty);
        set => SetValue(TileHeightProperty, value);
    }

    public double TileFontSizeDescription
    {
        get => (double)GetValue(TileFontSizeDescriptionProperty);
        set => SetValue(TileFontSizeDescriptionProperty, value);
    }

    public double TileFontSizeSmall
    {
        get => (double)GetValue(TileFontSizeSmallProperty);
        set => SetValue(TileFontSizeSmallProperty, value);
    }

    public ICommand? PlusCommand
    {
        get => (ICommand?)GetValue(PlusCommandProperty);
        set => SetValue(PlusCommandProperty, value);
    }

    public ICommand? MinusCommand
    {
        get => (ICommand?)GetValue(MinusCommandProperty);
        set => SetValue(MinusCommandProperty, value);
    }

    public ArticleTileGridControl()
    {
        InitializeComponent();
    }
}
