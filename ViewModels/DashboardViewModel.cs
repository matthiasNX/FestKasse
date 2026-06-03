using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace FestKasse.ViewModels;

public partial class DashboardViewModel : ObservableObject, FestKasse.Controls.IInitializable
{
    private readonly IOrderHistoryService _historyService;
    private readonly ICashSessionService  _cashSessionService;
    private readonly ILogService _log;

    // ── Filter ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private DateTime _filterFromDate = DateTime.Today.AddDays(-6);

    [ObservableProperty]
    private TimeSpan _filterFromTime = TimeSpan.Zero;

    [ObservableProperty]
    private DateTime _filterToDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _filterToTime = new TimeSpan(23, 59, 59);

    // ── Sample-data flag

    [ObservableProperty]
    private bool _isSampleData;

    // ── KPIs ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private int _orderCount;

    [ObservableProperty]
    private string _totalRevenueDisplay = "0,00 €";

    [ObservableProperty]
    private string _avgOrderValueDisplay = "0,00 €";

    [ObservableProperty]
    private string _topProductDisplay = "—";

    [ObservableProperty]
    private string _peakHourDisplay = "—";

    [ObservableProperty]
    private string _avgItemsPerOrderDisplay = "0";

    [ObservableProperty]
    private bool _isLoading;

    // ── Session KPIs ─────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _hasOpenSession;

    [ObservableProperty]
    private string _sessionOpenedAtDisplay = "—";

    [ObservableProperty]
    private string _sessionRevenueDisplay = "0,00 €";

    [ObservableProperty]
    private int _sessionOrderCount;

    // ── Charts ──────────────────────────────────────────────────────────

    /// <summary>Bar chart: orders per hour (count).</summary>
    public ISeries[] OrdersPerHourCountSeries { get; private set; } = Array.Empty<ISeries>();

    /// <summary>Bar chart: revenue per hour.</summary>
    public ISeries[] OrdersPerHourRevenueSeries { get; private set; } = Array.Empty<ISeries>();

    /// <summary>Bar chart: quantity per product.</summary>
    public ISeries[] ProductCountSeries { get; private set; } = Array.Empty<ISeries>();

    /// <summary>X-axis labels for the "per hour" charts (0-23).</summary>
    public Axis[] HourAxes { get; private set; } = Array.Empty<Axis>();

    /// <summary>X-axis labels for the product chart.</summary>
    public Axis[] ProductAxes { get; private set; } = Array.Empty<Axis>();

    public Axis[] CountYAxes { get; } =
    [
        new Axis { MinLimit = 0, IsVisible = true, LabelsPaint = new SolidColorPaint(SKColors.Gray) }
    ];

    public Axis[] RevenueYAxes { get; } =
    [
        new Axis { MinLimit = 0, IsVisible = true, Labeler = v => $"{v:F0} €", LabelsPaint = new SolidColorPaint(SKColors.Gray) }
    ];

    // ── Constructor ─────────────────────────────────────────────────────

    public DashboardViewModel(IOrderHistoryService historyService, ICashSessionService cashSessionService, ILogService logService)
    {
        _historyService     = historyService;
        _cashSessionService = cashSessionService;
        _log = logService;
    }

    public async Task InitializeAsync() => await LoadDataAsync();

    partial void OnFilterFromDateChanged(DateTime value) => _ = LoadDataAsync();
    partial void OnFilterFromTimeChanged(TimeSpan value) => _ = LoadDataAsync();
    partial void OnFilterToDateChanged(DateTime value) => _ = LoadDataAsync();
    partial void OnFilterToTimeChanged(TimeSpan value) => _ = LoadDataAsync();

    // ── Commands ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var fromUtc = (FilterFromDate.Date + FilterFromTime).ToUniversalTime();
            var toUtc = (FilterToDate.Date + FilterToTime).ToUniversalTime();

            var orders = await _historyService.GetOrdersAsync(fromUtc, toUtc);

            var totalOrderCount = await _historyService.GetOrderCountAsync();
            if (totalOrderCount == 0)
            {
                orders = GenerateSampleOrders(FilterFromDate, FilterToDate);
                IsSampleData = true;
            }
            else
            {
                IsSampleData = false;
            }

            ComputeKpis(orders);
            BuildCharts(orders);

