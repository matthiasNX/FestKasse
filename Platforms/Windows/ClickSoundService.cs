using FestKasse.Services;
using Microsoft.UI.Xaml;

namespace FestKasse.Platforms.Windows;

/// <summary>
/// Plays the WinUI ElementSoundPlayer "Invoke" sound (Maps to the system keyboard click).
/// The user can mute it via Windows Settings → Accessibility → Visual effects.
/// </summary>
public class ClickSoundService : IClickSoundService
{
    public void PlayClick()
    {
        try
        {
            // Only play when ElementSoundPlayer is not disabled by the user/OS.
            if (ElementSoundPlayer.State != ElementSoundPlayerState.Off)
                ElementSoundPlayer.Play(ElementSoundKind.Invoke);
        }
        catch { /* non-critical */ }
    }
}
