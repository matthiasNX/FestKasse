using CommunityToolkit.Mvvm.ComponentModel;

namespace FestKasse.Models;

public partial class CartItem : ObservableObject
{
    public Article Article { get; set; } = null!;

    [ObservableProperty]
    private int _quantity;

    public decimal Total => Article.Price * Quantity;

    public string TotalDisplay => $"{Total:F2} €";

    public CartItem()
    {
    }

    public CartItem(Article article)
    {
        Article = article;
        Quantity = 1;
    }

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalDisplay));
    }
}
