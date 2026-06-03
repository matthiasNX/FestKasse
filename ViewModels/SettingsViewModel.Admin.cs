using CommunityToolkit.Mvvm.Input;
using FestKasse.Services;
using FestKasse.Views;

namespace FestKasse.ViewModels;

public partial class SettingsViewModel
{
    // ── Settings export / import / reset ─────────────────────────────────

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        try
        {
            var json = await _dataService.ExportSettingsToJsonAsync();
            var fileName = $"FestKasse_settings_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "FestKasse Einstellungen Export",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error exporting settings.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_ExportSettings_Error", ex.Message), loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/json", "text/plain" } }
                }),
                PickerTitle = "Einstellungen-JSON auswählen"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                await _dataService.ImportSettingsFromJsonAsync(json);
                _logService.Info($"Settings imported from file '{result.FileName}'.");
                var loc = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(loc["Common_Success"], loc["Alert_ImportSettings_Success"], loc["Common_OK"]);
                await LoadSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error importing settings.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_ImportSettings_Error", ex.Message), loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private async Task ResetSettingsToDefaultAsync()
    {
        var loc = LocalizationService.Instance;
        var confirmed = await Shell.Current.DisplayAlert(
            loc["Settings_ResetSettings_Confirm_Title"],
            loc["Settings_ResetSettings_Confirm_Msg"],
            loc["Settings_ResetSettings_Confirm_Yes"],
            loc["Common_Cancel"]);

        if (!confirmed) return;

        try
        {
            await _dataService.ResetSettingsToDefaultAsync();
            _logService.Info("Settings reset to defaults.");
            var loc2 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc2["Common_Done"], loc2["Alert_ResetSettings_Done"], loc2["Common_OK"]);
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error resetting settings.");
            var loc2 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc2["Common_Error"], loc2.Format("Alert_ResetSettings_Error", ex.Message), loc2["Common_OK"]);
        }
    }

    // ── Articles reset ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ResetArticlesToDefaultAsync()
    {
        var loc = LocalizationService.Instance;
        var confirmed = await Shell.Current.DisplayAlert(
            loc["Settings_ResetArticles_Confirm_Title"],
            loc["Settings_ResetArticles_Confirm_Msg"],
            loc["Settings_ResetArticles_Confirm_Yes"],
            loc["Common_Cancel"]);

        if (!confirmed) return;

        try
        {
            await _dataService.ResetToDefaultAsync();
            _logService.Info("Master data reset to defaults.");
            var loc2 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc2["Common_Done"], loc2["Alert_ResetArticles_Done"], loc2["Common_OK"]);
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error resetting master data.");
            var loc2 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc2["Common_Error"], loc2.Format("Alert_ResetArticles_Error", ex.Message), loc2["Common_OK"]);
        }
    }

    // ── Order-history management ──────────────────────────────────────────

    [RelayCommand]
    private async Task ClearOrderHistoryAsync()
    {
        var loc = LocalizationService.Instance;
        var confirmed = await Shell.Current.DisplayAlert(
            loc["Settings_OrderHistory_Clear_Confirm_Title"],
            loc.Format("Alert_ClearHistory_Confirm_Msg", OrderCount),
            loc["Settings_OrderHistory_Clear_Confirm_Yes"],
            loc["Common_Cancel"]);

        if (!confirmed) return;

        try
        {
            await _orderHistoryService.ClearAllAsync();
            OrderCount = 0;
            _logService.Info("Order history cleared manually.");
            var loc2 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc2["Common_Done"], loc2["Alert_ClearHistory_Done"], loc2["Common_OK"]);
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error clearing order history.");
            var loc2 = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc2["Common_Error"], loc2.Format("Alert_ClearHistory_Error", ex.Message), loc2["Common_OK"]);
        }
    }

    // ── Navigation helpers ────────────────────────────────────────────────

    [RelayCommand]
    private Task NavigateToLogViewerAsync()
        => Shell.Current.GoToAsync(nameof(LogViewerPage));

    [RelayCommand]
    private Task NavigateToCategoriesAsync()
        => Shell.Current.GoToAsync(nameof(CategoryManagementPage));

    // ── Refresh ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        await LoadSettingsAsync();
        var loc = LocalizationService.Instance;
        await Shell.Current.DisplayAlert(loc["Common_Updated"], loc["Alert_Settings_Updated"], loc["Common_OK"]);
    }
}
