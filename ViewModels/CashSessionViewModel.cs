using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class CashSessionViewModel : ObservableObject
{
    private readonly ICashSessionService _sessionService;
    private readonly ILogService _log;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSessionOpen))]
    [NotifyPropertyChangedFor(nameof(IsSessionClosed))]
    private CashSession? _activeSession;

    [ObservableProperty]
    private decimal _openingCash;

    [ObservableProperty]
    private decimal _closingCash;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool IsSessionOpen   => ActiveSession?.IsOpen == true;
    public bool IsSessionClosed => !IsSessionOpen;

    public CashSessionViewModel(ICashSessionService sessionService, ILogService logService)
    {
        _sessionService = sessionService;
        _log = logService;
    }

    public async Task InitializeAsync()
    {
        ActiveSession = await _sessionService.GetOpenSessionAsync();
        StatusMessage = IsSessionOpen
            ? $"Kasse offen seit {ActiveSession!.OpenedAt:HH:mm}"
            : "Kasse ist geschlossen.";
    }

    [RelayCommand]
    private async Task OpenSessionAsync()
    {
        try
        {
            ActiveSession = await _sessionService.OpenSessionAsync(OpeningCash);
            StatusMessage = $"Kasse geöffnet um {ActiveSession.OpenedAt:HH:mm} mit {OpeningCash:F2} €.";
            _log.Info($"Cash session opened. Opening={OpeningCash:F2}");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Failed to open cash session.");
            await Shell.Current.DisplayAlert("Fehler", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task CloseSessionAsync()
    {
        if (ActiveSession is null) return;
        try
        {
            var closed = await _sessionService.CloseSessionAsync(ClosingCash);
            StatusMessage = $"Kasse geschlossen. Umsatz: {closed.Revenue:F2} €  |  Differenz: {closed.Difference:F2} €";
            _log.Info($"Cash session closed. Revenue={closed.Revenue:F2}, Diff={closed.Difference:F2}");
            ActiveSession = null;
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Failed to close cash session.");
            await Shell.Current.DisplayAlert("Fehler", ex.Message, "OK");
        }
    }
}
