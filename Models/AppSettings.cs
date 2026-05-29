namespace FestKasse.Models;

public class AppSettings
{
    public int DisplayTimeoutMinutes { get; set; } = 10;
    public string? LogoBase64 { get; set; }
    public string? SyncUrl { get; set; }
    public int TileSize { get; set; } = 120;  // tile width/height in dp

    // Order submission settings
    public bool OrderEnabled { get; set; }
    public string? OrderUrl { get; set; }
    public OrderSendMode OrderSendMode { get; set; } = OrderSendMode.JsonBody;
    public bool OrderIgnoreSslErrors { get; set; }

    // Local order history
    public bool SaveOrdersLocally { get; set; } = false;

    // Logging
    public string LogLevel { get; set; } = "Information";

    // Language: "system" | "de" | "en"
    public string Language { get; set; } = "system";

    public List<string> AvailableColors { get; set; } = new()
    {
        "#4CAF50", // Grün
        "#2196F3", // Blau
        "#F44336", // Rot
        "#FF9800", // Orange
        "#9C27B0", // Lila
        "#00BCD4", // Cyan
        "#795548", // Braun
        "#607D8B", // Grau
        "#E91E63", // Pink
        "#FFEB3B", // Gelb
        "#3F51B5", // Indigo
        "#009688"  // Teal
    };
}

public enum OrderSendMode
{
    JsonBody,
    UrlTemplate
}
