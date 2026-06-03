using FestKasse.Helpers;
using FestKasse.Models;
using System.Text.Json;

namespace FestKasse.Services;

public class DataService : IDataService
{
    private readonly string _dataFilePath;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogService _log;
    private AppData? _cachedData;
    private AppSettings? _cachedSettings;
    private static readonly HttpClient _sharedClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public DataService(ILogService logService)
    {
        _log = logService;
        _dataFilePath = AppConstants.DataFilePath;
        _settingsFilePath = AppConstants.SettingsFilePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        _log.Debug($"DataService initialized. Data path: {_dataFilePath}");
    }

    public async Task<AppData> LoadDataAsync()
    {
        if (_cachedData != null)
            return _cachedData;

        _log.Debug("Loading application data from file.");

        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_dataFilePath);
                var parsed = JsonSerializer.Deserialize<AppData>(json, _jsonOptions);
                if (parsed != null && parsed.Stands.Count > 0)
                {
                    _cachedData = parsed;
                    _log.Info($"Data loaded: {parsed.Stands.Count} stand(s).");
                }
                else
                    _log.Warning("Cache file empty or invalid – will be recreated.");
            }
            catch (Exception ex)
            {
                _log.Exception(ex, "Corrupt cache file is being deleted.");
            }

            if (_cachedData == null)
            {
                try { File.Delete(_dataFilePath); }
                catch (Exception delEx) { _log.Warning($"Could not delete corrupt data file: {delEx.Message}"); }
            }
        }

        if (_cachedData == null)
        {
            _log.Info("No valid data found – loading default data.");
            _cachedData = await LoadDefaultDataAsync() ?? new AppData();
            if (_cachedData.Stands.Count > 0)
                await SaveDataAsync(_cachedData);
        }

        // Ensure ActiveStandId is valid
        if (string.IsNullOrEmpty(_cachedData.ActiveStandId) ||
            _cachedData.Stands.All(s => s.Id != _cachedData.ActiveStandId))
        {
            _cachedData.ActiveStandId = _cachedData.Stands.Count > 0
                ? _cachedData.Stands[0].Id
                : string.Empty;
            _log.Debug($"ActiveStandId set to '{_cachedData.ActiveStandId}'.");
        }

        return _cachedData;
    }

    public async Task SaveDataAsync(AppData data)
    {
        try
        {
            _cachedData = data;
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(_dataFilePath, json);
            _log.Debug("Application data saved successfully.");
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Error saving application data.");
            throw;
        }
    }

    private async Task<AppData?> LoadDefaultDataAsync()
    {
        try
        {
            _log.Debug("Loading default data from app package.");
            using var stream = await FileSystem.OpenAppPackageFileAsync("default_data.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<AppData>(json, _jsonOptions);
            _log.Info($"Default data loaded: {result?.Stands.Count ?? 0} stand(s).");
            return result;
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Default data could not be loaded.");
            return null;
        }
    }

    // ─── Stand management ─────────────────────────────────────────────────────

    public async Task<List<Stand>> GetStandsAsync()
    {
        var data = await LoadDataAsync();
        return data.Stands;
    }

    public async Task SaveStandsAsync(List<Stand> stands)
    {
        var data = await LoadDataAsync();
        data.Stands = stands;
        await SaveDataAsync(data);
    }

    public async Task<Stand?> GetActiveStandAsync()
    {
        var data = await LoadDataAsync();
        return data.Stands.FirstOrDefault(s => s.Id == data.ActiveStandId)
            ?? data.Stands.FirstOrDefault();
    }

    public async Task SetActiveStandAsync(string standId)
    {
        var data = await LoadDataAsync();
        if (data.Stands.Any(s => s.Id == standId))
        {
            data.ActiveStandId = standId;
            _log.Info($"Active stand changed to ID '{standId}'.");
            try { await SaveDataAsync(data); }
            catch (Exception ex) { _log.Exception(ex, "Error persisting active stand change."); }
        }
        else
        {
            _log.Warning($"SetActiveStandAsync: Stand with ID '{standId}' not found.");
        }
    }

    // ─── Per-stand articles ────────────────────────────────────────────────────

    public async Task<List<Article>> GetArticlesAsync()
    {
        var stand = await GetActiveStandAsync();
        return stand?.Articles ?? new List<Article>();
    }

    public async Task SaveArticlesAsync(List<Article> articles)
    {
        var data = await LoadDataAsync();
        var stand = data.Stands.First(s => s.Id == data.ActiveStandId);
        stand.Articles = articles;
        await SaveDataAsync(data);
    }

    // ─── AppSettings (global, separate file) ──────────────────────────────────

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                _log.Debug("Settings loaded from file.");
            }
            catch (Exception ex)
            {
                _log.Exception(ex, "Settings file could not be read – using defaults.");
            }
        }

        _cachedSettings ??= new AppSettings();
        return _cachedSettings;
    }

    public AppSettings GetSettingsCached() => _cachedSettings ?? new AppSettings();

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _cachedSettings = settings;
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_settingsFilePath, json);
        _log.Debug("Settings saved.");
    }

    public async Task<string> ExportSettingsToJsonAsync()
    {
        var settings = await GetSettingsAsync();
        return JsonSerializer.Serialize(settings, _jsonOptions);
    }

    public async Task ImportSettingsFromJsonAsync(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Invalid JSON format for settings");
        _log.Info("Settings imported from JSON.");
        await SaveSettingsAsync(settings);
    }

    public async Task ResetSettingsToDefaultAsync()
    {
        var settings = await LoadDefaultSettingsAsync() ?? new AppSettings();
        await SaveSettingsAsync(settings);
    }

    private async Task<AppSettings?> LoadDefaultSettingsAsync()
    {
        try
        {
            _log.Debug("Loading default settings from app package.");
            using var stream = await FileSystem.OpenAppPackageFileAsync("default_settings.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _log.Exception(ex, "Default settings could not be loaded.");
            return null;
        }
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var stand = await GetActiveStandAsync();
        return stand?.Categories.OrderBy(c => c.SortOrder).ToList() ?? new List<Category>();
    }

    public async Task SaveCategoriesAsync(List<Category> categories)
    {
        var data = await LoadDataAsync();
        var stand = data.Stands.First(s => s.Id == data.ActiveStandId);
        stand.Categories = categories;
        await SaveDataAsync(data);
    }

    // ─── Export / Import ──────────────────────────────────────────────────────

    public async Task<string> ExportToJsonAsync(IEnumerable<string> standIds)
    {
        var data = await LoadDataAsync();
        var ids = standIds.ToHashSet();
        var selected = data.Stands.Where(s => ids.Contains(s.Id)).ToList();
        var exportData = new AppData
        {
            Stands = selected,
            ActiveStandId = selected.Any(s => s.Id == data.ActiveStandId)
                ? data.ActiveStandId
                : selected.FirstOrDefault()?.Id ?? string.Empty
        };
        return JsonSerializer.Serialize(exportData, _jsonOptions);
    }

    public async Task ImportFromJsonAsync(string json)
    {
        try
        {
            var imported = JsonSerializer.Deserialize<AppData>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Invalid JSON format");

            if (imported.Stands.Count == 0)
                throw new InvalidOperationException("The file contains no stands.");

            _log.Info($"Import: {imported.Stands.Count} stand(s) found.");

            var data = await LoadDataAsync();
            foreach (var stand in imported.Stands)
            {
                for (int i = 0; i < stand.Articles.Count; i++)
                    stand.Articles[i].SortOrder = i;

                var existing = data.Stands.FirstOrDefault(s => s.Id == stand.Id);
                if (existing != null)
                {
                    _log.Debug($"Import: Stand '{stand.Name}' updated.");
                    existing.Name = stand.Name;
                    existing.Articles = stand.Articles;
                    existing.Categories = stand.Categories;
                }
                else
                {
                    _log.Debug($"Import: New stand '{stand.Name}' added.");
                    data.Stands.Add(stand);
                }
            }

            if (data.Stands.All(s => s.Id != data.ActiveStandId))
                data.ActiveStandId = data.Stands[0].Id;

            await SaveDataAsync(data);
            _log.Info("Import completed successfully.");
        }
        catch (JsonException ex)
        {
            _log.Exception(ex, "JSON parse error during import.");
            throw new InvalidOperationException($"JSON parse error: {ex.Message}", ex);
        }
    }

    public async Task<bool> SyncFromUrlAsync(string url, bool ignoreSslErrors = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            _log.Info($"Starting sync from URL: {url} (ignore SSL: {ignoreSslErrors})");

            HttpClient client = _sharedClient;
            HttpClient? tempClient = null;
            if (ignoreSslErrors)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                tempClient = new HttpClient(handler) { Timeout = _sharedClient.Timeout };
                client = tempClient;
            }

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url);
            }
            finally
            {
                tempClient?.Dispose();
            }
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var imported = JsonSerializer.Deserialize<AppData>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Invalid JSON format");
            if (imported.Stands.Count == 0)
                throw new InvalidOperationException("The file contains no stands.");

            _log.Info($"Sync successful: {imported.Stands.Count} stand(s) received.");

            _cachedData = null;
            try { if (File.Exists(_dataFilePath)) File.Delete(_dataFilePath); } catch { }
            await SaveDataAsync(imported);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _log.Exception(ex, $"HTTP error during sync from '{url}'.");
            throw new InvalidOperationException($"Connection error: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _log.Exception(ex, $"Timeout during sync from '{url}'.");
            throw new InvalidOperationException("Connection timed out");
        }
    }

    public async Task ResetArticlesToDefaultAsync()
    {
        var defaultData = await LoadDefaultDataAsync();
        if (defaultData == null || defaultData.Stands.Count == 0)
            return;

        var data = await LoadDataAsync();
        var stand = data.Stands.First(s => s.Id == data.ActiveStandId);
        stand.Articles = defaultData.Stands[0].Articles;
        await SaveDataAsync(data);
    }

    public async Task ResetToDefaultAsync()
    {
        _log.Warning("Resetting all data to defaults.");
        _cachedData = null;
        try { if (File.Exists(_dataFilePath)) File.Delete(_dataFilePath); } catch { }
        var defaultData = await LoadDefaultDataAsync() ?? new AppData();
        if (defaultData.Stands.Count > 0)
            await SaveDataAsync(defaultData);
        else
            _cachedData = defaultData;
        _log.Info("Data reset to defaults successfully.");
    }
}
