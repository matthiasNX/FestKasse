using CommunityToolkit.Mvvm.ComponentModel;

namespace FestKasse.Models;

public partial class DenominationTile : ObservableObject
{
    public decimal Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool IsNote { get; init; }

    private int _count;
    public int Count
    {
        get => _count;
        set
        {
            if (SetProperty(ref _count, value))
                OnPropertyChanged(nameof(CountDisplay));
        }
    }

    public string CountDisplay => _count > 0 ? _count.ToString() : "0";
}
