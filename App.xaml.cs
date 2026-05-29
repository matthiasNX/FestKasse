using FestKasse.Services;

namespace FestKasse;

public partial class App : Application
{
    private readonly ILogService _log;

    public App(ILogService logService)
    {
        _log = logService;
        InitializeComponent();

        _log.Info("App gestartet.");

        // Catches exceptions on the main UI thread
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            LogCrash("AppDomain", e.ExceptionObject as Exception);

        // Catches exceptions from Task / async that were never awaited
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            LogCrash("UnobservedTask", e.Exception);
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private void LogCrash(string source, Exception? ex)
    {
        if (ex is null) return;

        _log.Exception(ex, $"[CRASH:{source}]");

        // Also try to show an alert if the UI is still alive
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Windows.Count > 0 && Windows[0].Page is not null)
                    await Windows[0].Page!.DisplayAlert(
                        $"Crash ({source})",
                        $"{ex.GetType().Name}: {ex.Message}",
                        "OK");
            }
            catch { /* UI may already be dead */ }
        });
    }
}
