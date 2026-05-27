using System.Globalization;

namespace FestKasse.Converters;

public class ChangeColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal change)
        {
            return change >= 0 ? Colors.Green : Colors.Red;
        }
        return Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
