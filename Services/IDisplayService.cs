namespace FestKasse.Services;

public interface IDisplayService
{
    void KeepScreenOn(int minutes);
    void AllowScreenOff();
}
