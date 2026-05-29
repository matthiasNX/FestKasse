using System.Globalization;
using FestKasse.Services;

namespace FestKasse.Converters;

public class SyncButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var loc = LocalizationService.Instance;
        if (value is bool isSyncing)
            return isSyncing ? loc["Converter_Syncing"] : loc["Converter_SyncNow"];
        return loc["Converter_SyncNow"];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
