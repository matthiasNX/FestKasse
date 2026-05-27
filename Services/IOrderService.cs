using FestKasse.Models;

namespace FestKasse.Services;

public interface IOrderService
{
    /// <summary>Sends the order to the remote server. Returns true on success.</summary>
    Task<bool> SendOrderAsync(OrderRecord order, AppSettings settings);
}

