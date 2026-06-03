using CommunityToolkit.Mvvm.Input;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class SettingsViewModel
{
    // ── Logo management ───────────────────────────────────────────────────

    private async Task LoadLogoPreviewAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_settings.LogoBase64))
            {
                var bytes = Convert.FromBase64String(_settings.LogoBase64);
                LogoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
                HasLogo = true;
            }
            else
            {
                LogoPreview = null;
                HasLogo = false;
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error decoding logo preview.");
            LogoPreview = null;
            HasLogo = false;
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Warning"], loc["Alert_Logo_LoadError"], loc["Common_OK"]);
            _settings.LogoBase64 = null;
        }
    }

    [RelayCommand]
    private async Task SelectLogoAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Logo auswählen"
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _settings.LogoBase64 = Convert.ToBase64String(memoryStream.ToArray());
                await LoadLogoPreviewAsync();
            }
        }
        catch (Exception ex)
        {
            _logService.Exception(ex, "Error picking logo.");
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_Logo_PickError", ex.Message), loc["Common_OK"]);
        }
    }

    [RelayCommand]
    private Task RemoveLogoAsync()
    {
        _settings.LogoBase64 = null;
        LogoPreview = null;
        HasLogo = false;
        return Task.CompletedTask;
    }
}
