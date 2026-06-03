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
        Routing.RegisterRoute(nameof(QrDisplayPage), typeof(QrDisplayPage));
        Routing.RegisterRoute(nameof(CategoryManagementPage), typeof(CategoryManagementPage));
        Routing.RegisterRoute(nameof(StandManagementPage), typeof(StandManagementPage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(OrderHistoryPage), typeof(OrderHistoryPage));
        Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
        Routing.RegisterRoute(nameof(CashSessionPage), typeof(CashSessionPage));
        Routing.RegisterRoute(nameof(LogViewerPage), typeof(LogViewerPage));
    }

    private async void OnKasseClicked(object sender, EventArgs e)
    {
        Current.FlyoutIsPresented = false;
        await Current.GoToAsync("//MainPage");
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

    private async void OnLogViewerClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(LogViewerPage));
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

    private async void OnDashboardClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(DashboardPage));
        Current.FlyoutIsPresented = false;
    }

    private async void OnCashSessionClicked(object sender, EventArgs e)
    {
        await Current.GoToAsync(nameof(CashSessionPage));
        Current.FlyoutIsPresented = false;
    }
}
