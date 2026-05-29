using System.ComponentModel;
using FestKasse.Services;

namespace FestKasse.Helpers;

/// <summary>
/// XAML markup extension that returns a localised string from <see cref="LocalizationService"/>.
/// Usage in XAML:
///   xmlns:helpers="clr-namespace:FestKasse.Helpers"
///   Text="{helpers:Tr Key=Menu_POS}"
/// The binding updates automatically when the language changes.
/// </summary>
[ContentProperty(nameof(Key))]
public sealed class TrExtension : IMarkupExtension<BindingBase>
{
    /// <summary>Resource key to look up.</summary>
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Key}]",
            Source = LocalizationService.Instance
        };
        return binding;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}
