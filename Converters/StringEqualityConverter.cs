using System.Globalization;

namespace FestKasse.Converters;

/// <summary>
/// Returns true when two string values are equal.
/// Used in MultiBinding DataTriggers to compare the current item to the selected value.
/// </summary>
public class StringEqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        => values.Length == 2 && values[0]?.ToString() == values[1]?.ToString();

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
