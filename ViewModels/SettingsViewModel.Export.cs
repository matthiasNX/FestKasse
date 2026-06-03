using System.IO.Compression;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Services;
using FestKasse.Views;

namespace FestKasse.ViewModels;

public partial class SettingsViewModel
{
    // ── Master-data export/import/sync ────────────────────────────────────

    [RelayCommand]
    private async Task ShowExportSelectionAsync()
    {
        var stands = await _dataService.GetStandsAsync();
        StandExportItems.Clear();
        foreach (var s in stands)
            StandExportItems.Add(new StandExportItem(s));
        IsExportSelectionVisible = true;
    }

    [RelayCommand]
    private void CancelExport() => IsExportSelectionVisible = false;

    [RelayCommand]
    private async Task ExportSelectedStandsAsync()
    {
        var selected = StandExportItems.Where(x => x.IsSelected).Select(x => x.Stand.Id).ToList();
        if (selected.Count == 0)
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Info"], loc["Alert_Export_NoStand"], loc["Common_OK"]);
            return;
        }

        try
        {
            var json = await _dataService.ExportToJsonAsync(selected);
            var fileName = $"FestKasse_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);

            IsExportSelectionVisible = false;

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "FestKasse Export",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error exporting master data.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_Export_Error", ex.Message), loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ExportSelectedStandsAsQrAsync()
    {
        var selected = StandExportItems.Where(x => x.IsSelected).Select(x => x.Stand.Id).ToList();
        if (selected.Count == 0)
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Info"], loc["Alert_Export_NoStand"], loc["Common_OK"]);
            return;
        }

        try
        {
            var json = await _dataService.ExportToJsonAsync(selected);
            var qrContent = CompressToQrPayload(json);
            var standNames = string.Join(", ", StandExportItems.Where(x => x.IsSelected).Select(x => x.Name));

            IsExportSelectionVisible = false;

            await Shell.Current.GoToAsync(
                $"{nameof(QrDisplayPage)}?content={Uri.EscapeDataString(qrContent)}&stands={Uri.EscapeDataString(standNames)}");
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error generating QR export.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_Export_Error", ex.Message), loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/json", "text/plain" } }
                }),
                PickerTitle = "JSON-Datei auswählen"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                await _dataService.ImportFromJsonAsync(json);
                _logService.Info($"Master data successfully imported from file '{result.FileName}'.");
                var loc = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(loc["Common_Success"], loc["Alert_Import_Success"], loc["Common_OK"]);
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error importing master data from file.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_Import_Error", ex.Message), loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ImportFromQrAsync()
    {
        _qrScanTarget = "masterdata";
        var hint = LocalizationService.Instance["QrScan_Hint_MasterData"];
        await Shell.Current.GoToAsync($"{nameof(QrScanPage)}?hint={Uri.EscapeDataString(hint)}");
    }

    private async Task ImportFromQrDataAsync(string qrValue)
    {
        try
        {
            string json;
            if (qrValue.StartsWith("FK:", StringComparison.Ordinal))
            {
                var compressed = Convert.FromBase64String(qrValue[3..]);
                using var ms = new MemoryStream(compressed);
                using var gz = new GZipStream(ms, CompressionMode.Decompress);
                using var reader = new StreamReader(gz, Encoding.UTF8);
                json = await reader.ReadToEndAsync();
            }
            else
            {
                json = qrValue;
            }

            await _dataService.ImportFromJsonAsync(json);
            _logService.Info("Master data successfully imported from QR code.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Success"], loc["Alert_Import_Success"], loc["Common_OK"]);
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error importing master data from QR code.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_Import_Error", ex.Message), loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task SyncFromUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncUrl))
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc["Alert_Sync_NoUrl"], loc["Common_OK"]);
            return;
        }

        IsSyncing = true;
        try
        {
            _logService.Info($"Starting data sync from URL: {SyncUrl}.");
            var success = await _dataService.SyncFromUrlAsync(SyncUrl, IgnoreSslErrors);
            if (success)
            {
                _logService.Info("Data sync completed successfully.");
                var loc = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(loc["Common_Success"], loc["Alert_Sync_Success"], loc["Common_OK"]);
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, $"Error during data sync from '{SyncUrl}'.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_Sync_Error", ex.Message), loc["Common_OK"]);
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task ScanSyncUrlAsync()
    {
        _qrScanTarget = "sync";
        await Shell.Current.GoToAsync(nameof(QrScanPage));
    }

    [RelayCommand]
    private async Task ScanOrderUrlAsync()
    {
        _qrScanTarget = "order";
        await Shell.Current.GoToAsync(nameof(QrScanPage));
    }

    private static string CompressToQrPayload(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
            gz.Write(bytes, 0, bytes.Length);
        return "FK:" + Convert.ToBase64String(ms.ToArray());
    }
}
