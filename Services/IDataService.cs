using FestKasse.Models;

namespace FestKasse.Services;

public interface IDataService
{
    Task<AppData> LoadDataAsync();
    Task SaveDataAsync(AppData data);

    // Export selected stands; import merges by stand ID
    Task<string> ExportToJsonAsync(IEnumerable<string> standIds);
    Task ImportFromJsonAsync(string json);
    Task<bool> SyncFromUrlAsync(string url, bool ignoreSslErrors = false);

    // Stand management
    Task<List<Stand>> GetStandsAsync();
    Task SaveStandsAsync(List<Stand> stands);
    Task<Stand?> GetActiveStandAsync();
    Task SetActiveStandAsync(string standId);

    // Per-stand article (operate on active stand)
    Task<List<Article>> GetArticlesAsync();
    Task SaveArticlesAsync(List<Article> articles);
    Task ResetArticlesToDefaultAsync();
    Task ResetToDefaultAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task SaveCategoriesAsync(List<Category> categories);

    // AppSettings (global, separate file)
    Task<AppSettings> GetSettingsAsync();
    AppSettings GetSettingsCached();
    Task SaveSettingsAsync(AppSettings settings);
    Task<string> ExportSettingsToJsonAsync();
    Task ImportSettingsFromJsonAsync(string json);
    Task ResetSettingsToDefaultAsync();
}
