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
    private readonly ILogService _logService;

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
    private string _selectedLogLevel = "Information";

    public List<string> LogLevels { get; } = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    // Language selection: keys are "system", "de", "en"
    [ObservableProperty]
    private string _selectedLanguage = "system";

    public List<(string Key, string Display)> LanguageOptions { get; } =
    [
        ("system", "Systemsprache / System language"),
        ("de", "Deutsch"),
        ("en", "English")
    ];

    public List<string> LanguageDisplayNames => LanguageOptions.Select(l => l.Display).ToList();

    public int SelectedLanguageIndex
    {
        get => Math.Max(0, LanguageOptions.FindIndex(l => l.Key == SelectedLanguage));
        set
        {
            if (value >= 0 && value < LanguageOptions.Count)
                SelectedLanguage = LanguageOptions[value].Key;
        }
    }

    [ObservableProperty]
    private int _orderCount;

    public List<string> OrderSendModes { get; } = ["JSON Body (POST)", "URL-Vorlage (GET)"];

    private AppSettings _settings = new();

    // Tracks which URL field triggered the QR scan
    private string _qrScanTarget = "sync";

    public SettingsViewModel(IDataService dataService, IDisplayService displayService, IOrderHistoryService orderHistoryService, ILogService logService)
    {
        _dataService = dataService;
        _displayService = displayService;
        _orderHistoryService = orderHistoryService;
        _logService = logService;
        WeakReferenceMessenger.Default.Register<QrCodeScannedMessage>(this, (_, m) =>
        {
            if (_qrScanTarget == "order")
                OrderUrl = m.Value;
            else
                SyncUrl = m.Value;
        });
    }

    partial void OnTileSizeChanged(double value)
    {
        const double min = 80, step = 6;
        var snapped = Math.Round((value - min) / step) * step + min;
        snapped = Math.Clamp(snapped, 80, 200);
        if (Math.Abs(snapped - value) > 0.01)
            TileSize = snapped;
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
        SelectedLogLevel = _settings.LogLevel ?? "Information";
        SelectedLanguage = _settings.Language ?? "system";

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
            _logService.Exception(ex, "Error decoding logo preview.");
            LogoPreview = null;
            HasLogo = false;
            var _loc0 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_loc0["Common_Warning"], _loc0["Alert_Logo_LoadError"], _loc0["Common_OK"]);
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
        _settings.LogLevel = SelectedLogLevel;
        _settings.Language = SelectedLanguage;

        await _dataService.SaveSettingsAsync(_settings);

        _logService.SetLogLevel(SelectedLogLevel);
        LocalizationService.Instance.SetLanguage(SelectedLanguage);
        _logService.Info($"Settings saved: Timeout={DisplayTimeoutMinutes}min, TileSize={TileSize}dp, LogLevel={SelectedLogLevel}, Language={SelectedLanguage}.");

        if (DisplayTimeoutMinutes > 0)
        {
            _displayService.KeepScreenOn(DisplayTimeoutMinutes);
        }
        else
        {
            _displayService.AllowScreenOff();
        }

        var loc = LocalizationService.Instance;
        await Shell.Current.DisplayAlert(loc["Common_Saved"], loc["Alert_Settings_Saved"], loc["Common_OK"]);
    }

    [RelayCommand]
    private async Task NavigateToLogViewerAsync()
    {
        await Shell.Current.GoToAsync(nameof(LogViewerPage));
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
            _logService.Exception(ex, "Error picking logo.");
            var _loc1 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_loc1["Common_Error"], _loc1.Format("Alert_Logo_PickError", ex.Message), _loc1["Common_OK"]);
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
            var _loc2 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_loc2["Common_Info"], _loc2["Alert_Export_NoStand"], _loc2["Common_OK"]);
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
            var _loc3 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_loc3["Common_Error"], _loc3.Format("Alert_Export_Error", ex.Message), _loc3["Common_OK"]);
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
                _logService.Info($"Master data successfully imported from file '{result.FileName}'.");
                var _loc4 = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(_loc4["Common_Success"], _loc4["Alert_Import_Success"], _loc4["Common_OK"]);
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error importing master data from file.");
            var _loc5 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_loc5["Common_Error"], _loc5.Format("Alert_Import_Error", ex.Message), _loc5["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task SyncFromUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncUrl))
        {
            var _loc6 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_loc6["Common_Error"], _loc6["Alert_Sync_NoUrl"], _loc6["Common_OK"]);
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
                var _loc7 = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(_loc7["Common_Success"], _loc7["Alert_Sync_Success"], _loc7["Common_OK"]);
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, $"Error during data sync from '{SyncUrl}'.");
            var _loc8 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_loc8["Common_Error"], _loc8.Format("Alert_Sync_Error", ex.Message), _loc8["Common_OK"]);
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
        var _loc9 = LocalizationService.Instance;
        await Shell.Current.DisplayAlert(_loc9["Common_Updated"], _loc9["Alert_Settings_Updated"], _loc9["Common_OK"]);
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

    [RelayCommand]
    private async Task ResetArticlesToDefaultAsync()
    {
        var _locRA = LocalizationService.Instance;
        var confirmed = await Shell.Current.DisplayAlert(
            _locRA["Settings_ResetArticles_Confirm_Title"],
            _locRA["Settings_ResetArticles_Confirm_Msg"],
            _locRA["Settings_ResetArticles_Confirm_Yes"],
            _locRA["Common_Cancel"]);

        if (!confirmed)
            return;

        try
        {
            await _dataService.ResetToDefaultAsync();
            _logService.Info("Master data reset to defaults.");
            var _locRAS = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locRAS["Common_Done"], _locRAS["Alert_ResetArticles_Done"], _locRAS["Common_OK"]);
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error resetting master data.");
            var _locRAE = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locRAE["Common_Error"], _locRAE.Format("Alert_ResetArticles_Error", ex.Message), _locRAE["Common_OK"]);
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
            _logService.Exception(ex, "Error exporting settings.");
            var _locES = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locES["Common_Error"], _locES.Format("Alert_ExportSettings_Error", ex.Message), _locES["Common_OK"]);
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
                _logService.Info($"Settings imported from file '{result.FileName}'.");
                var _locIS = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(_locIS["Common_Success"], _locIS["Alert_ImportSettings_Success"], _locIS["Common_OK"]);
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error importing settings.");
            var _locISE = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locISE["Common_Error"], _locISE.Format("Alert_ImportSettings_Error", ex.Message), _locISE["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ResetSettingsToDefaultAsync()
    {
        var _locRS = LocalizationService.Instance;
        var confirmed = await Shell.Current.DisplayAlert(
            _locRS["Settings_ResetSettings_Confirm_Title"],
            _locRS["Settings_ResetSettings_Confirm_Msg"],
            _locRS["Settings_ResetSettings_Confirm_Yes"],
            _locRS["Common_Cancel"]);

        if (!confirmed)
            return;

        try
        {
            await _dataService.ResetSettingsToDefaultAsync();
            _logService.Info("Settings reset to defaults.");
            var _locRSS = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locRSS["Common_Done"], _locRSS["Alert_ResetSettings_Done"], _locRSS["Common_OK"]);
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error resetting settings.");
            var _locRSE = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locRSE["Common_Error"], _locRSE.Format("Alert_ResetSettings_Error", ex.Message), _locRSE["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ClearOrderHistoryAsync()
    {
        var _locCH = LocalizationService.Instance;
        var confirmed = await Shell.Current.DisplayAlert(
            _locCH["Settings_OrderHistory_Clear_Confirm_Title"],
            _locCH.Format("Alert_ClearHistory_Confirm_Msg", OrderCount),
            _locCH["Settings_OrderHistory_Clear_Confirm_Yes"],
            _locCH["Common_Cancel"]);

        if (!confirmed) return;

        try
        {
            await _orderHistoryService.ClearAllAsync();
            OrderCount = 0;
            _logService.Info("Order history cleared manually.");
            var _locCHS = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locCHS["Common_Done"], _locCHS["Alert_ClearHistory_Done"], _locCHS["Common_OK"]);
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error clearing order history.");
            var _locCHE = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(_locCHE["Common_Error"], _locCHE.Format("Alert_ClearHistory_Error", ex.Message), _locCHE["Common_OK"]);
        }
    }
}
