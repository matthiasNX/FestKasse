using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private VerticalStackLayout[] _tabs = null!;
    private Button[] _tabBtns = null!;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _tabs    = [Tab0, Tab1, Tab2, Tab3];
        _tabBtns = [TabBtn0, TabBtn1, TabBtn2, TabBtn3];
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Yield();
        await _viewModel.InitializeAsync();
    }

    private void OnTabClicked(object sender, EventArgs e)
    {
        if (sender is Button btn &&
            btn.CommandParameter is string param &&
            int.TryParse(param, out int index))
        {
            SelectTab(index);
        }
    }

    private void SelectTab(int index)
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            bool active = i == index;
            _tabs[i].IsVisible = active;
            _tabBtns[i].FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        }
    }
}
