namespace FestKasse.Services;

/// <summary>
/// Plays the OS default keyboard click sound.
/// Implemented per platform in the Platforms/* folders.
/// </summary>
public interface IClickSoundService
{
    void PlayClick();
}