            // ── Session KPIs ──────────────────────────────────────────────
            var session = await _cashSessionService.GetOpenSessionAsync();
            HasOpenSession         = session is not null;
            SessionOpenedAtDisplay = session is not null ? session.OpenedAt.ToString("HH:mm") : "—";
            SessionRevenueDisplay  = session is not null ? $"{session.Revenue:F2} €" : "0,00 €";
            SessionOrderCount      = session?.OrderCount ?? 0;
        }
        catch (Exception ex)
        {
            _log.Error($"Dashboard load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── KPI computation ─────────────────────────────────────────────────

    private void ComputeKpis(List<OrderRecord> orders)
    {
        OrderCount = orders.Count;

        var total = orders.Sum(o => o.Total);
        TotalRevenueDisplay = $"{total:F2} €";
        AvgOrderValueDisplay = orders.Count > 0 ? $"{total / orders.Count:F2} €" : "0,00 €";

        // top product by total quantity
        var allItems = orders.SelectMany(o => o.Items).ToList();
        var topItem = allItems
            .GroupBy(i => i.ArticleName)
            .Select(g => (Name: g.Key, Qty: g.Sum(i => i.Quantity)))
            .OrderByDescending(x => x.Qty)
            .FirstOrDefault();
        TopProductDisplay = topItem == default ? "—" : $"{topItem.Name} ({topItem.Qty}×)";

        // peak hour
        var peakHour = orders
            .GroupBy(o => o.Timestamp.ToLocalTime().Hour)
            .Select(g => (Hour: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();
        PeakHourDisplay = peakHour == default ? "—" : $"{peakHour.Hour:00}:00 ({peakHour.Count})";

        // avg items per order
        AvgItemsPerOrderDisplay = orders.Count > 0
            ? $"{allItems.Sum(i => i.Quantity) / (double)orders.Count:F1}"
            : "0";
    }

    // ── Chart building ──────────────────────────────────────────────────

    private void BuildCharts(List<OrderRecord> orders)
    {
        // --- Orders per hour ---
        var byHour = new int[24];
        var revenueByHour = new double[24];
        foreach (var o in orders)
        {
            var h = o.Timestamp.ToLocalTime().Hour;
            byHour[h]++;
            revenueByHour[h] += (double)o.Total;
        }

        var hourLabels = Enumerable.Range(0, 24).Select(h => $"{h:00}").ToArray();

        HourAxes =
        [
            new Axis
            {
                Labels = hourLabels,
                LabelsRotation = 0,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 11
            }
        ];

        OrdersPerHourCountSeries =
        [
            new ColumnSeries<int>
            {
                Name = "Orders",
                Values = byHour,
                Fill = new SolidColorPaint(new SKColor(0x2E, 0x7D, 0x32, 200)),
                Stroke = null,
                MaxBarWidth = double.MaxValue
            }
        ];

        OrdersPerHourRevenueSeries =
        [
            new ColumnSeries<double>
            {
                Name = "Revenue",
                Values = revenueByHour,
                Fill = new SolidColorPaint(new SKColor(0x81, 0xC7, 0x84, 200)),
                Stroke = null,
                MaxBarWidth = double.MaxValue
            }
        ];

        // --- Count per product ---
        var productGroups = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ArticleName)
            .Select(g => (Name: g.Key, Qty: g.Sum(i => i.Quantity)))
            .OrderByDescending(x => x.Qty)
            .Take(15)
            .ToList();

        ProductAxes =
        [
            new Axis
            {
                Labels = productGroups.Select(p => p.Name).ToArray(),
                LabelsRotation = -30,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 11
            }
        ];

        ProductCountSeries =
        [
            new ColumnSeries<int>
            {
                Name = "Qty",
                Values = productGroups.Select(p => p.Qty).ToArray(),
                Fill = new SolidColorPaint(new SKColor(0xFF, 0x98, 0x00, 200)),
                Stroke = null,
                MaxBarWidth = double.MaxValue
            }
        ];

        // Notify UI of all chart property changes
        OnPropertyChanged(nameof(HourAxes));
        OnPropertyChanged(nameof(OrdersPerHourCountSeries));
        OnPropertyChanged(nameof(OrdersPerHourRevenueSeries));
        OnPropertyChanged(nameof(ProductAxes));
        OnPropertyChanged(nameof(ProductCountSeries));
    }

    // ── Sample data generator ────────────────────────────────────────────

    private static List<OrderRecord> GenerateSampleOrders(DateTime from, DateTime to)
    {
        var rng = new Random(42);
        var products = new[]
        {
            ("Bier 0,5l", 3.50m), ("Bier 0,3l", 2.50m), ("Wein weiß", 2.80m),
            ("Wein rot", 2.80m), ("Wasser", 1.50m), ("Cola", 2.00m),
            ("Bratwurst", 3.00m), ("Brezel", 1.80m), ("Kaffee", 2.20m), ("Sekt", 3.00m)
        };

        var orders = new List<OrderRecord>();
        int id = 1;
        var span = (to.Date - from.Date).Days + 1;

        for (int day = 0; day < span; day++)
        {
            var base_ = from.Date.AddDays(day);
            // busy hours: 11-22
            int count = rng.Next(20, 40);
            for (int i = 0; i < count; i++)
            {
                var hour = rng.Next(11, 23);
                var minute = rng.Next(0, 60);
                var ts = base_.AddHours(hour).AddMinutes(minute).ToUniversalTime();

                var items = new List<OrderItemRecord>();
                int lineCount = rng.Next(1, 5);
                for (int j = 0; j < lineCount; j++)
                {
                    var (name, price) = products[rng.Next(products.Length)];
                    var qty = rng.Next(1, 4);
                    items.Add(new OrderItemRecord
                    {
                        ArticleName = name,
                        Quantity = qty,
                        UnitPrice = price,
                        LineTotal = price * qty
                    });
                }

                orders.Add(new OrderRecord
                {
                    Id = id++,
                    Timestamp = ts,
                    StandName = "Musterstand",
                    Total = items.Sum(x => x.LineTotal),
                    Items = items
                });
            }
        }

        return orders;
    }
}
