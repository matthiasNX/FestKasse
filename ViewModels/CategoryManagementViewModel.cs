using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class CategoryManagementViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ILogService _log;
    private List<Article> _allArticles = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isNewCategory;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private int _editSortOrder;

    public CategoryManagementViewModel(IDataService dataService, ILogService logService)
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
        var stand = await _dataService.GetActiveStandAsync();
        _allArticles = stand?.Articles ?? new List<Article>();
        var categories = stand?.Categories.OrderBy(c => c.SortOrder).ToList() ?? new List<Category>();
        _log.Debug($"Kategorieverwaltung: Stand='{stand?.Name}', {categories.Count} Kategorie(n), {_allArticles.Count} Artikel.");

        Categories.Clear();
        foreach (var cat in categories)
            Categories.Add(cat);
    }

    [RelayCommand]
    private void NewCategory()
    {
        SelectedCategory = null;
        EditName = string.Empty;
        EditSortOrder = Categories.Count;
        IsNewCategory = true;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditCategory(Category category)
    {
        SelectedCategory = category;
        EditName = category.Name;
        EditSortOrder = category.SortOrder;
        IsNewCategory = false;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc["Alert_Category_NoName"], loc["Common_OK"]);
            return;
        }

        if (IsNewCategory)
        {
            var newCat = new Category
            {
                Id = Guid.NewGuid().ToString(),
                Name = EditName,
                SortOrder = EditSortOrder
            };
            Categories.Add(newCat);
            _log.Info($"Category created: '{EditName}', SortOrder={EditSortOrder}.");
        }
        else if (SelectedCategory != null)
        {
            var oldName = SelectedCategory.Name;
            SelectedCategory.Name = EditName;
            SelectedCategory.SortOrder = EditSortOrder;
            _log.Info($"Category updated: '{oldName}' → '{EditName}'.");

            if (oldName != EditName)
            {
                foreach (var article in _allArticles.Where(a => a.CategoryId == SelectedCategory.Id))
                    article.Category = EditName;
                await _dataService.SaveArticlesAsync(_allArticles);
            }
        }

        await SaveAllCategoriesAsync();
        CancelEdit();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SelectedCategory = null;
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category category)
    {
        if (_allArticles.Any(a => a.CategoryId == category.Id))
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(
                loc["Category_Delete_InUse_Title"],
                loc.Format("Category_Delete_InUse_Msg", category.Name),
                loc["Common_OK"]);
            return;
        }

        var loc2 = LocalizationService.Instance;
        var confirm = await Shell.Current.DisplayAlert(
            loc2["Category_Delete_Title"],
            loc2.Format("Category_Delete_Msg", category.Name),
            loc2["Common_Yes"], loc2["Common_No"]);

        if (confirm)
        {
            _log.Info($"Category deleted: '{category.Name}'.");
            Categories.Remove(category);
            await SaveAllCategoriesAsync();
        }
    }

    [RelayCommand]
    private async Task MoveUpAsync(Category category)
    {
        var index = Categories.IndexOf(category);
        if (index > 0)
        {
            Categories.Move(index, index - 1);
            UpdateSortOrder();
            await SaveAllCategoriesAsync();
        }
    }

    [RelayCommand]
    private async Task MoveDownAsync(Category category)
    {
        var index = Categories.IndexOf(category);
        if (index < Categories.Count - 1)
        {
            Categories.Move(index, index + 1);
            UpdateSortOrder();
            await SaveAllCategoriesAsync();
        }
    }

    private void UpdateSortOrder()
    {
        for (int i = 0; i < Categories.Count; i++)
            Categories[i].SortOrder = i;
    }

    private async Task SaveAllCategoriesAsync()
    {
        try
        {
            await _dataService.SaveCategoriesAsync(Categories.ToList());
            _log.Debug($"Kategorieliste gespeichert: {Categories.Count} Kategorie(n).");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Fehler beim Speichern der Kategorieliste.");
            throw;
        }
    }

    [RelayCommand]
    private void IncreaseSortOrder() => EditSortOrder++;

    [RelayCommand]
    private void DecreaseSortOrder()
    {
        if (EditSortOrder > 0)
            EditSortOrder--;
    }
}
