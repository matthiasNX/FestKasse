using FestKasse.Data;
using FestKasse.Models;
using Microsoft.EntityFrameworkCore;

namespace FestKasse.Services;

public class OrderHistoryService : IOrderHistoryService
{
    private readonly ILogService _log;
    private bool _initialized;

    public OrderHistoryService(ILogService logService)
    {
        _log = logService;
    }

    public async Task InitAsync()
    {
        if (_initialized) return;
        _log.Debug("Initializing SQLite order database.");
        using var db = new OrderDbContext();
        await db.Database.EnsureCreatedAsync();
        _initialized = true;
        _log.Info("Order database ready.");
    }

    public async Task SaveOrderAsync(OrderRecord order)
    {
        await InitAsync();
        _log.Info($"Saving order: stand='{order.StandName}', total={order.Total:F2}€, items={order.Items.Count}.");
        try
        {
            using var db = new OrderDbContext();
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            _log.Debug($"Order with ID {order.Id} saved.");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error saving order to database.");
            throw;
        }
    }

    public async Task<List<OrderRecord>> GetOrdersAsync(DateTime? from = null, DateTime? to = null)
    {
        await InitAsync();
        _log.Debug($"Loading order history: from={from?.ToString("u") ?? "unlimited"} to={to?.ToString("u") ?? "unlimited"}.");
        try
        {
            using var db = new OrderDbContext();
            var query = db.Orders.Include(o => o.Items).AsQueryable();

            if (from.HasValue)  query = query.Where(o => o.Timestamp >= from.Value);
            if (to.HasValue)    query = query.Where(o => o.Timestamp <= to.Value);

            var result = await query.OrderByDescending(o => o.Timestamp).ToListAsync();
            _log.Debug($"{result.Count} order(s) loaded.");
            return result;
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error loading order history.");
            throw;
        }
    }

    public async Task<int> GetOrderCountAsync()
    {
        await InitAsync();
        using var db = new OrderDbContext();
        return await db.Orders.CountAsync();
    }

    public async Task ClearAllAsync()
    {
        await InitAsync();
        _log.Warning("Clearing entire order history.");
        try
        {
            using var db = new OrderDbContext();
            db.OrderItems.RemoveRange(db.OrderItems);
            db.Orders.RemoveRange(db.Orders);
            await db.SaveChangesAsync();
            _log.Info("Order history cleared completely.");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error clearing order history.");
            throw;
        }
    }

    public async Task DeleteOrderAsync(int orderId)
    {
        await InitAsync();
        _log.Info($"Deleting order with ID {orderId}.");
        try
        {
            using var db = new OrderDbContext();
            var order = await db.Orders.FindAsync(orderId);
            if (order != null)
            {
                db.Orders.Remove(order);
                await db.SaveChangesAsync();
                _log.Debug($"Order {orderId} deleted.");
            }
            else
            {
                _log.Warning($"DeleteOrderAsync: Order with ID {orderId} not found.");
            }
        }
        catch (Exception ex)
        {
            _log.Exception(ex, $"Error deleting order {orderId}.");
            throw;
        }
    }
}
