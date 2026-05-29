using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FestKasse.Models;

namespace FestKasse.Services;

public class OrderService : IOrderService
{
    private readonly ILogService _log;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OrderService(ILogService logService)
    {
        _log = logService;
    }

    public async Task<bool> SendOrderAsync(OrderRecord order, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.OrderUrl))
        {
            _log.Warning("SendOrderAsync: Keine Bestell-URL konfiguriert – Bestellung wird nicht gesendet.");
            return false;
        }

        _log.Info($"Sende Bestellung: Stand='{order.StandName}', Gesamtbetrag={order.Total:F2}€, Modus={settings.OrderSendMode}, URL={settings.OrderUrl}");

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
                _log.Info($"Bestellung erfolgreich per POST gesendet. HTTP-Status: {(int)response.StatusCode}.");
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
                _log.Info($"Bestellung erfolgreich per GET (URL-Vorlage) gesendet. HTTP-Status: {(int)response.StatusCode}.");
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _log.Exception(ex, $"HTTP-Fehler beim Senden der Bestellung an '{settings.OrderUrl}'.");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _log.Exception(ex, "Timeout while sending order.");
            return false;
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Unexpected error while sending order.");
            return false;
        }
        finally
        {
            tempClient?.Dispose();
        }
    }
}
