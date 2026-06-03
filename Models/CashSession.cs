using System.Text.Json.Serialization;

namespace FestKasse.Models;

public class CashSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal ClosingCash { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }

    [JsonIgnore]
    public bool IsOpen => ClosedAt is null;

    [JsonIgnore]
    public decimal Difference => ClosingCash - (OpeningCash + Revenue);
}
