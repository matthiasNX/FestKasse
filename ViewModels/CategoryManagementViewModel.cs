using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class CategoryManagementViewModel : SortableListViewModelBase<Category>, FestKasse.Controls.IInitializable
{
    private readonly IDataService _dataService;
    private readonly ILogService _log;
    private List<Article> _allArticles = new();

    // Alias for XAML bindings
    public ObservableCollection<Category> Categories => Items;
    public bool IsNewCategory { get => IsNewItem; private set => IsNewItem = value; }

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _editName = string.Empty;

    public CategoryManagementViewModel(IDataService dataService, ILogService logService)
    {
        _dataService = dataService;
        _log = logService;
    }

    public async Task InitializeAsync() => await LoadDataAsync();

    private async Task LoadDataAsync()
    {
        var stand = await _dataService.GetActiveStandAsync();
        _allArticles = stand?.Articles ?? new List<Article>();
        var categories = stand?.Categories.OrderBy(c => c.SortOrder).ToList() ?? new List<Category>();
        _log.Debug($"Kategorieverwaltung: Stand='{stand?.Name}', {categories.Count} Kategorie(n), {_allArticles.Count} Artikel.");

        Items.Clear();
        foreach (var cat in categories)
            Items.Add(cat);
    }

    [RelayCommand]
    private void NewCategory()
    {
        SelectedCategory = null;
        EditName = string.Empty;
        EditSortOrder = Items.Count;
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
            Items.Add(newCat);
            _log.Info($"Category created: '{EditName}', SortOrder={EditSortOrder}.");
        }
        else if (SelectedCategory != null)
        {
            var oldName = SelectedCategory.Name;
            SelectedCategory.Name = EditName;
            SelectedCategory.SortOrder = EditSortOrder;
            _log.Info($"Category updated: '{oldName}' -> '{EditName}'.");
        }

        try
        {
            await SaveAllCategoriesAsync();
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error saving category.");
            await Shell.Current.DisplayAlert(LocalizationService.Instance["Common_Error"], ex.Message, LocalizationService.Instance["Common_OK"]);
            return;
        }
        CancelEdit();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        CancelEditBase();
        SelectedCategory = null;
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category category)
    {
        var loc = LocalizationService.Instance;
        if (_allArticles.Any(a => a.CategoryId == category.Id))
        {
            await Shell.Current.DisplayAlert(
                loc["Category_Delete_InUse_Title"],
                loc.Format("Category_Delete_InUse_Msg", category.Name),
                loc["Common_OK"]);
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            loc["Category_Delete_Title"],
            loc.Format("Category_Delete_Msg", category.Name),
            loc["Common_Yes"], loc["Common_No"]);

        if (!confirm) return;

        _log.Info($"Category deleted: '{category.Name}'.");
        Items.Remove(category);
        try { await SaveAllCategoriesAsync(); }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error saving after category delete.");
            await Shell.Current.DisplayAlert(loc["Common_Error"], ex.Message, loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task MoveUpAsync(Category category)
    {
        if (MoveItemUp(category))
            await SaveAllCategoriesAsync();
    }

    [RelayCommand]
    private async Task MoveDownAsync(Category category)
    {
        if (MoveItemDown(category))
            await SaveAllCategoriesAsync();
    }

    [RelayCommand]
    private void IncreaseSortOrder() => IncreaseSortOrderBase();

    [RelayCommand]
    private void DecreaseSortOrder() => DecreaseSortOrderBase();

    private async Task SaveAllCategoriesAsync()
    {
        try
        {
            await _dataService.SaveCategoriesAsync(Items.ToList());
            _log.Debug($"Kategorieliste gespeichert: {Items.Count} Kategorie(n).");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Fehler beim Speichern der Kategorieliste.");
            throw;
        }
    }
}
