using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FestKasse.Messages;
using FestKasse.Models;
using FestKasse.Services;
using FestKasse.Views;

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

public partial class SettingsViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IDisplayService _displayService;
    private readonly IOrderHistoryService _orderHistoryService;

    [ObservableProperty]
    private int _displayTimeoutMinutes;

    [ObservableProperty]
    private string _syncUrl = string.Empty;

    [ObservableProperty]
    private double _tileSize = 120;

    [ObservableProperty]
    private ImageSource? _logoPreview;

    [ObservableProperty]
    private bool _hasLogo;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private bool _ignoreSslErrors;

    [ObservableProperty]
    private bool _isExportSelectionVisible;

    [ObservableProperty]
    private ObservableCollection<StandExportItem> _standExportItems = new();

    [ObservableProperty]
    private string _orderUrl = string.Empty;

    [ObservableProperty]
    private int _orderSendModeIndex;

    [ObservableProperty]
    private bool _orderIgnoreSslErrors;

    [ObservableProperty]
    private bool _orderEnabled;

    [ObservableProperty]
    private bool _saveOrdersLocally;

    [ObservableProperty]
    private int _orderCount;

    public List<string> OrderSendModes { get; } = ["JSON Body (POST)", "URL-Vorlage (GET)"];

    [ObservableProperty]
    private int _selectedTab;

    public bool IsTab0 => SelectedTab == 0;
    public bool IsTab1 => SelectedTab == 1;
    public bool IsTab2 => SelectedTab == 2;

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsTab0));
        OnPropertyChanged(nameof(IsTab1));
        OnPropertyChanged(nameof(IsTab2));
    }

    private AppSettings _settings = new();

    public SettingsViewModel(IDataService dataService, IDisplayService displayService, IOrderHistoryService orderHistoryService)
    {
        _dataService = dataService;
        _displayService = displayService;
        _orderHistoryService = orderHistoryService;
        WeakReferenceMessenger.Default.Register<QrCodeScannedMessage>(this, (_, m) =>
            SyncUrl = m.Value);
    }

    partial void OnTileSizeChanged(double value)
    {
        const double min = 80, step = 6;
        var snapped = Math.Round((value - min) / step) * step + min;
        snapped = Math.Clamp(snapped, 80, 200);
        if (Math.Abs(snapped - value) > 0.01)
            TileSize = snapped;
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        if (int.TryParse(tab, out var index))
            SelectedTab = index;
    }

    public async Task InitializeAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        _settings = await _dataService.GetSettingsAsync();

        DisplayTimeoutMinutes = _settings.DisplayTimeoutMinutes;
        SyncUrl = _settings.SyncUrl ?? string.Empty;
        TileSize = _settings.TileSize > 0 ? _settings.TileSize : 120;
        OrderUrl = _settings.OrderUrl ?? string.Empty;
        OrderSendModeIndex = (int)_settings.OrderSendMode;
        OrderIgnoreSslErrors = _settings.OrderIgnoreSslErrors;
        OrderEnabled = _settings.OrderEnabled;
        SaveOrdersLocally = _settings.SaveOrdersLocally;

        OrderCount = await _orderHistoryService.GetOrderCountAsync();

        await LoadLogoPreviewAsync();
    }

    private async Task LoadLogoPreviewAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_settings.LogoBase64))
            {
                var bytes = Convert.FromBase64String(_settings.LogoBase64);
                LogoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
                HasLogo = true;
            }
            else
            {
                LogoPreview = null;
                HasLogo = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden des Logo-Previews: {ex.Message}");
            LogoPreview = null;
            HasLogo = false;
            await Shell.Current.DisplayAlert("Warnung", "Das Logo konnte nicht geladen werden und wurde zurückgesetzt.", "OK");
            _settings.LogoBase64 = null;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _settings.DisplayTimeoutMinutes = DisplayTimeoutMinutes;
        _settings.SyncUrl = string.IsNullOrWhiteSpace(SyncUrl) ? null : SyncUrl;
        _settings.TileSize = (int)TileSize;
        _settings.OrderUrl = string.IsNullOrWhiteSpace(OrderUrl) ? null : OrderUrl;
        _settings.OrderSendMode = (Models.OrderSendMode)OrderSendModeIndex;
        _settings.OrderIgnoreSslErrors = OrderIgnoreSslErrors;
        _settings.OrderEnabled = OrderEnabled;
        _settings.SaveOrdersLocally = SaveOrdersLocally;

        await _dataService.SaveSettingsAsync(_settings);

        if (DisplayTimeoutMinutes > 0)
        {
            _displayService.KeepScreenOn(DisplayTimeoutMinutes);
        }
        else
        {
            _displayService.AllowScreenOff();
        }

        await Shell.Current.DisplayAlert("Gespeichert", "Einstellungen wurden gespeichert.", "OK");
    }

    [RelayCommand]
    private async Task NavigateToCategoriesAsync()
    {
        await Shell.Current.GoToAsync(nameof(CategoryManagementPage));
    }

    [RelayCommand]
    private async Task SelectLogoAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Logo auswählen"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                _settings.LogoBase64 = Convert.ToBase64String(bytes);
                await LoadLogoPreviewAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Logo konnte nicht geladen werden: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RemoveLogoAsync()
    {
        _settings.LogoBase64 = null;
        LogoPreview = null;
        HasLogo = false;
        await Task.CompletedTask;
    }

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
    private async Task ExportSelectedStandsAsync()
    {
        var selected = StandExportItems.Where(x => x.IsSelected).Select(x => x.Stand.Id).ToList();
        if (selected.Count == 0)
        {
            await Shell.Current.DisplayAlert("Hinweis", "Bitte mindestens einen Stand auswählen.", "OK");
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
            await Shell.Current.DisplayAlert("Fehler", $"Export fehlgeschlagen: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void CancelExport() => IsExportSelectionVisible = false;

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
                await Shell.Current.DisplayAlert("Erfolg", "Stände, Kategorien und Artikel wurden importiert. Einstellungen bleiben unverändert.", "OK");
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Import fehlgeschlagen: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SyncFromUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncUrl))
        {
            await Shell.Current.DisplayAlert("Fehler", "Bitte eine Sync-URL eingeben.", "OK");
            return;
        }

        IsSyncing = true;
        try
        {
            var success = await _dataService.SyncFromUrlAsync(SyncUrl, IgnoreSslErrors);
            if (success)
            {
                await Shell.Current.DisplayAlert("Erfolg", "Stände, Kategorien und Artikel wurden synchronisiert. Einstellungen bleiben unverändert.", "OK");
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Sync fehlgeschlagen: {ex.Message}", "OK");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        await LoadSettingsAsync();
        await Shell.Current.DisplayAlert("Aktualisiert", "Daten wurden neu geladen.", "OK");
    }

    [RelayCommand]
    private async Task ScanSyncUrlAsync()
    {
        await Shell.Current.GoToAsync(nameof(QrScanPage));
    }

    [RelayCommand]
    private async Task ResetArticlesToDefaultAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Artikel zurücksetzen",
            "Alle Artikel werden auf die Standard-Artikel zurückgesetzt. Eigene Änderungen gehen verloren. Fortfahren?",
            "Ja, zurücksetzen",
            "Abbrechen");

        if (!confirmed)
            return;

        try
        {
            await _dataService.ResetToDefaultAsync();
            await Shell.Current.DisplayAlert("Erledigt", "Artikel wurden auf die Standard-Daten zurückgesetzt.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Zurücksetzen fehlgeschlagen: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        try
        {
            var json = await _dataService.ExportSettingsToJsonAsync();
            var fileName = $"FestKasse_settings_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "FestKasse Einstellungen Export",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Einstellungen-Export fehlgeschlagen: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/json", "text/plain" } }
                }),
                PickerTitle = "Einstellungen-JSON auswählen"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                await _dataService.ImportSettingsFromJsonAsync(json);
                await Shell.Current.DisplayAlert("Erfolg", "Einstellungen wurden importiert.", "OK");
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Einstellungen-Import fehlgeschlagen: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ResetSettingsToDefaultAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Einstellungen zurücksetzen",
            "Alle Einstellungen werden auf die Standardwerte zurückgesetzt. Fortfahren?",
            "Ja, zurücksetzen",
            "Abbrechen");

        if (!confirmed)
            return;

        try
        {
            await _dataService.ResetSettingsToDefaultAsync();
            await Shell.Current.DisplayAlert("Erledigt", "Einstellungen wurden auf die Standardwerte zurückgesetzt.", "OK");
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Zurücksetzen fehlgeschlagen: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ClearOrderHistoryAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Bestellverlauf löschen",
            $"Alle {OrderCount} gespeicherten Bestellungen werden unwiderruflich gelöscht. Fortfahren?",
            "Ja, löschen",
            "Abbrechen");

        if (!confirmed) return;

        try
        {
            await _orderHistoryService.ClearAllAsync();
            OrderCount = 0;
            await Shell.Current.DisplayAlert("Erledigt", "Bestellverlauf wurde gelöscht.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Löschen fehlgeschlagen: {ex.Message}", "OK");
        }
    }
}
