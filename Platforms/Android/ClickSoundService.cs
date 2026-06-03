using Android.Media;
using FestKasse.Services;

namespace FestKasse.Platforms.Android;

/// <summary>
/// Plays the system keyboard click sound via <see cref="AudioManager.PlaySoundEffect"/>.
/// The effect is only audible when the user has "Touch sounds" enabled in system settings.
/// </summary>
public class ClickSoundService : IClickSoundService
{
    public void PlayClick()
    {
        try
        {
            var ctx = global::Android.App.Application.Context;
            var am = (AudioManager?)ctx.GetSystemService(global::Android.Content.Context.AudioService);
            am?.PlaySoundEffect(SoundEffect.KeyClick);
        }
        catch { /* non-critical */ }
    }
}
