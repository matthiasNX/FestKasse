using System.Text.Json.Serialization;

namespace FestKasse.Models;

public class Article
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Color { get; set; } = "#4CAF50";
    public decimal Price { get; set; }
    public int SortOrder { get; set; }

    /// <summary>A single Unicode character / emoji shown large on the tile. Null = no icon.</summary>
    public string? Icon { get; set; }

    [JsonIgnore]
    public Microsoft.Maui.Graphics.Color DisplayColor
    {
        get
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Color))
                    return Microsoft.Maui.Graphics.Color.FromArgb(Color);
            }
            catch { }
            return Microsoft.Maui.Graphics.Colors.Gray;
        }
    }

    [JsonIgnore]
    public string PriceDisplay => $"{Price:F2} €";
}
