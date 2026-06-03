namespace FestKasse.Helpers;

/// <summary>
/// Single source of truth for formatting denomination values (notes and coins)
/// as human-readable labels. Replaces the duplicated logic that previously
/// existed in both <c>MainViewModel.InitDenominationTiles()</c> and
/// <c>SettingsViewModel.DenominationItem.MakeLabel()</c>.
/// </summary>
public static class DenominationFormatter
{
    /// <summary>
    /// Returns a display label for a currency denomination value.
    /// Values ≥ 1 are shown in euros (e.g. "10 €"), values &lt; 1 in cents (e.g. "50 ct").
    /// </summary>
    public static string MakeLabel(decimal value)
        => value >= 1m
            ? $"{value:0.##} \u20ac"           // e.g. "10 €"
            : $"{value * 100m:0.##} ct";       // e.g. "50 ct"
}
