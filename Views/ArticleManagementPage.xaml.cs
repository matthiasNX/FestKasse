using FestKasse.ViewModels;

namespace FestKasse.Views;

public partial class ArticleManagementPage : ContentPage
{
    private readonly ArticleManagementViewModel _viewModel;

    public ArticleManagementPage(ArticleManagementViewModel viewModel)
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

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
    }
}
