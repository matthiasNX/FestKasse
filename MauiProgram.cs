using Microsoft.Extensions.Logging;
using FestKasse.Helpers;
using FestKasse.Services;
using FestKasse.ViewModels;
using FestKasse.Views;
using ZXing.Net.Maui.Controls;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FestKasse;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Logging – read initial level from persisted settings
        var logService = CreateLogService();
        builder.Services.AddSingleton<ILogService>(logService);
        // Localization – apply persisted language before any UI is built
        ApplyInitialLanguage();

        // Services
        builder.Services.AddSingleton<IDataService, DataService>();
        builder.Services.AddSingleton<IDisplayService, DisplayService>();
        builder.Services.AddSingleton<IOrderService, OrderService>();
        builder.Services.AddSingleton<IOrderHistoryService, OrderHistoryService>();
        builder.Services.AddSingleton<ICashSessionService, CashSessionService>();
        builder.Services.AddSingleton<IOfflineOrderQueueService, OfflineOrderQueueService>();
        // Platform click-sound
#if ANDROID
        builder.Services.AddSingleton<IClickSoundService, FestKasse.Platforms.Android.ClickSoundService>();
#elif WINDOWS
        builder.Services.AddSingleton<IClickSoundService, FestKasse.Platforms.Windows.ClickSoundService>();
#else
        builder.Services.AddSingleton<IClickSoundService, FestKasse.Services.NullClickSoundService>();
#endif

        // ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<ArticleManagementViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<QrScanViewModel>();
        builder.Services.AddTransient<QrDisplayViewModel>();
        builder.Services.AddTransient<CategoryManagementViewModel>();
        builder.Services.AddTransient<StandManagementViewModel>();

        // Views
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<ArticleManagementPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<QrScanPage>();
        builder.Services.AddTransient<QrDisplayPage>();
        builder.Services.AddTransient<CategoryManagementPage>();
        builder.Services.AddTransient<StandManagementPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<OrderHistoryPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<LogViewerViewModel>();
        builder.Services.AddTransient<LogViewerPage>();
        builder.Services.AddTransient<OrderHistoryViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<CashSessionViewModel>();
        builder.Services.AddTransient<CashSessionPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Reads the persisted language from settings and applies it to
    /// <see cref="LocalizationService"/> before any UI is created.
    /// </summary>
    private static void ApplyInitialLanguage()
    {
        try
        {
            var settingsPath = AppConstants.SettingsFilePath;
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Models.AppSettings>(json);
                if (settings?.Language is { Length: > 0 } lang)
                    LocalizationService.Instance.SetLanguage(lang);
            }
        }
        catch { /* fall back to system language */ }
    }

    /// <summary>
    /// Reads the persisted log level from settings (if available) and creates the
    /// LogService before the DI container is built.
    /// </summary>
    private static LogService CreateLogService()
    {
        try
        {
            var settingsPath = AppConstants.SettingsFilePath;
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Models.AppSettings>(json);
                if (settings?.LogLevel is { Length: > 0 } level)
                    return new LogService(level);
            }
        }
        catch { /* fall back to default */ }

        return new LogService();
    }
}
