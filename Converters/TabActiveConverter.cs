using System.Globalization;

namespace FestKasse.Converters;

public class TabActiveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selectedTab && parameter is string paramStr && int.TryParse(paramStr, out var tabIndex))
        {
            return selectedTab == tabIndex
                ? Color.FromArgb("#1565C0")  // darker shade = active
                : Colors.Transparent;
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
