using CommunityToolkit.Mvvm.ComponentModel;
using FestKasse.Models;

namespace FestKasse.ViewModels;

/// <summary>Represents a stand with an 'include in export' flag.</summary>
public partial class StandExportItem : ObservableObject
{
    public Stand Stand { get; }
    public string Name => Stand.Name;

    [ObservableProperty]
    private bool _isSelected = true;

    public StandExportItem(Stand stand) => Stand = stand;
}

/// <summary>Represents a single note or coin denomination entry in the settings list.</summary>
public partial class DenominationItem : ObservableObject
{
    public decimal Value { get; init; }
    public string Label { get; init; } = string.Empty;

    public static string MakeLabel(decimal v)
        => Helpers.DenominationFormatter.MakeLabel(v);
}
