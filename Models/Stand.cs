namespace FestKasse.Models;

public class Stand
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<Article> Articles { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}
