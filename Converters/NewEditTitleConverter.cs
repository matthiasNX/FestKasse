using System.Globalization;

namespace FestKasse.Converters;

public class NewEditTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isNew)
        {
            return isNew ? "Neuer Artikel" : "Artikel bearbeiten";
        }
        return "Artikel";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
