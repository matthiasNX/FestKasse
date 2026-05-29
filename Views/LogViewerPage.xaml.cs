using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class LogViewerPage : ContentPage
{
    private readonly LogViewerViewModel _viewModel;

    public LogViewerPage(LogViewerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadLogAsync();
    }
}
