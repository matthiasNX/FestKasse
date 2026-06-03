using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _isLandscape = false;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Yield();
        await _viewModel.InitializeAsync();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0)
            return;

        bool landscape = width > height;
        if (landscape == _isLandscape)
            return;

        _isLandscape = landscape;
        PortraitLayout.IsVisible = !landscape;
        LandscapeLayout.IsVisible = landscape;
    }


}
