using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

/// <summary>A flat order row plus its expanded line items, for display.</summary>
public partial class OrderHistoryItem : ObservableObject
{
    public OrderRecord Order { get; }
    public List<OrderItemRecord> Items { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public string TimestampDisplay => Order.Timestamp.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string TotalDisplay => $"{Order.Total:F2} €";
    public string StandName => Order.StandName;

    /// <summary>Pre-formatted line items text for display without BindableLayout.</summary>
    public string ItemsDetail { get; }

    public OrderHistoryItem(OrderRecord order, List<OrderItemRecord> items)
    {
        Order = order;
        Items = items;
        ItemsDetail = string.Join("\n", items.Select(i => $"  {i.Quantity}x  {i.ArticleName,-20} {i.LineTotal:F2} €"));
    }
}

public partial class OrderHistoryViewModel : ObservableObject
{
    private readonly IOrderHistoryService _historyService;
    private readonly ILogService _log;

    public ObservableCollection<OrderHistoryItem> Orders { get; } = new();

    [ObservableProperty]
    private DateTime _filterFromDate = DateTime.Today.AddDays(-6);

    [ObservableProperty]
    private DateTime _filterToDate = DateTime.Today;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasOrders;

    [ObservableProperty]
    private string _totalSumDisplay = "0,00 €";

    public OrderHistoryViewModel(IOrderHistoryService historyService, ILogService logService)
    {
        _historyService = historyService;
        _log = logService;
    }

    public async Task InitializeAsync()
    {
        await LoadOrdersAsync();
    }

    partial void OnFilterFromDateChanged(DateTime value) => _ = LoadOrdersAsync();
    partial void OnFilterToDateChanged(DateTime value) => _ = LoadOrdersAsync();

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        IsLoading = true;
        try
        {
            var fromUtc = FilterFromDate.Date.ToUniversalTime();
            var toUtc = FilterToDate.Date.AddDays(1).ToUniversalTime();
            _log.Debug($"Loading order history: {FilterFromDate:dd.MM.yyyy} – {FilterToDate:dd.MM.yyyy}.");

            var records = await _historyService.GetOrdersAsync(from: fromUtc, to: toUtc);

            Orders.Clear();
            foreach (var r in records)
                Orders.Add(new OrderHistoryItem(r, r.Items));

            HasOrders = Orders.Count > 0;
            var sum = Orders.Sum(o => o.Order.Total);
            TotalSumDisplay = $"{sum:F2} €";
            _log.Debug($"Order history loaded: {Orders.Count} order(s), total={sum:F2}€.");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error loading order history.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleExpand(OrderHistoryItem item)
    {
        item.IsExpanded = !item.IsExpanded;
    }

    [RelayCommand]
    private async Task DeleteOrderAsync(OrderHistoryItem item)
    {
        var loc = LocalizationService.Instance;
        bool confirmed = await Shell.Current.DisplayAlert(
            loc["OrderHistory_Delete_Title"],
            loc.Format("OrderHistory_Delete_Msg", item.TimestampDisplay),
            loc["Common_Yes"], loc["Common_Cancel"]);
        if (!confirmed) return;

        try
        {
            await _historyService.DeleteOrderAsync(item.Order.Id);
            Orders.Remove(item);
            HasOrders = Orders.Count > 0;
            TotalSumDisplay = $"{Orders.Sum(o => o.Order.Total):F2} €";
            _log.Info($"Order from {item.TimestampDisplay} deleted.");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, $"Error deleting order from {item.TimestampDisplay}.");
        }
    }

    [RelayCommand]
    private async Task ExportDbFileAsync()
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "festkasse_orders.db");
            if (!File.Exists(dbPath))
            {
                var loc2 = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(loc2["Common_Info"], loc2["Alert_OrderHistory_NoDb"], loc2["Common_OK"]);
                return;
            }

            var fileName = $"FestKasse_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var copyPath = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.Copy(dbPath, copyPath, overwrite: true);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "SQLite Datenbank exportieren",
                File = new ShareFile(copyPath)
            });
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error exporting SQLite database.");
            var loc3 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc3["Common_Error"], loc3.Format("Alert_OrderHistory_DbExportError", ex.Message), loc3["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ExportToJsonAsync()
    {
        try
        {
            var export = Orders.Select(o => o.Order).ToList();

            var json = JsonSerializer.Serialize(export, OrderExportJsonContext.Default.ListOrderRecord);
            var fileName = $"FestKasse_Bestellungen_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);

            _log.Info($"Order history exported as JSON: {export.Count} order(s).");
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Bestellverlauf exportieren",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error exporting order history as JSON.");
            var loc4 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc4["Common_Error"], loc4.Format("Alert_OrderHistory_ExportError", ex.Message), loc4["Common_OK"]);
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<OrderRecord>))]
internal partial class OrderExportJsonContext : JsonSerializerContext
{
}
