using CommunityToolkit.Mvvm.Input;

namespace FestKasse.ViewModels;

public partial class SettingsViewModel
{
    // ── Denomination management ───────────────────────────────────────────

    [RelayCommand]
    private void AddNote()
    {
        if (!decimal.TryParse(NewNoteEntry.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) || v <= 0)
            return;
        if (NoteItems.Any(i => i.Value == v)) return;
        var item = new DenominationItem { Value = v, Label = DenominationItem.MakeLabel(v) };
        var idx = NoteItems.TakeWhile(i => i.Value > v).Count();
        NoteItems.Insert(idx, item);
        NewNoteEntry = string.Empty;
    }

    [RelayCommand]
    private void RemoveNote(DenominationItem item) => NoteItems.Remove(item);

    [RelayCommand]
    private void AddCoin()
    {
        if (!decimal.TryParse(NewCoinEntry.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) || v <= 0)
            return;
        if (CoinItems.Any(i => i.Value == v)) return;
        var item = new DenominationItem { Value = v, Label = DenominationItem.MakeLabel(v) };
        var idx = CoinItems.TakeWhile(i => i.Value > v).Count();
        CoinItems.Insert(idx, item);
        NewCoinEntry = string.Empty;
    }

    [RelayCommand]
    private void RemoveCoin(DenominationItem item) => CoinItems.Remove(item);

    [RelayCommand]
    private void ResetNotesToEuro()
    {
        NoteItems.Clear();
        foreach (var v in new[] { 200m, 100m, 50m, 20m, 10m, 5m })
            NoteItems.Add(new DenominationItem { Value = v, Label = DenominationItem.MakeLabel(v) });
    }

    [RelayCommand]
    private void ResetCoinsToEuro()
    {
        CoinItems.Clear();
        foreach (var v in new[] { 2m, 1m, 0.50m, 0.20m, 0.10m, 0.05m, 0.02m, 0.01m })
            CoinItems.Add(new DenominationItem { Value = v, Label = DenominationItem.MakeLabel(v) });
    }
}
