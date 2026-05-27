using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace FestKasse;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Catches crashes on Android's main (UI) thread from the Java/JVM side
        AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            e.Handled = true;
            var msg = $"[AndroidEnvironment] {e.Exception.GetType().FullName}\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}";
            try
            {
                var path = System.IO.Path.Combine(
                    Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "crash.log");
                System.IO.File.AppendAllText(path, $"\n---{DateTime.UtcNow:u}---\n{msg}\n");
            }
            catch { }
        };
    }
}
