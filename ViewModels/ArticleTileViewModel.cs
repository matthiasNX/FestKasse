using CommunityToolkit.Mvvm.ComponentModel;
using FestKasse.Models;

namespace FestKasse.ViewModels;

/// <summary>
/// Wrapper around Article for the main-page tile grid.
/// Holds the live cart quantity so the badge updates automatically.
/// </summary>
public partial class ArticleTileViewModel : ObservableObject
{
    [ObservableProperty]
    private int _quantity;

    public Article Article { get; }

    public ArticleTileViewModel(Article article, int quantity = 0)
    {
        Article = article;
        _quantity = quantity;
    }
}
