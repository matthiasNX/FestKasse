namespace FestKasse.Services;

/// <summary>No-op fallback for platforms without a click-sound implementation.</summary>
public class NullClickSoundService : IClickSoundService
{
    public void PlayClick() { }
}
