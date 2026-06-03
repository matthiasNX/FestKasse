using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;
using FestKasse.Helpers;

namespace FestKasse.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IDisplayService _displayService;
    private readonly IOrderService _orderService;
    private readonly IOrderHistoryService _orderHistoryService;
    private readonly ILogService _log;
    private readonly IOfflineOrderQueueService _offlineOrderQueueService;
    private readonly ICashSessionService _cashSessionService;

    // Internal master list (all articles, unfiltered)
    private readonly List<Article> _allArticles = new();

    [ObservableProperty]
    private RangeObservableCollection<ArticleTileViewModel> _filteredTiles = new();

    [ObservableProperty]
    private RangeObservableCollection<ArticleCategoryGroup> _categoryGroups = new();

    [ObservableProperty]
    private ObservableCollection<CartItem> _cartItems = new();

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private decimal _givenAmount;

    [ObservableProperty]
    private decimal _change;

    public bool HasCartItems => CartItems.Count > 0;

    public bool ShowCompleteButton => HasCartItems;

    [ObservableProperty]
    private ImageSource? _logoSource;

    [ObservableProperty]
    private bool _isLogoVisible;

    [ObservableProperty]
    private string _selectedCategory = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private string _activeStandName = "Kasse";

    // Tile sizing
    [ObservableProperty]
    private int _tileWidth = 120;

    [ObservableProperty]
    private int _tileHeight = 110;

    [ObservableProperty]
    private double _tileFontSizeDescription = 13;

    [ObservableProperty]
    private double _tileFontSizeSmall = 12;

    // Special sentinel category representing "show all"
    private static Category AllCategoriesEntry =>
        new() { Id = string.Empty, Name = LocalizationService.Instance["Main_AllCategories"] };

    // True when no specific category is selected ("All") AND the setting is enabled
    public bool ShowGroupHeaders => string.IsNullOrEmpty(SelectedCategory) && _settings.ShowCategoryGroupHeaders;

    // ─── Denomination / payment panel ────────────────────────────────────────
    [ObservableProperty]
    private bool _isPaymentPanelVisible;

    public ObservableCollection<DenominationTile> NoteTiles { get; } = new();
    public ObservableCollection<DenominationTile> CoinTiles { get; } = new();

    private AppSettings _settings = new();
    private List<Category> _standCategories = new();
    private bool _standSelectionDone = false;
    private string? _cachedLogoBase64;

    public MainViewModel(IDataService dataService, IDisplayService displayService, IOrderService orderService, IOrderHistoryService orderHistoryService, ILogService logService, IOfflineOrderQueueService offlineOrderQueueService, ICashSessionService cashSessionService)
    {
        _dataService = dataService;
        _displayService = displayService;
        _orderService = orderService;
        _orderHistoryService = orderHistoryService;
        _log = logService;
        _offlineOrderQueueService = offlineOrderQueueService;
        _cashSessionService = cashSessionService;
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
        InitDenominationTiles();
        _log.Debug("MainViewModel initialisiert.");
    }

    public async Task InitializeAsync()
    {
        // On first call: if multiple stands exist, ask user to select one
        if (!_standSelectionDone)
        {
            _standSelectionDone = true;
            await PromptStandSelectionIfNeededAsync();
        }
        await LoadDataAsync();
    }

    private async Task PromptStandSelectionIfNeededAsync()
    {
        try
        {
            var stands = await _dataService.GetStandsAsync();
            if (stands.Count <= 1) return;

            _log.Info($"Multiple stands available ({stands.Count}) – asking user to select.");
            var names = stands.Select(s => s.Name).ToArray();
            var choice = await Shell.Current.DisplayActionSheet(
                LocalizationService.Instance["Stand_Select_Title"], null, null, names);

            if (!string.IsNullOrEmpty(choice))
            {
                var selected = stands.FirstOrDefault(s => s.Name == choice);
                if (selected != null)
                {
                    _log.Info($"User selected stand '{choice}'.");
                    await _dataService.SetActiveStandAsync(selected.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Fehler bei der Stand-Auswahl.");
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _log.Debug("Lade Kassendaten.");
            var stand = await _dataService.GetActiveStandAsync();
            if (stand == null)
            {
                _log.Warning("LoadDataAsync: Kein aktiver Stand gefunden.");
                return;
            }

            _settings = await _dataService.GetSettingsAsync();
            _standCategories = stand.Categories;
            ActiveStandName = stand.Name;

            _allArticles.Clear();
            _allArticles.AddRange(stand.Articles.OrderBy(a => a.SortOrder).ThenBy(a => a.Description));

            _log.Info($"Kassendaten geladen: Stand='{stand.Name}', Artikel={_allArticles.Count}, Kategorien={_standCategories.Count}.");

            var cats = new ObservableCollection<Category> { AllCategoriesEntry };
            foreach (var category in _standCategories.OrderBy(c => c.SortOrder))
                cats.Add(category);
            Categories = cats;
            SelectedCategory = string.Empty;

            await LoadLogoAsync();
            ApplyDisplaySettings();
            ApplyTileSize();
            InitDenominationTiles();
            RebuildFilteredTiles();
            OnPropertyChanged(nameof(ShowCompleteButton));
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Fehler beim Laden der Kassendaten.");
        }
    }

    private async Task LoadLogoAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_settings.LogoBase64))
            {
                if (_settings.LogoBase64 == _cachedLogoBase64)
                    return;

                var bytes = Convert.FromBase64String(_settings.LogoBase64);
                LogoSource = ImageSource.FromStream(() => new MemoryStream(bytes));
                IsLogoVisible = true;
                _cachedLogoBase64 = _settings.LogoBase64;
                _log.Debug("Logo aus Base64 geladen und angezeigt.");
            }
            else
            {
                LogoSource = null;
                IsLogoVisible = false;
                _cachedLogoBase64 = null;
            }
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Fehler beim Dekodieren des Logos.");
            LogoSource = null;
            IsLogoVisible = false;
        }
    }

    private void ApplyDisplaySettings()
    {
        if (_settings.DisplayTimeoutMinutes > 0)
        {
            _displayService.KeepScreenOn(_settings.DisplayTimeoutMinutes);
        }
    }

    private void ApplyTileSize()
    {
        var size = _settings.TileSize > 0 ? _settings.TileSize : AppConstants.DefaultTileSize;
        TileWidth = size;
        TileHeight = (int)(size * 0.92);
        TileFontSizeDescription = Math.Max(9, size * 13.0 / 120.0);
        TileFontSizeSmall = Math.Max(8, size * 12.0 / 120.0);
    }

    // ─── Tile helpers ──────────────────────────────────────────────────────────

    /// <summary>Rebuilds FilteredTiles and CategoryGroups from the current category filter + cart state.</summary>
    private void RebuildFilteredTiles()
    {
        var cartLookup = BuildCartLookup();

        var source = string.IsNullOrEmpty(SelectedCategory)
            ? _allArticles
            : _allArticles.Where(a => a.CategoryId == SelectedCategory).ToList();

        // Build the new tile list first, then swap in one shot (single Reset notification).
        var newTiles = source.Select(a =>
        {
            cartLookup.TryGetValue(a.Id, out var qty);
            return new ArticleTileViewModel(a, qty);
        });
        FilteredTiles.ReplaceRange(newTiles);

        // Rebuild grouped view
        var categoryOrder = _standCategories.OrderBy(c => c.SortOrder).Select(c => c.Id).ToList();
        var articlesToGroup = string.IsNullOrEmpty(SelectedCategory) ? _allArticles : source;
        var newGroups = articlesToGroup
            .GroupBy(a => a.CategoryId)
            .OrderBy(g => { var idx = categoryOrder.IndexOf(g.Key); return idx < 0 ? 999 : idx; })
            .Select(g =>
            {
                var name = _standCategories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? g.Key;
                return new ArticleCategoryGroup(name,
                    g.Select(a =>
                    {
                        cartLookup.TryGetValue(a.Id, out var qty);
                        return new ArticleTileViewModel(a, qty);
                    }));
            });
        CategoryGroups.ReplaceRange(newGroups);
    }

    /// <summary>Updates only the Quantity on existing tiles (faster than full rebuild).</summary>
    private void UpdateTileQuantities()
    {
        var cartLookup = BuildCartLookup();

        foreach (var tile in FilteredTiles)
        {
            cartLookup.TryGetValue(tile.Article.Id, out var qty);
            tile.Quantity = qty;
        }

        foreach (var group in CategoryGroups)
            foreach (var tile in group)
            {
                cartLookup.TryGetValue(tile.Article.Id, out var qty);
                tile.Quantity = qty;
            }
    }

    /// <summary>Builds a dictionary from article ID to cart quantity for fast tile updates.</summary>
    private Dictionary<string, int> BuildCartLookup()
        => CartItems.ToDictionary(c => c.Article.Id, c => c.Quantity);

    public IEnumerable<Article> GetFilteredArticles()
    {
        if (string.IsNullOrEmpty(SelectedCategory))
            return _allArticles;
        return _allArticles.Where(a => a.CategoryId == SelectedCategory);
    }

    public int GetArticleQuantity(string articleId)
    {
        var item = CartItems.FirstOrDefault(c => c.Article.Id == articleId);
        return item?.Quantity ?? 0;
    }

    [RelayCommand]
    private void AddToCart(Article article)
    {
        var existingItem = CartItems.FirstOrDefault(c => c.Article.Id == article.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            CartItems.Add(new CartItem(article));
        }
        _log.Debug($"Cart: '{article.Description}' added (qty={CartItems.FirstOrDefault(c=>c.Article.Id==article.Id)?.Quantity ?? 1}, total={CartItems.Count} item(s)).");
        CalculateTotal();
        UpdateTileQuantities();
        OnPropertyChanged(nameof(CartItems));
        OnPropertyChanged(nameof(HasCartItems)); OnPropertyChanged(nameof(ShowCompleteButton));
    }

    [RelayCommand]
    private void RemoveFromCart(Article article)
    {
        var existingItem = CartItems.FirstOrDefault(c => c.Article.Id == article.Id);
        if (existingItem != null)
        {
            existingItem.Quantity--;
            if (existingItem.Quantity <= 0)
            {
                CartItems.Remove(existingItem);
            }
        }
        CalculateTotal();
        UpdateTileQuantities();
        OnPropertyChanged(nameof(CartItems));
        OnPropertyChanged(nameof(HasCartItems)); OnPropertyChanged(nameof(ShowCompleteButton));
    }

    [RelayCommand]
    private void RemoveCartItem(CartItem item)
    {
        CartItems.Remove(item);
        CalculateTotal();
        UpdateTileQuantities();
        OnPropertyChanged(nameof(HasCartItems)); OnPropertyChanged(nameof(ShowCompleteButton));
    }

    [RelayCommand]
    private void ClearCart()
    {
        _log.Debug("Warenkorb geleert.");
        CartItems.Clear();
        GivenAmount = 0;
        Change = 0;
        foreach (var t in NoteTiles) t.Count = 0;
        foreach (var t in CoinTiles) t.Count = 0;
        CalculateTotal();
        UpdateTileQuantities();
        OnPropertyChanged(nameof(HasCartItems)); OnPropertyChanged(nameof(ShowCompleteButton));
    }

    [RelayCommand]
    private async Task CompleteOrderAsync()
    {
        if (CartItems.Count == 0) return;

        var order = new OrderRecord
        {
            Timestamp = DateTime.UtcNow,
            StandName = ActiveStandName,
            Total = Total,
            Items = CartItems.Select(c => new OrderItemRecord
            {
                ArticleName = c.Article.Description,
                Quantity = c.Quantity,
                UnitPrice = c.Article.Price,
                LineTotal = c.Total
            }).ToList()
        };

        _log.Info($"Order completed: stand='{ActiveStandName}', items={order.Items.Count}, total={Total:F2}€.");

        bool orderSentOrQueued = true;

        if (_settings.OrderEnabled)
        {
            if (string.IsNullOrWhiteSpace(_settings.OrderUrl))
            {
                _log.Warning("Order submission active but no URL configured.");
                var loc = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(loc["Common_Info"], loc["Alert_Order_NoUrl"], loc["Common_OK"]);
                // Missing URL is a configuration error – still clear cart so user isn't stuck
            }
            else
            {
                try
                {
                    var sent = await _orderService.SendOrderAsync(order, _settings);
                    if (!sent)
                    {
                        _log.Warning("Order not confirmed (SendOrderAsync returned false).");
                        await _offlineOrderQueueService.EnqueueAsync(order);
                        _log.Info("Order queued for offline retry.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Exception(ex, "Error sending order.");
                    var loc2 = LocalizationService.Instance;
                    await Shell.Current.DisplayAlert(loc2["Common_Error"], loc2.Format("Alert_Order_SendError", ex.Message), loc2["Common_OK"]);
                    orderSentOrQueued = false;
                }
            }
        }

        if (_settings.SaveOrdersLocally)
        {
            try { await _orderHistoryService.SaveOrderAsync(order); }
            catch (Exception ex)
            {
                _log.Exception(ex, "Error saving order locally.");
            }
        }

        // Record in the active cash session
        try { await _cashSessionService.RecordOrderAsync(order.Total); }
        catch (Exception ex) { _log.Exception(ex, "Error recording order in cash session."); }

        // Share receipt if enabled
        if (_settings.ShareReceiptAfterOrder)
        {
            try { await ShareReceiptAsync(order); }
            catch (Exception ex) { _log.Exception(ex, "Error sharing receipt."); }
        }

        if (orderSentOrQueued)
            ClearCart();
    }

    private static async Task ShareReceiptAsync(OrderRecord order)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══════════════════════════");
        sb.AppendLine($"  {order.StandName}");
        sb.AppendLine($"  {order.Timestamp.ToLocalTime():dd.MM.yyyy HH:mm}");
        sb.AppendLine("═══════════════════════════");
        foreach (var item in order.Items)
            sb.AppendLine($"  {item.Quantity,2}x  {item.ArticleName,-18} {item.LineTotal,7:F2} €");
        sb.AppendLine("───────────────────────────");
        sb.AppendLine($"  Gesamt:              {order.Total,7:F2} €");
        sb.AppendLine("═══════════════════════════");

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Beleg",
            Text = sb.ToString()
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    private void CalculateTotal()
    {
        Total = CartItems.Sum(c => c.Total);
        CalculateChange();
    }

    private void CalculateChange()
    {
        Change = GivenAmount - Total;
    }

    [RelayCommand]
    private void SelectCategory(string category)
    {
        SelectedCategory = category;
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        OnPropertyChanged(nameof(ShowGroupHeaders));
        RebuildFilteredTiles();
    }

    // ─── Denomination / payment panel ────────────────────────────────────────

    private void InitDenominationTiles()
    {
        NoteTiles.Clear();
        foreach (var v in _settings.Notes.OrderBy(x => x))
            NoteTiles.Add(new DenominationTile { Value = v, Label = DenominationFormatter.MakeLabel(v), IsNote = true });

        CoinTiles.Clear();
        foreach (var v in _settings.Coins.OrderByDescending(x => x))
            CoinTiles.Add(new DenominationTile { Value = v, Label = DenominationFormatter.MakeLabel(v), IsNote = false });
    }

    [RelayCommand]
    private void TogglePaymentPanel()
    {
        IsPaymentPanelVisible = !IsPaymentPanelVisible;
    }

    [RelayCommand]
    private void AddDenomination(DenominationTile tile)
    {
        tile.Count++;
        SyncGivenAmountFromTiles();
    }

    [RelayCommand]
    private void RemoveDenomination(DenominationTile tile)
    {
        if (tile.Count > 0)
        {
            tile.Count--;
            SyncGivenAmountFromTiles();
        }
    }

    [RelayCommand]
    private void ResetPayment()
    {
        foreach (var t in NoteTiles) t.Count = 0;
        foreach (var t in CoinTiles) t.Count = 0;
        GivenAmount = 0;
    }

    private void SyncGivenAmountFromTiles()
    {
        GivenAmount = NoteTiles.Sum(t => t.Value * t.Count)
                    + CoinTiles.Sum(t => t.Value * t.Count);
    }

    // When GivenAmount is typed manually, keep tiles in sync (reset counts)
    partial void OnGivenAmountChanged(decimal value)
    {
        CalculateChange();
    }

    // ── Connectivity change → retry queued offline orders ────────────────

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            _log.Info("Connectivity restored – retrying offline order queue.");
            try { await _offlineOrderQueueService.RetryAllAsync(); }
            catch (Exception ex) { _log.Exception(ex, "Offline queue retry failed."); }
        }
    }
}
