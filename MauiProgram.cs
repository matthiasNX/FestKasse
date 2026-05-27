using Microsoft.Extensions.Logging;
using FestKasse.Services;
using FestKasse.ViewModels;
using FestKasse.Views;
using ZXing.Net.Maui.Controls;

namespace FestKasse;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<IDataService, DataService>();
        builder.Services.AddSingleton<IDisplayService, DisplayService>();
        builder.Services.AddSingleton<IOrderService, OrderService>();
        builder.Services.AddSingleton<IOrderHistoryService, OrderHistoryService>();

        // ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<ArticleManagementViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<QrScanViewModel>();
        builder.Services.AddTransient<CategoryManagementViewModel>();
        builder.Services.AddTransient<StandManagementViewModel>();

        // Views
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<ArticleManagementPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<QrScanPage>();
        builder.Services.AddTransient<CategoryManagementPage>();
        builder.Services.AddTransient<StandManagementPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<OrderHistoryPage>();
        builder.Services.AddTransient<OrderHistoryViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
