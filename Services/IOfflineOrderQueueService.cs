using FestKasse.Models;

namespace FestKasse.Services;

public interface IOfflineOrderQueueService
{
    /// <summary>Persists an order for later retry when connectivity is restored.</summary>
    Task EnqueueAsync(OrderRecord order);

    /// <summary>
    /// Processes all queued orders. Successfully sent orders are removed from the
    /// queue; failed ones remain for the next retry cycle.
    /// </summary>
    Task RetryAllAsync();

    Task<int> GetQueueLengthAsync();
}
