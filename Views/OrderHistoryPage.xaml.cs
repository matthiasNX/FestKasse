using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class OrderHistoryPage : ContentPage
{
    private readonly OrderHistoryViewModel _viewModel;

    public OrderHistoryPage(OrderHistoryViewModel viewModel)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            // Show on next UI tick — page isn't ready yet during ctor
            Dispatcher.Dispatch(async () =>
                await DisplayAlert("InitializeComponent Crash", $"{ex.GetType().FullName}\n\n{ex.Message}\n\n{ex.StackTrace}", "OK"));
            return;
        }
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Crash-Details", $"{ex.GetType().FullName}\n\n{ex.Message}\n\n{ex.StackTrace}", "OK");
        }
    }
}
