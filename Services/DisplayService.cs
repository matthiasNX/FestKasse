namespace FestKasse.Services;

public class DisplayService : IDisplayService
{
    private CancellationTokenSource? _cancellationTokenSource;

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

                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(minutes), token);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            AllowScreenOff();
                        });
                    }
                    catch (TaskCanceledException)
                    {
                        // Erwartet bei Abbruch
                    }
                }, token);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Aktivieren des Wake-Lock: {ex.Message}");
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
            activity?.Window?.ClearFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Deaktivieren des Wake-Lock: {ex.Message}");
        }
#endif
    }
}
