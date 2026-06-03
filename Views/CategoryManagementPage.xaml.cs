using FestKasse.Controls;
using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class CategoryManagementPage : ContentPage
{
    private readonly CategoryManagementViewModel _vm;

    public CategoryManagementPage(CategoryManagementViewModel viewModel)
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
