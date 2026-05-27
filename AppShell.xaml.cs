using FestKasse.Views;

namespace FestKasse;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ArticleManagementPage), typeof(ArticleManagementPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(QrScanPage), typeof(QrScanPage));
        Routing.RegisterRoute(nameof(CategoryManagementPage), typeof(CategoryManagementPage));
        Routing.RegisterRoute(nameof(StandManagementPage), typeof(StandManagementPage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(OrderHistoryPage), typeof(OrderHistoryPage));
    }

    private async void OnKasseClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync("//MainPage/MainPage");
        Current.FlyoutIsPresented = false;
    }

    private async void OnStandManagementClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(StandManagementPage));
        Current.FlyoutIsPresented = false;
    }

    private async void OnCategoryManagementClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(CategoryManagementPage));
        Current.FlyoutIsPresented = false;
    }

    private async void OnArticleManagementClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(ArticleManagementPage));
        Current.FlyoutIsPresented = false;
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(SettingsPage));
        Current.FlyoutIsPresented = false;
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(AboutPage));
        Current.FlyoutIsPresented = false;
    }

    private async void OnOrderHistoryClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(OrderHistoryPage));
        Current.FlyoutIsPresented = false;
    }
}
