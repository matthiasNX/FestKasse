using FestKasse.Data;
using FestKasse.Models;
using Microsoft.EntityFrameworkCore;

namespace FestKasse.Services;

public class OrderHistoryService : IOrderHistoryService
{
    private bool _initialized;

    public async Task InitAsync()
    {
        if (_initialized) return;
        using var db = new OrderDbContext();
        await db.Database.EnsureCreatedAsync();
        _initialized = true;
    }

    public async Task SaveOrderAsync(OrderRecord order)
    {
        await InitAsync();
        using var db = new OrderDbContext();
        db.Orders.Add(order);          // EF cascades Items automatically
        await db.SaveChangesAsync();
    }

    public async Task<List<OrderRecord>> GetOrdersAsync(DateTime? from = null, DateTime? to = null)
    {
        await InitAsync();
        using var db = new OrderDbContext();
        var query = db.Orders.Include(o => o.Items).AsQueryable();

        if (from.HasValue)  query = query.Where(o => o.Timestamp >= from.Value);
        if (to.HasValue)    query = query.Where(o => o.Timestamp <= to.Value);

        return await query.OrderByDescending(o => o.Timestamp).ToListAsync();
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
        using var db = new OrderDbContext();
        db.OrderItems.RemoveRange(db.OrderItems);
        db.Orders.RemoveRange(db.Orders);
        await db.SaveChangesAsync();
    }

    public async Task DeleteOrderAsync(int orderId)
    {
        await InitAsync();
        using var db = new OrderDbContext();
        var order = await db.Orders.FindAsync(orderId);
        if (order != null)
        {
            db.Orders.Remove(order);
            await db.SaveChangesAsync();
        }
    }
}
