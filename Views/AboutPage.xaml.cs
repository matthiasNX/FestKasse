namespace FestKasse.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void OnWebsiteTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await Browser.Default.OpenAsync("https://www.weinfest-donnersdorf.de", BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            // ignore if browser can't open
        }
    }
}
