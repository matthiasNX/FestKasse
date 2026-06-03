using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FestKasse.Helpers;
using FestKasse.Messages;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

/// <summary>
/// Settings screen ViewModel — split across several partial-class files:
/// <list type="bullet">
///   <item><term>SettingsViewModel.cs</term><description>Core: DI, properties, load/save</description></item>
///   <item><term>SettingsViewModel.Denomination.cs</term><description>Note/coin commands</description></item>
///   <item><term>SettingsViewModel.Logo.cs</term><description>Logo pick/remove/preview</description></item>
///   <item><term>SettingsViewModel.Export.cs</term><description>Master-data export, import, sync</description></item>
///   <item><term>SettingsViewModel.Admin.cs</term><description>Settings IO, resets, history, navigation</description></item>
///   <item><term>SettingsViewModelModels.cs</term><description>StandExportItem, DenominationItem</description></item>
/// </list>
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    // ── DI ────────────────────────────────────────────────────────────────

    private readonly IDataService _dataService;
    private readonly IDisplayService _displayService;
    private readonly IOrderHistoryService _orderHistoryService;
    private readonly ILogService _logService;

    // ── Observable properties ─────────────────────────────────────────────

    [ObservableProperty] private int _displayTimeoutMinutes;
    [ObservableProperty] private string _syncUrl = string.Empty;
    [ObservableProperty] private double _tileSize = 120;
    [ObservableProperty] private ImageSource? _logoPreview;
    [ObservableProperty] private bool _hasLogo;
    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private bool _ignoreSslErrors;
    [ObservableProperty] private bool _isExportSelectionVisible;
    [ObservableProperty] private ObservableCollection<StandExportItem> _standExportItems = new();
    [ObservableProperty] private string _orderUrl = string.Empty;
    [ObservableProperty] private int _orderSendModeIndex;
    [ObservableProperty] private bool _orderIgnoreSslErrors;
    [ObservableProperty] private bool _orderEnabled;
    [ObservableProperty] private bool _saveOrdersLocally;
    [ObservableProperty] private bool _shareReceiptAfterOrder;
    [ObservableProperty] private bool _hapticVibrationEnabled = true;
    [ObservableProperty] private bool _hapticSoundEnabled;
    [ObservableProperty] private bool _showCategoryGroupHeaders = true;
    [ObservableProperty] private string _newNoteEntry = string.Empty;
    [ObservableProperty] private string _newCoinEntry = string.Empty;
    [ObservableProperty] private string _selectedLogLevel = "Information";
    [ObservableProperty] private string _selectedLanguage = "system";
    [ObservableProperty] private int _orderCount;

    // ── Collections ───────────────────────────────────────────────────────

    public ObservableCollection<DenominationItem> NoteItems { get; } = new();
    public ObservableCollection<DenominationItem> CoinItems { get; } = new();

    // ── Static lookup lists ───────────────────────────────────────────────

    public List<string> LogLevels { get; } = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];
    public List<string> OrderSendModes { get; } = ["JSON Body (POST)", "URL-Vorlage (GET)"];

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

    // ── Internal state ────────────────────────────────────────────────────

    private AppSettings _settings = new();

    /// <summary>Tracks which URL field triggered the last QR scan.</summary>
    private string _qrScanTarget = "sync";

    // ── Constructor ───────────────────────────────────────────────────────

    public SettingsViewModel(
        IDataService dataService,
        IDisplayService displayService,
        IOrderHistoryService orderHistoryService,
        ILogService logService)
    {
        _dataService = dataService;
        _displayService = displayService;
        _orderHistoryService = orderHistoryService;
        _logService = logService;

        WeakReferenceMessenger.Default.Register<QrCodeScannedMessage>(this, async (_, m) =>
        {
            if (_qrScanTarget == "order")
                OrderUrl = m.Value;
            else if (_qrScanTarget == "masterdata")
                await ImportFromQrDataAsync(m.Value);
            else
                SyncUrl = m.Value;
        });
    }

    // ── Property-change hooks ─────────────────────────────────────────────

    partial void OnTileSizeChanged(double value)
    {
        const double min = 80, step = 6;
        var snapped = Math.Round((value - min) / step) * step + min;
        snapped = Math.Clamp(snapped, 80, 200);
        if (Math.Abs(snapped - value) > 0.01)
            TileSize = snapped;
    }

    // ── Initialisation ────────────────────────────────────────────────────

    public async Task InitializeAsync() => await LoadSettingsAsync();

    private async Task LoadSettingsAsync()
    {
        _settings = await _dataService.GetSettingsAsync();

        DisplayTimeoutMinutes    = _settings.DisplayTimeoutMinutes;
        SyncUrl                  = _settings.SyncUrl ?? string.Empty;
        TileSize                 = _settings.TileSize > 0 ? _settings.TileSize : AppConstants.DefaultTileSize;
        OrderUrl                 = _settings.OrderUrl ?? string.Empty;
        OrderSendModeIndex       = (int)_settings.OrderSendMode;
        OrderIgnoreSslErrors     = _settings.OrderIgnoreSslErrors;
        OrderEnabled             = _settings.OrderEnabled;
        SaveOrdersLocally        = _settings.SaveOrdersLocally;
        ShareReceiptAfterOrder   = _settings.ShareReceiptAfterOrder;
        ShowCategoryGroupHeaders = _settings.ShowCategoryGroupHeaders;
        HapticVibrationEnabled   = _settings.HapticVibrationEnabled;
        HapticSoundEnabled       = _settings.HapticSoundEnabled;

        NoteItems.Clear();
        foreach (var v in _settings.Notes.OrderBy(x => x))
            NoteItems.Add(new DenominationItem { Value = v, Label = DenominationItem.MakeLabel(v) });

        CoinItems.Clear();
        foreach (var v in _settings.Coins.OrderByDescending(x => x))
            CoinItems.Add(new DenominationItem { Value = v, Label = DenominationItem.MakeLabel(v) });

        NewNoteEntry     = string.Empty;
        NewCoinEntry     = string.Empty;
        OrderCount       = await _orderHistoryService.GetOrderCountAsync();
        SelectedLogLevel = _settings.LogLevel ?? "Information";
        SelectedLanguage = _settings.Language ?? "system";

        await LoadLogoPreviewAsync();
    }

    // ── Save ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _settings.DisplayTimeoutMinutes    = DisplayTimeoutMinutes;
        _settings.SyncUrl                  = string.IsNullOrWhiteSpace(SyncUrl) ? null : SyncUrl;
        _settings.TileSize                 = (int)TileSize;
        _settings.OrderUrl                 = string.IsNullOrWhiteSpace(OrderUrl) ? null : OrderUrl;
        _settings.OrderSendMode            = (Models.OrderSendMode)OrderSendModeIndex;
        _settings.OrderIgnoreSslErrors     = OrderIgnoreSslErrors;
        _settings.OrderEnabled             = OrderEnabled;
        _settings.SaveOrdersLocally        = SaveOrdersLocally;
        _settings.ShareReceiptAfterOrder   = ShareReceiptAfterOrder;
        _settings.ShowCategoryGroupHeaders = ShowCategoryGroupHeaders;
        _settings.HapticVibrationEnabled   = HapticVibrationEnabled;
        _settings.HapticSoundEnabled       = HapticSoundEnabled;
        _settings.Notes    = NoteItems.Select(i => i.Value).ToList();
        _settings.Coins    = CoinItems.Select(i => i.Value).ToList();
        _settings.LogLevel = SelectedLogLevel;
        _settings.Language = SelectedLanguage;

        await _dataService.SaveSettingsAsync(_settings);

        _logService.SetLogLevel(SelectedLogLevel);
        LocalizationService.Instance.SetLanguage(SelectedLanguage);
        _logService.Info($"Settings saved: Timeout={DisplayTimeoutMinutes}min, TileSize={TileSize}dp, " +
                         $"LogLevel={SelectedLogLevel}, Language={SelectedLanguage}.");

        if (DisplayTimeoutMinutes > 0)
            _displayService.KeepScreenOn(DisplayTimeoutMinutes);
        else
            _displayService.AllowScreenOff();

        var loc = LocalizationService.Instance;
        await Shell.Current.DisplayAlert(loc["Common_Saved"], loc["Alert_Settings_Saved"], loc["Common_OK"]);
    }
}
