using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FestKasse.ViewModels;

[QueryProperty(nameof(QrContent), "content")]
[QueryProperty(nameof(StandNames), "stands")]
public partial class QrDisplayViewModel : ObservableObject
{
    [ObservableProperty]
    private string _qrContent = string.Empty;

    [ObservableProperty]
    private string _standNames = string.Empty;

    [ObservableProperty]
    private bool _isTooLarge;

    partial void OnQrContentChanged(string value)
    {
        // A single QR code in binary mode fits about 2 953 bytes.
        // With Base64 overhead (~4/3) the raw compressed payload limit is ~2 200 bytes.
        // Warn the user when the encoded string exceeds this.
        IsTooLarge = System.Text.Encoding.UTF8.GetByteCount(value) > 2200;
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
