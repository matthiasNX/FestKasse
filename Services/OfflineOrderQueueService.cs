using System.Text.Json;
using FestKasse.Helpers;
using FestKasse.Models;

namespace FestKasse.Services;

/// <summary>
/// Persists orders that could not be sent (offline or server error) to a local
/// JSON file, and retries them via <see cref="IOrderService"/> when called.
/// </summary>
public class OfflineOrderQueueService : IOfflineOrderQueueService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _filePath = AppConstants.OfflineQueueFilePath;

    private readonly IOrderService      _orderService;
    private readonly ILogService        _log;
    private readonly IDataService       _dataService;

    public OfflineOrderQueueService(
        IOrderService orderService,
        ILogService   logService,
        IDataService  dataService)
    {
        _orderService = orderService;
        _log          = logService;
        _dataService  = dataService;
    }

    // ── IOfflineOrderQueueService ─────────────────────────────────────────

    public async Task EnqueueAsync(OrderRecord order)
    {
        var queue = await LoadAsync();
        queue.Add(order);
        await SaveAsync(queue);
        _log.Info($"Offline queue: enqueued order {order.Id}. Queue length={queue.Count}.");
    }

    public async Task RetryAllAsync()
    {
        var queue = await LoadAsync();
        if (queue.Count == 0) return;

        var settings = await _dataService.GetSettingsAsync();
        var remaining = new List<OrderRecord>();

        foreach (var order in queue)
        {
            try
            {
                var sent = await _orderService.SendOrderAsync(order, settings);
                if (sent)
                    _log.Info($"Offline queue: resent order {order.Id}.");
                else
                    remaining.Add(order);
            }
            catch (Exception ex)
            {
                _log.Exception(ex, $"Offline queue: retry failed for order {order.Id}.");
                remaining.Add(order);
            }
        }

        await SaveAsync(remaining);
        _log.Info($"Offline queue retry complete. Remaining={remaining.Count}.");
    }

    public async Task<int> GetQueueLengthAsync()
        => (await LoadAsync()).Count;

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<List<OrderRecord>> LoadAsync()
    {
        if (!File.Exists(_filePath)) return [];
        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<OrderRecord>>(stream, _jsonOptions)
                   ?? [];
        }
        catch { return []; }
    }

    private async Task SaveAsync(List<OrderRecord> queue)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, queue, _jsonOptions);
    }
}
