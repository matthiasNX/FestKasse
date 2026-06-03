using System.Windows.Input;

namespace FestKasse.Controls;

public partial class SaveCancelButtonRow : ContentView
{
    public static readonly BindableProperty SaveCommandProperty =
        BindableProperty.Create(nameof(SaveCommand), typeof(ICommand), typeof(SaveCancelButtonRow));

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(SaveCancelButtonRow));

    public static readonly BindableProperty SaveTextProperty =
        BindableProperty.Create(nameof(SaveText), typeof(string), typeof(SaveCancelButtonRow), "Save");

    public static readonly BindableProperty CancelTextProperty =
        BindableProperty.Create(nameof(CancelText), typeof(string), typeof(SaveCancelButtonRow), "Cancel");

    public ICommand? SaveCommand
    {
        get => (ICommand?)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public string SaveText
    {
        get => (string)GetValue(SaveTextProperty);
        set => SetValue(SaveTextProperty, value);
    }

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    public SaveCancelButtonRow()
    {
        InitializeComponent();
    }
}
