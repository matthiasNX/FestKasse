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

    public OrderHistoryViewModel(IOrderHistoryService historyService)
    {
        _historyService = historyService;
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

            var records = await _historyService.GetOrdersAsync(from: fromUtc, to: toUtc);

            Orders.Clear();
            foreach (var r in records)
                Orders.Add(new OrderHistoryItem(r, r.Items));

            HasOrders = Orders.Count > 0;
            var sum = Orders.Sum(o => o.Order.Total);
            TotalSumDisplay = $"{sum:F2} €";
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
        bool confirmed = await Shell.Current.DisplayAlert(
            "Löschen", $"Bestellung vom {item.TimestampDisplay} wirklich löschen?", "Ja", "Abbrechen");
        if (!confirmed) return;

        await _historyService.DeleteOrderAsync(item.Order.Id);
        Orders.Remove(item);
        HasOrders = Orders.Count > 0;
        TotalSumDisplay = $"{Orders.Sum(o => o.Order.Total):F2} €";
    }

    [RelayCommand]
    private async Task ExportDbFileAsync()
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "festkasse_orders.db");
            if (!File.Exists(dbPath))
            {
                await Shell.Current.DisplayAlert("Info", "Keine Datenbankdatei gefunden.", "OK");
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
            await Shell.Current.DisplayAlert("Fehler", $"DB-Export fehlgeschlagen: {ex.Message}", "OK");
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

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Bestellverlauf exportieren",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Fehler", $"Export fehlgeschlagen: {ex.Message}", "OK");
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<OrderRecord>))]
internal partial class OrderExportJsonContext : JsonSerializerContext
{
}
