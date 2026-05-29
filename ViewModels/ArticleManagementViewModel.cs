using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class ArticleManagementViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ILogService _log;

    [ObservableProperty]
    private ObservableCollection<Article> _articles = new();

    [ObservableProperty]
    private Article? _selectedArticle;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private Category? _editCategory;

    [ObservableProperty]
    private string _editColor = "#4CAF50";

    [ObservableProperty]
    private decimal _editPrice;

    [ObservableProperty]
    private string _editPriceText = "0,00";

    private static readonly CultureInfo _de = new("de-DE");
    private bool _syncingPrice;

    partial void OnEditPriceChanged(decimal value)
    {
        if (_syncingPrice) return;
        var formatted = value.ToString("F2", _de);
        if (EditPriceText != formatted)
            EditPriceText = formatted;
    }

    partial void OnEditPriceTextChanged(string value)
    {
        if (_syncingPrice) return;
        var parsed = ParseDecimalText(value);
        if (_editPrice == parsed) return;
        _syncingPrice = true;
        try
        {
            _editPrice = parsed;
            OnPropertyChanged(nameof(EditPrice));
        }
        finally { _syncingPrice = false; }
    }

    private static decimal ParseDecimalText(string? value)
    {
        var s = (value ?? string.Empty).Trim()
            .Replace("\u202f", "").Replace(" ", "").Replace(".", ",");
        if (s.StartsWith(",")) s = "0" + s;
        else if (s.StartsWith("-,")) s = "-0" + s[1..];
        if (s.EndsWith(",")) s += "0";
        return decimal.TryParse(s, NumberStyles.Any, _de, out var r) ? r : 0m;
    }

    [ObservableProperty]
    private int _editSortOrder;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNewArticle;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableColors = new();

    private Stand _stand = new();

    [ObservableProperty]
    private string? _editIcon;

    [ObservableProperty]
    private bool _hasEditIcon;

    partial void OnEditIconChanged(string? value) => HasEditIcon = !string.IsNullOrEmpty(value);

    public static readonly IReadOnlyList<string> AvailableIcons = new[]
    {
        "🍷","🍸","🍹","🍺","🥂","🫗","🍾","🧃","💧","🫧",
        "🥤","☕","🍵","🧋","🧊","🍋","🍊","🍓","🍇","🍑",
        "🌿","🍀","🌸","⭐","❤️","🔥","💎","🎉","🎁","🏆",
        "🌭","🍔","🍕","🌮","🌯","🥙","🧆","🥪","🍱","🍜",
        "🍰","🧁","🍩","🍪","🍫","🍬","🍭","🍦","🧇","🥞"
    };

    public ArticleManagementViewModel(IDataService dataService, ILogService logService)
    {
        _dataService = dataService;
        _log = logService;
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _stand = await _dataService.GetActiveStandAsync() ?? new Stand();
        _log.Debug($"Article management: loading data for stand '{_stand.Name}', {_stand.Articles.Count} article(s).");

        Articles.Clear();
        foreach (var article in _stand.Articles.OrderBy(a => a.SortOrder).ThenBy(a => a.Description))
            Articles.Add(article);

        Categories.Clear();
        foreach (var category in _stand.Categories.OrderBy(c => c.SortOrder))
            Categories.Add(category);

        AvailableColors.Clear();
        var settings = await _dataService.GetSettingsAsync();
        foreach (var color in settings.AvailableColors)
            AvailableColors.Add(color);
    }

    [RelayCommand]
    private void NewArticle()
    {
        SelectedArticle = null;
        EditDescription = string.Empty;
        EditCategory = Categories.FirstOrDefault();
        EditColor = AvailableColors.FirstOrDefault() ?? "#4CAF50";
        EditPrice = 0;
        EditSortOrder = Articles.Count;
        EditIcon = null;
        IsNewArticle = true;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditArticle(Article article)
    {
        SelectedArticle = article;
        EditDescription = article.Description;
        EditCategory = Categories.FirstOrDefault(c => c.Id == article.CategoryId) ?? Categories.FirstOrDefault();
        EditColor = !string.IsNullOrWhiteSpace(article.Color) ? article.Color : (AvailableColors.FirstOrDefault() ?? "#4CAF50");
        EditPrice = article.Price;
        EditSortOrder = article.SortOrder;
        EditIcon = article.Icon;
        IsNewArticle = false;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveArticleAsync()
    {
        if (string.IsNullOrWhiteSpace(EditDescription))
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc["Alert_Article_NoDescription"], loc["Common_OK"]);
            return;
        }

        if (IsNewArticle)
        {
            var newArticle = new Article
            {
                Description = EditDescription,
                CategoryId = EditCategory?.Id ?? string.Empty,
                Category = EditCategory?.Name ?? string.Empty,
                Color = !string.IsNullOrWhiteSpace(EditColor) ? EditColor : "#4CAF50",
                Price = EditPrice,
                SortOrder = EditSortOrder,
                Icon = string.IsNullOrEmpty(EditIcon) ? null : EditIcon
            };
            Articles.Add(newArticle);
            _log.Info($"Article created: '{EditDescription}', price={EditPrice:F2}€, category='{EditCategory?.Name}'.");
        }
        else if (SelectedArticle != null)
        {
            SelectedArticle.Description = EditDescription;
            SelectedArticle.CategoryId = EditCategory?.Id ?? string.Empty;
            SelectedArticle.Category = EditCategory?.Name ?? string.Empty;
            SelectedArticle.Color = !string.IsNullOrWhiteSpace(EditColor) ? EditColor : "#4CAF50";
            SelectedArticle.Price = EditPrice;
            SelectedArticle.SortOrder = EditSortOrder;
            SelectedArticle.Icon = string.IsNullOrEmpty(EditIcon) ? null : EditIcon;
            _log.Info($"Article updated: '{EditDescription}', price={EditPrice:F2}€.");
        }

        await SaveAllArticlesAsync();
        CancelEdit();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SelectedArticle = null;
    }

    [RelayCommand]
    private void ClearIcon() => EditIcon = null;

    [RelayCommand]
    private void SelectIcon(string icon) => EditIcon = icon;

    [RelayCommand]
    private async Task DeleteArticleAsync(Article article)
    {
        var loc = LocalizationService.Instance;
        var confirm = await Shell.Current.DisplayAlert(
            loc["Article_Delete_Title"],
            loc.Format("Article_Delete_Msg", article.Description),
            loc["Common_Yes"], loc["Common_No"]);

        if (confirm)
        {
            _log.Info($"Article deleted: '{article.Description}'.");
            Articles.Remove(article);
            await SaveAllArticlesAsync();
        }
    }

    [RelayCommand]
    private async Task MoveUpAsync(Article article)
    {
        var index = Articles.IndexOf(article);
        if (index > 0)
        {
            Articles.Move(index, index - 1);
            UpdateSortOrder();
            await SaveAllArticlesAsync();
        }
    }

    [RelayCommand]
    private async Task MoveDownAsync(Article article)
    {
        var index = Articles.IndexOf(article);
        if (index < Articles.Count - 1)
        {
            Articles.Move(index, index + 1);
            UpdateSortOrder();
            await SaveAllArticlesAsync();
        }
    }

    private void UpdateSortOrder()
    {
        for (int i = 0; i < Articles.Count; i++)
        {
            Articles[i].SortOrder = i;
        }
    }

    private async Task SaveAllArticlesAsync()
    {
        try
        {
            await _dataService.SaveArticlesAsync(Articles.ToList());
            _log.Debug($"Article list saved: {Articles.Count} article(s).");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error saving article list.");
            throw;
        }
    }

    [RelayCommand]
    private void IncreaseSortOrder()
    {
        EditSortOrder++;
    }

    [RelayCommand]
    private void DecreaseSortOrder()
    {
        if (EditSortOrder > 0)
            EditSortOrder--;
    }

    [RelayCommand]
    private void SelectColor(string color)
    {
        EditColor = color;
    }
}
