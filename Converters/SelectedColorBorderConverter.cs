using System.Globalization;

namespace FestKasse.Converters;

public class SelectedColorBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string selectedColor && parameter is string currentColor)
        {
            return selectedColor.Equals(currentColor, StringComparison.OrdinalIgnoreCase) 
                ? Colors.Black 
                : Colors.Transparent;
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
