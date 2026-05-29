using System.Globalization;
using FestKasse.Services;

namespace FestKasse.Converters;

/// <summary>
/// Returns the "New" or "Edit" title for articles or categories.
/// ConverterParameter: "category" -> use Category keys; otherwise -> Article keys.
/// </summary>
public class NewEditTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var loc = LocalizationService.Instance;
        bool isCategory = parameter is string p && p.Equals("category", StringComparison.OrdinalIgnoreCase);

        if (value is bool isNew)
        {
            if (isCategory)
                return isNew ? loc["Converter_NewCategory"] : loc["Converter_EditCategory"];
            return isNew ? loc["Converter_NewArticle"] : loc["Converter_EditArticle"];
        }
        return isCategory ? loc["Converter_NewCategory"] : loc["Converter_NewArticle"];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
