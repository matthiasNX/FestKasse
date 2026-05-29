using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FestKasse.Messages;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class QrScanViewModel : ObservableObject
{
    private readonly ILogService _log;

    [ObservableProperty]
    private bool _isScanning = true;

    public QrScanViewModel(ILogService logService)
    {
        _log = logService;
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        _log.Debug("QR-Scan abgebrochen.");
        IsScanning = false;
        await Shell.Current.GoToAsync("..");
    }

    public async Task OnBarcodeDetectedAsync(string result)
    {
        if (!IsScanning) return;
        IsScanning = false;

        _log.Info($"QR-Code erkannt: '{result}'.");
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new QrCodeScannedMessage(result));
    }
}
