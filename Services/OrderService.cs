using System.Text;
using System.Text.Json;
using FestKasse.Models;

namespace FestKasse.Services;

public class OrderService : IOrderService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<bool> SendOrderAsync(OrderRecord order, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.OrderUrl))
            return false;

        HttpClient? tempClient = null;
        HttpClient client;

        if (settings.OrderIgnoreSslErrors)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            tempClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client = tempClient;
        }
        else
        {
            client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            tempClient = client;
        }

        try
        {
            if (settings.OrderSendMode == OrderSendMode.JsonBody)
            {
                var json = JsonSerializer.Serialize(order, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(settings.OrderUrl, content);
                response.EnsureSuccessStatusCode();
            }
            else // UrlTemplate
            {
                var itemsJson = JsonSerializer.Serialize(order.Items, _jsonOptions);
                var url = settings.OrderUrl
                    .Replace("{total}", order.Total.ToString("F2"))
                    .Replace("{timestamp}", Uri.EscapeDataString(order.Timestamp.ToString("o")))
                    .Replace("{items}", Uri.EscapeDataString(itemsJson));

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }

            return true;
        }
        finally
        {
            tempClient?.Dispose();
        }
    }
}
