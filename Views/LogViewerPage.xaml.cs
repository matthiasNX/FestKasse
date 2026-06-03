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

        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(LogViewerViewModel.LogContent))
                await ScrollToEndAsync();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Yield();
        await _viewModel.RefreshLogFilesAsync();
    }

    private async void OnScrollToEndClicked(object sender, EventArgs e) =>
        await ScrollToEndAsync();

    private async Task ScrollToEndAsync() =>
        await LogScrollView.ScrollToAsync(0, double.MaxValue, animated: false);
}
