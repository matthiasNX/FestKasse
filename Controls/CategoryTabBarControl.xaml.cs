using System.Collections;
using System.Windows.Input;

namespace FestKasse.Controls;

public partial class CategoryTabBarControl : ContentView
{
    public static readonly BindableProperty CategoriesProperty =
        BindableProperty.Create(nameof(Categories), typeof(IEnumerable), typeof(CategoryTabBarControl));

    public static readonly BindableProperty SelectedCategoryProperty =
        BindableProperty.Create(nameof(SelectedCategory), typeof(string), typeof(CategoryTabBarControl), string.Empty);

    public static readonly BindableProperty SelectCategoryCommandProperty =
        BindableProperty.Create(nameof(SelectCategoryCommand), typeof(ICommand), typeof(CategoryTabBarControl));

    public static readonly BindableProperty LogoSourceProperty =
        BindableProperty.Create(nameof(LogoSource), typeof(ImageSource), typeof(CategoryTabBarControl));

    public static readonly BindableProperty IsLogoVisibleProperty =
        BindableProperty.Create(nameof(IsLogoVisible), typeof(bool), typeof(CategoryTabBarControl));

    public IEnumerable? Categories
    {
        get => (IEnumerable?)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public string SelectedCategory
    {
        get => (string)GetValue(SelectedCategoryProperty);
        set => SetValue(SelectedCategoryProperty, value);
    }

    public ICommand? SelectCategoryCommand
    {
        get => (ICommand?)GetValue(SelectCategoryCommandProperty);
        set => SetValue(SelectCategoryCommandProperty, value);
    }

    public ImageSource? LogoSource
    {
        get => (ImageSource?)GetValue(LogoSourceProperty);
        set => SetValue(LogoSourceProperty, value);
    }

    public bool IsLogoVisible
    {
        get => (bool)GetValue(IsLogoVisibleProperty);
        set => SetValue(IsLogoVisibleProperty, value);
    }

    public CategoryTabBarControl()
    {
        InitializeComponent();
    }
}
