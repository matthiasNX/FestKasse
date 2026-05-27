using System.Text.Json.Serialization;

namespace FestKasse.Models;

/// <summary>Order header — one row per completed order.</summary>
public class OrderRecord
{
    public int Id { get; set; }

    /// <summary>UTC timestamp of when the order was finalised.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Stand name at the time of the order.</summary>
    public string StandName { get; set; } = string.Empty;

    /// <summary>Order grand total.</summary>
    public decimal Total { get; set; }

    /// <summary>Navigation property
    [JsonPropertyName("items")]
    public List<OrderItemRecord> Items { get; set; } = new();
}
