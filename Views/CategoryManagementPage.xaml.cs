using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class CategoryManagementPage : ContentPage
{
    private readonly CategoryManagementViewModel _viewModel;

    public CategoryManagementPage(CategoryManagementViewModel viewModel)
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
