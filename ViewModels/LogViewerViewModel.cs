using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class LogViewerViewModel : ObservableObject
{
    private readonly ILogService _logService;

    [ObservableProperty]
    private string _logContent = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public string LogFilePath => _logService.LogFilePath;

    public LogViewerViewModel(ILogService logService)
    {
        _logService = logService;
    }

    [RelayCommand]
    public async Task LoadLogAsync()
    {
        IsLoading = true;
        try
        {
            LogContent = await _logService.ReadLogAsync();
            if (string.IsNullOrWhiteSpace(LogContent))
                LogContent = "(No log content)";
        }
        catch (Exception ex)
        {
            LogContent = $"Error reading log:\n{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearLogAsync()
    {
        try
        {
            await _logService.ClearLogAsync();
            LogContent = "(Log cleared)";
            _logService.Info("Log cleared manually.");
        }
        catch (Exception ex)
        {
            LogContent = $"Error clearing log:\n{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportLogAsync()
    {
        try
        {
            if (!File.Exists(LogFilePath))
            {
                var loc = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(loc["Common_Info"], loc["Alert_LogExport_NoFile"], loc["Common_OK"]);
                return;
            }

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "FestKasse Log",
                File = new ShareFile(LogFilePath)
            });
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_LogExport_Error", ex.Message), loc["Common_OK"]);
        }
    }
}
