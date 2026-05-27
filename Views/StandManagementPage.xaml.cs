using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class StandManagementPage : ContentPage
{
    private readonly StandManagementViewModel _viewModel;

    public StandManagementPage(StandManagementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
