using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class CashSessionPage : ContentPage
{
    private readonly CashSessionViewModel _vm;

    public CashSessionPage(CashSessionViewModel vm)
    {
        _vm = vm;
        BindingContext = vm;
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
