namespace FestKasse.Models;

public class Category : ISortable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
