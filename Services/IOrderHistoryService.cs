using FestKasse.Models;

namespace FestKasse.Services;

public interface IOrderHistoryService
{
    Task InitAsync();

    /// <summary>Persist a completed order with its line items already populated in order.Items.</summary>
    Task SaveOrderAsync(OrderRecord order);

    /// <summary>Return all orders with items, optionally filtered by UTC date range.</summary>
    Task<List<OrderRecord>> GetOrdersAsync(DateTime? from = null, DateTime? to = null);

    /// <summary>Total number of stored order records.</summary>
    Task<int> GetOrderCountAsync();

    /// <summary>Delete all stored orders and items.</summary>
    Task ClearAllAsync();

    /// <summary>Delete a single order by id (cascades to items).</summary>
    Task DeleteOrderAsync(int orderId);
}
