using System.Collections.ObjectModel;
using FestKasse.ViewModels;

namespace FestKasse.Models;

/// <summary>A named group of article tiles used for category-grouped display on the main page.</summary>
public class ArticleCategoryGroup : ObservableCollection<ArticleTileViewModel>
{
    public string CategoryName { get; }

    public ArticleCategoryGroup(string categoryName, IEnumerable<ArticleTileViewModel> items)
        : base(items)
    {
        CategoryName = categoryName;
    }
}
