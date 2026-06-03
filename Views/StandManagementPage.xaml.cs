using FestKasse.Controls;
using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class StandManagementPage : ContentPage
{
    private readonly StandManagementViewModel _vm;

    public StandManagementPage(StandManagementViewModel viewModel)
    {
        _vm = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Yield();
        try { await _vm.InitializeAsync(); }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }
}
