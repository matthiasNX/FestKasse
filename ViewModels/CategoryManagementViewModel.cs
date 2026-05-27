using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class CategoryManagementViewModel : ObservableObject
{
    private readonly IDataService _dataService;
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

    public CategoryManagementViewModel(IDataService dataService)
    {
        _dataService = dataService;
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
            await Shell.Current.DisplayAlert("Fehler", "Bitte einen Namen eingeben.", "OK");
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
        }
        else if (SelectedCategory != null)
        {
            var oldName = SelectedCategory.Name;
            SelectedCategory.Name = EditName;
            SelectedCategory.SortOrder = EditSortOrder;

            // Keep Article.Category name in sync
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
            await Shell.Current.DisplayAlert(
                "Nicht möglich",
                $"Die Kategorie \"{category.Name}\" wird noch von Artikeln verwendet und kann nicht gelöscht werden.",
                "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Löschen",
            $"Kategorie \"{category.Name}\" wirklich löschen?",
            "Ja", "Nein");

        if (confirm)
        {
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
        await _dataService.SaveCategoriesAsync(Categories.ToList());
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
