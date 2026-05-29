using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace FestKasse.Services;

/// <summary>
/// Manages app language / culture at runtime.
/// Language values: "system" (default), "de", "en".
/// Call <see cref="SetLanguage"/> to switch – all bindings using
/// <see cref="Current"/> are updated automatically via <see cref="PropertyChanged"/>.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly Lazy<LocalizationService> _instance =
        new(() => new LocalizationService());

    public static LocalizationService Instance => _instance.Value;

    private ResourceManager _resourceManager =
        new("FestKasse.Resources.Strings.AppResources", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.CurrentUICulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationService() { }

    /// <summary>Returns the localised string for <paramref name="key"/>.</summary>
    public string this[string key]
    {
        get
        {
            var value = _resourceManager.GetString(key, _culture);
            return value ?? $"[{key}]";
        }
    }

    /// <summary>
    /// Switches the active culture.
    /// Pass "system" to follow the device's UI culture, "de" for German, "en" for English.
    /// </summary>
    public void SetLanguage(string language)
    {
        _culture = language switch
        {
            "de" => new CultureInfo("de"),
            "en" => new CultureInfo("en"),
            _ => CultureInfo.CurrentUICulture   // "system"
        };

        // Notify all XAML bindings that every key changed
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    /// <summary>
    /// Returns a formatted string using <see cref="string.Format(IFormatProvider,string,object[])"/>.
    /// </summary>
    public string Format(string key, params object[] args)
    {
        var template = this[key];
        try { return string.Format(_culture, template, args); }
        catch { return template; }
    }
}
