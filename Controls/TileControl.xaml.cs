using System.Windows.Input;

namespace FestKasse.Controls;

public partial class TileControl : ContentView
{
    // ── Appearance ────────────────────────────────────────────────────────────
    public static readonly BindableProperty TileColorProperty =
        BindableProperty.Create(nameof(TileColor), typeof(Color), typeof(TileControl), Colors.Gray);

    public static readonly BindableProperty TileWidthRequestProperty =
        BindableProperty.Create(nameof(TileWidthRequest), typeof(double), typeof(TileControl), 120.0);

    public static readonly BindableProperty TileHeightRequestProperty =
        BindableProperty.Create(nameof(TileHeightRequest), typeof(double), typeof(TileControl), 110.0);

    // ── Content ───────────────────────────────────────────────────────────────
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(TileControl), string.Empty);

    public static readonly BindableProperty TitleFontSizeProperty =
        BindableProperty.Create(nameof(TitleFontSize), typeof(double), typeof(TileControl), 13.0);

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(TileControl), null,
            propertyChanged: (b, _, n) => ((TileControl)b).IconVisible = !string.IsNullOrEmpty((string?)n));

    public static readonly BindableProperty IconVisibleProperty =
        BindableProperty.Create(nameof(IconVisible), typeof(bool), typeof(TileControl), false);

    public static readonly BindableProperty SubTitleProperty =
        BindableProperty.Create(nameof(SubTitle), typeof(string), typeof(TileControl), string.Empty);

    public static readonly BindableProperty SubTitleVisibleProperty =
        BindableProperty.Create(nameof(SubTitleVisible), typeof(bool), typeof(TileControl), true);

    public static readonly BindableProperty SubTitleFontSizeProperty =
        BindableProperty.Create(nameof(SubTitleFontSize), typeof(double), typeof(TileControl), 12.0);

    public static readonly BindableProperty CountProperty =
        BindableProperty.Create(nameof(Count), typeof(string), typeof(TileControl), "0");

    // ── Commands ──────────────────────────────────────────────────────────────
    public static readonly BindableProperty PlusCommandProperty =
        BindableProperty.Create(nameof(PlusCommand), typeof(ICommand), typeof(TileControl));

    public static readonly BindableProperty PlusCommandParameterProperty =
        BindableProperty.Create(nameof(PlusCommandParameter), typeof(object), typeof(TileControl));

    public static readonly BindableProperty MinusCommandProperty =
        BindableProperty.Create(nameof(MinusCommand), typeof(ICommand), typeof(TileControl));

    public static readonly BindableProperty MinusCommandParameterProperty =
        BindableProperty.Create(nameof(MinusCommandParameter), typeof(object), typeof(TileControl));

    // ── CLR wrappers ─────────────────────────────────────────────────────────
    public Color TileColor
    {
        get => (Color)GetValue(TileColorProperty);
        set => SetValue(TileColorProperty, value);
    }

    public double TileWidthRequest
    {
        get => (double)GetValue(TileWidthRequestProperty);
        set => SetValue(TileWidthRequestProperty, value);
    }

    public double TileHeightRequest
    {
        get => (double)GetValue(TileHeightRequestProperty);
        set => SetValue(TileHeightRequestProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double TitleFontSize
    {
        get => (double)GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IconVisible
    {
        get => (bool)GetValue(IconVisibleProperty);
        private set => SetValue(IconVisibleProperty, value);
    }

    public string SubTitle
    {
        get => (string)GetValue(SubTitleProperty);
        set => SetValue(SubTitleProperty, value);
    }

    public bool SubTitleVisible
    {
        get => (bool)GetValue(SubTitleVisibleProperty);
        set => SetValue(SubTitleVisibleProperty, value);
    }

    public double SubTitleFontSize
    {
        get => (double)GetValue(SubTitleFontSizeProperty);
        set => SetValue(SubTitleFontSizeProperty, value);
    }

    public string Count
    {
        get => (string)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public ICommand? PlusCommand
    {
        get => (ICommand?)GetValue(PlusCommandProperty);
        set => SetValue(PlusCommandProperty, value);
    }

    public object? PlusCommandParameter
    {
        get => GetValue(PlusCommandParameterProperty);
        set => SetValue(PlusCommandParameterProperty, value);
    }

    public ICommand? MinusCommand
    {
        get => (ICommand?)GetValue(MinusCommandProperty);
        set => SetValue(MinusCommandProperty, value);
    }

    public object? MinusCommandParameter
    {
        get => GetValue(MinusCommandParameterProperty);
        set => SetValue(MinusCommandParameterProperty, value);
    }

    public TileControl()
    {
        InitializeComponent();
    }
}
