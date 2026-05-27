using FestKasse.ViewModels;
using ZXing.Net.Maui;

namespace FestKasse.Views;

public partial class QrScanPage : ContentPage
{
    private readonly QrScanViewModel _viewModel;

    public QrScanPage(QrScanViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.IsScanning = true;
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var first = e.Results?.FirstOrDefault();
        if (first is null) return;

        var value = first.Value;
        MainThread.BeginInvokeOnMainThread(async () =>
            await _viewModel.OnBarcodeDetectedAsync(value));
    }
}
