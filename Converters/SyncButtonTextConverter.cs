using System.Globalization;

namespace FestKasse.Converters;

public class SyncButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSyncing)
        {
            return isSyncing ? "⏳ Synchronisiere..." : "🔄 Jetzt synchronisieren";
        }
        return "🔄 Jetzt synchronisieren";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
