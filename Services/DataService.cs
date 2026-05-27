using FestKasse.Models;
using System.Text.Json;

namespace FestKasse.Services;

public class DataService : IDataService
{
    private readonly string _dataFilePath;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly HttpClient _httpClient;
    private AppData? _cachedData;
    private AppSettings? _cachedSettings;

    public DataService()
    {
        _dataFilePath = Path.Combine(FileSystem.AppDataDirectory, "festkasse_data.json");
        _settingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "festkasse_settings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<AppData> LoadDataAsync()
    {
        if (_cachedData != null)
            return _cachedData;

        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_dataFilePath);
                var parsed = JsonSerializer.Deserialize<AppData>(json, _jsonOptions);
                if (parsed != null && parsed.Stands.Count > 0)
                    _cachedData = parsed;
                else
                    System.Diagnostics.Debug.WriteLine("Cachedatei leer oder ungültig – wird neu erstellt.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Korrupte Cachedatei wird gelöscht: {ex.Message}");
            }

            if (_cachedData == null)
            {
                try { File.Delete(_dataFilePath); } catch { }
            }
        }

        if (_cachedData == null)
        {
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Daten: {ex.Message}");
            throw;
        }
    }

    private async Task<AppData?> LoadDefaultDataAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("default_data.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<AppData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Standard-Daten konnten nicht geladen werden: {ex.Message}");
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
            // Persist in background to avoid blocking the UI
            _ = SaveDataAsync(data);
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
            }
            catch { }
        }

        _cachedSettings ??= new AppSettings();
        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _cachedSettings = settings;
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_settingsFilePath, json);
    }

    public async Task<string> ExportSettingsToJsonAsync()
    {
        var settings = await GetSettingsAsync();
        return JsonSerializer.Serialize(settings, _jsonOptions);
    }

    public async Task ImportSettingsFromJsonAsync(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Ungültiges JSON-Format für Einstellungen");
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
            using var stream = await FileSystem.OpenAppPackageFileAsync("default_settings.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Standard-Einstellungen konnten nicht geladen werden: {ex.Message}");
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
                ?? throw new InvalidOperationException("Ungültiges JSON-Format");

            if (imported.Stands.Count == 0)
                throw new InvalidOperationException("Die Datei enthält keine Stände.");

            var data = await LoadDataAsync();
            foreach (var stand in imported.Stands)
            {
                // Renumber sort order
                for (int i = 0; i < stand.Articles.Count; i++)
                    stand.Articles[i].SortOrder = i;

                var existing = data.Stands.FirstOrDefault(s => s.Id == stand.Id);
                if (existing != null)
                {
                    existing.Name = stand.Name;
                    existing.Articles = stand.Articles;
                    existing.Categories = stand.Categories;
                }
                else
                {
                    data.Stands.Add(stand);
                }
            }

            // Keep active stand valid
            if (data.Stands.All(s => s.Id != data.ActiveStandId))
                data.ActiveStandId = data.Stands[0].Id;

            await SaveDataAsync(data);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON-Parsing-Fehler: {ex.Message}", ex);
        }
    }

    public async Task<bool> SyncFromUrlAsync(string url, bool ignoreSslErrors = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            HttpClient client = _httpClient;
            HttpClient? tempClient = null;
            if (ignoreSslErrors)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                tempClient = new HttpClient(handler) { Timeout = _httpClient.Timeout };
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

            // Vollständiges Reset mit heruntergeladenem JSON
            var json = await response.Content.ReadAsStringAsync();
            var imported = JsonSerializer.Deserialize<AppData>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Ungültiges JSON-Format");
            if (imported.Stands.Count == 0)
                throw new InvalidOperationException("Die Datei enthält keine Stände.");

            _cachedData = null;
            try { if (File.Exists(_dataFilePath)) File.Delete(_dataFilePath); } catch { }
            await SaveDataAsync(imported);
            return true;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP-Fehler beim Sync: {ex.Message}");
            throw new InvalidOperationException($"Verbindungsfehler: {ex.Message}", ex);
        }
        catch (TaskCanceledException)
        {
            throw new InvalidOperationException("Zeitüberschreitung bei der Verbindung");
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
        // Clear in-memory cache
        _cachedData = null;

        // Delete persisted data file
        try { if (File.Exists(_dataFilePath)) File.Delete(_dataFilePath); } catch { }

        // Reload from default_data.json and persist
        var defaultData = await LoadDefaultDataAsync() ?? new AppData();
        if (defaultData.Stands.Count > 0)
            await SaveDataAsync(defaultData);
        else
            _cachedData = defaultData;
    }
}
