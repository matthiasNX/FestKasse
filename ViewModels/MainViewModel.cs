using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IDisplayService _displayService;
    private readonly IOrderService _orderService;
    private readonly IOrderHistoryService _orderHistoryService;

    // Internal master list (all articles, unfiltered)
    private readonly List<Article> _allArticles = new();

    [ObservableProperty]
    private ObservableCollection<ArticleTileViewModel> _filteredTiles = new();

    [ObservableProperty]
    private ObservableCollection<ArticleCategoryGroup> _categoryGroups = new();

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
    private string _selectedCategory = "Alle";

    [ObservableProperty]
    private ObservableCollection<string> _categories = new() { "Alle" };

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

    // True when SelectedCategory is "Alle" so group headers are shown
    public bool ShowGroupHeaders => SelectedCategory == "Alle";

    // ─── Denomination / payment panel ────────────────────────────────────────
    [ObservableProperty]
    private bool _isPaymentPanelVisible;

    public ObservableCollection<DenominationTile> NoteTiles { get; } = new();
    public ObservableCollection<DenominationTile> CoinTiles { get; } = new();

    private AppSettings _settings = new();
    private List<Category> _standCategories = new();
    private bool _standSelectionDone = false;
    private string? _cachedLogoBase64;

    public MainViewModel(IDataService dataService, IDisplayService displayService, IOrderService orderService, IOrderHistoryService orderHistoryService)
    {
        _dataService = dataService;
        _displayService = displayService;
        _orderService = orderService;
        _orderHistoryService = orderHistoryService;
        InitDenominationTiles();
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

            var names = stands.Select(s => s.Name).ToArray();
            var choice = await Shell.Current.DisplayActionSheet(
                "Stand auswählen", null, null, names);

            if (!string.IsNullOrEmpty(choice))
            {
                var selected = stands.FirstOrDefault(s => s.Name == choice);
                if (selected != null)
                    await _dataService.SetActiveStandAsync(selected.Id);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Stand-Auswahl Fehler: {ex.Message}");
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var stand = await _dataService.GetActiveStandAsync();
            if (stand == null) return;

            _settings = await _dataService.GetSettingsAsync();
            _standCategories = stand.Categories;
            ActiveStandName = stand.Name;

            _allArticles.Clear();
            _allArticles.AddRange(stand.Articles.OrderBy(a => a.SortOrder).ThenBy(a => a.Description));

            var cats = new ObservableCollection<string> { "Alle" };
            foreach (var category in _standCategories.OrderBy(c => c.SortOrder))
                cats.Add(category.Name);
            Categories = cats;
            SelectedCategory = "Alle";

            await LoadLogoAsync();
            ApplyDisplaySettings();
            ApplyTileSize();
            RebuildFilteredTiles();
            OnPropertyChanged(nameof(ShowCompleteButton));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden: {ex.Message}");
        }
    }

    private async Task LoadLogoAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_settings.LogoBase64))
            {
                if (_settings.LogoBase64 == _cachedLogoBase64)
                    return; // logo unchanged, skip decode

                var bytes = Convert.FromBase64String(_settings.LogoBase64);
                LogoSource = ImageSource.FromStream(() => new MemoryStream(bytes));
                IsLogoVisible = true;
                _cachedLogoBase64 = _settings.LogoBase64;
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
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden des Logos: {ex.Message}");
            LogoSource = null;
            IsLogoVisible = false;
        }
        await Task.CompletedTask;
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
        var size = _settings.TileSize > 0 ? _settings.TileSize : 120;
        TileWidth = size;
        TileHeight = (int)(size * 0.92);
        TileFontSizeDescription = Math.Max(9, size * 13.0 / 120.0);
        TileFontSizeSmall = Math.Max(8, size * 12.0 / 120.0);
    }

    // ─── Tile helpers ──────────────────────────────────────────────────────────

    /// <summary>Rebuilds FilteredTiles and CategoryGroups from the current category filter + cart state.</summary>
    private void RebuildFilteredTiles()
    {
        var cartLookup = CartItems.ToDictionary(c => c.Article.Id, c => c.Quantity);

        var source = SelectedCategory == "Alle"
            ? _allArticles
            : _allArticles.Where(a => a.Category == SelectedCategory).ToList();

        var newTiles = new ObservableCollection<ArticleTileViewModel>(
            source.Select(a =>
            {
                cartLookup.TryGetValue(a.Id, out var qty);
                return new ArticleTileViewModel(a, qty);
            }));
        FilteredTiles = newTiles;

        // Rebuild grouped view
        var categoryOrder = _standCategories.OrderBy(c => c.SortOrder).Select(c => c.Name).ToList();
        var articlesToGroup = SelectedCategory == "Alle"
            ? _allArticles
            : source;
        var grouped = articlesToGroup
            .GroupBy(a => a.Category)
            .OrderBy(g => { var idx = categoryOrder.IndexOf(g.Key); return idx < 0 ? 999 : idx; });

        var newGroups = new ObservableCollection<ArticleCategoryGroup>(
            grouped.Select(g => new ArticleCategoryGroup(g.Key,
                g.Select(a =>
                {
                    cartLookup.TryGetValue(a.Id, out var qty);
                    return new ArticleTileViewModel(a, qty);
                }))));
        CategoryGroups = newGroups;
    }

    /// <summary>Updates only the Quantity on existing tiles (faster than full rebuild).</summary>
    private void UpdateTileQuantities()
    {
        var cartLookup = CartItems.ToDictionary(c => c.Article.Id, c => c.Quantity);

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

    public IEnumerable<Article> GetFilteredArticles()
    {
        if (SelectedCategory == "Alle")
            return _allArticles;
        return _allArticles.Where(a => a.Category == SelectedCategory);
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

        // Build the order record once — shared by both send and persist paths
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

        if (_settings.OrderEnabled)
        {
            if (string.IsNullOrWhiteSpace(_settings.OrderUrl))
            {
                await Shell.Current.DisplayAlert("Hinweis",
                    "Keine Bestell-URL konfiguriert. Bitte in den Einstellungen hinterlegen.", "OK");
            }
            else
            {
                try
                {
                    await _orderService.SendOrderAsync(order, _settings);
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Fehler", $"Bestellung konnte nicht gesendet werden: {ex.Message}", "OK");
                }
            }
        }

        // Persist locally if enabled
        if (_settings.SaveOrdersLocally)
        {
            try { await _orderHistoryService.SaveOrderAsync(order); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Bestellung: {ex.Message}");
            }
        }

        ClearCart();
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
        foreach (var v in new[] { 5m, 10m, 20m, 50m, 100m, 200m })
            NoteTiles.Add(new DenominationTile { Value = v, Label = $"{v:0} €", IsNote = true });

        CoinTiles.Clear();
        foreach (var v in new[] { 2m, 1m, 0.50m, 0.20m, 0.10m, 0.05m, 0.02m, 0.01m })
        {
            var label = v >= 1m ? $"{v:0} €" : $"{v * 100m:0} ct";
            CoinTiles.Add(new DenominationTile { Value = v, Label = label, IsNote = false });
        }
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
}
