using System.Text.Json.Serialization;

namespace FestKasse.Models;

/// <summary>One line item belonging to an <see cref="OrderRecord"/>.</summary>
public class OrderItemRecord
{
    public int Id { get; set; }

    /// <summary>FK to <see cref="OrderRecord"/>.</summary>
    public int OrderRecordId { get; set; }

    [JsonPropertyName("name")]
    public string ArticleName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal LineTotal { get; set; }
}
