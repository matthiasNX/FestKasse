namespace FestKasse.Services;

public class DisplayService : IDisplayService
{
    private readonly ILogService _log;
    private CancellationTokenSource? _cancellationTokenSource;

    public DisplayService(ILogService logService)
    {
        _log = logService;
    }

    public void KeepScreenOn(int minutes)
    {
        AllowScreenOff();

        if (minutes <= 0)
            return;

#if ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity != null)
            {
                activity.Window?.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
                _log.Info($"Display wake lock activated for {minutes} minute(s).");

                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(minutes), token);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _log.Debug("Display-Wake-Lock-Timeout erreicht – deaktiviere.");
                            AllowScreenOff();
                        });
                    }
                    catch (TaskCanceledException)
                    {
                        // Erwartet bei Abbruch
                    }
                }, token);
            }
            else
            {
                _log.Warning("KeepScreenOn: Platform.CurrentActivity is null – wake lock not possible.");
            }
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error activating display wake lock.");
        }
#endif
    }

    public void AllowScreenOff()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

#if ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity?.Window != null)
            {
                activity.Window.ClearFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
                _log.Debug("Display-Wake-Lock deaktiviert.");
            }
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error deactivating display wake lock.");
        }
#endif
    }
}
