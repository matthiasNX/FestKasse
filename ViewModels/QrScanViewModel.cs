using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FestKasse.Messages;

namespace FestKasse.ViewModels;

public partial class QrScanViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isScanning = true;

    [RelayCommand]
    private async Task CancelAsync()
    {
        IsScanning = false;
        await Shell.Current.GoToAsync("..");
    }

    public async Task OnBarcodeDetectedAsync(string result)
    {
        if (!IsScanning) return;
        IsScanning = false;

        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new QrCodeScannedMessage(result));
    }
}
