namespace FestKasse.Models;

public class AppSettings
{
    public int DisplayTimeoutMinutes { get; set; } = 10;
    public string? LogoBase64 { get; set; }
    public string? SyncUrl { get; set; }
    public int TileSize { get; set; } = FestKasse.Helpers.AppConstants.DefaultTileSize;
    public bool ShowCategoryGroupHeaders { get; set; } = true;

    // Order submission settings
    public bool OrderEnabled { get; set; }
    public string? OrderUrl { get; set; }
    public OrderSendMode OrderSendMode { get; set; } = OrderSendMode.JsonBody;
    public bool OrderIgnoreSslErrors { get; set; }

    // Local order history
    public bool SaveOrdersLocally { get; set; } = false;

    // Receipt sharing after order completion
    public bool ShareReceiptAfterOrder { get; set; } = false;

    // Haptic feedback
    public bool HapticVibrationEnabled { get; set; } = true;
    public bool HapticSoundEnabled { get; set; } = false;

    // Logging
    public string LogLevel { get; set; } = "Information";

    // Language: "system" | "de" | "en"
    public string Language { get; set; } = "system";

    // Denomination tiles shown in the payment panel
    public List<decimal> Notes { get; set; } = new() { 200, 100, 50, 20, 10, 5 };
    public List<decimal> Coins { get; set; } = new() { 2m, 1m, 0.50m, 0.20m, 0.10m, 0.05m, 0.02m, 0.01m };

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
