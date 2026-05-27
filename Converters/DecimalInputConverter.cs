using System.Globalization;

namespace FestKasse.Converters;

/// <summary>
/// Converts between decimal and string allowing both ',' and '.' as decimal separator.
/// Displays with the German comma format (e.g. "4,50").
/// </summary>
public class DecimalInputConverter : IValueConverter
{
    private static readonly CultureInfo _de = new("de-DE");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value is decimal d)
                return d.ToString("F2", _de);
        }
        catch { }
        return "0,00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            var s = (value as string ?? string.Empty).Trim()
                        .Replace("\u202f", "")   // narrow no-break space (some keyboards)
                        .Replace(" ", "")
                        .Replace(".", ",");

            // Allow intermediate inputs like ",50" or "4," or "-,5" without snapping to 0
            if (s == "-" || s == "," || s == "-,")
                return Binding.DoNothing;

            // Normalise leading decimal: ",50" → "0,50", "-,50" → "-0,50"
            if (s.StartsWith(","))
                s = "0" + s;
            else if (s.StartsWith("-,"))
                s = "-0" + s[1..];

            // Normalise trailing decimal: "4," → "4,0"
            if (s.EndsWith(","))
                s += "0";

            if (decimal.TryParse(s, NumberStyles.Any, _de, out var result))
                return result;
        }
        catch { }
        return 0m;
    }
}
