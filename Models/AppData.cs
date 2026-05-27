namespace FestKasse.Models;

public class AppData
{
    public List<Stand> Stands { get; set; } = new();
    public string ActiveStandId { get; set; } = string.Empty;
}
