namespace FestKasse;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

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

        var msg = $"[{source}] {ex.GetType().FullName}\n\n{ex.Message}\n\n{ex.StackTrace}";

        // Write to a file so it survives even if the UI is gone
        try
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "crash.log");
            File.AppendAllText(path, $"\n---{DateTime.Now:u}---\n{msg}\n");
        }
        catch { /* ignore write failures */ }

        // Also try to show an alert if the UI is still alive
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Windows.Count > 0 && Windows[0].Page is not null)
                    await Windows[0].Page!.DisplayAlert($"Crash ({source})", msg, "OK");
            }
            catch { /* UI may already be dead */ }
        });
    }
}
