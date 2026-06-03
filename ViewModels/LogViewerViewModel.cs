using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Helpers;
using FestKasse.Services;

namespace FestKasse.ViewModels;

/// <summary>Thin display model for a single rolling log file.</summary>
public sealed class LogFileEntry
{
    public string FullPath { get; }
    public LogFileEntry(string fullPath) => FullPath = fullPath;
    public override string ToString() => Path.GetFileName(FullPath);
}

public partial class LogViewerViewModel : ObservableObject
{
    private readonly ILogService _logService;

    [ObservableProperty]
    private string _logContent = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LogFilePath))]
    private LogFileEntry? _selectedLogFile;

    public ObservableCollection<LogFileEntry> LogFiles { get; } = [];

    public string LogFilePath => _selectedLogFile?.FullPath ?? _logService.LogFilePath;

    public LogViewerViewModel(ILogService logService)
    {
        _logService = logService;
    }

    partial void OnSelectedLogFileChanged(LogFileEntry? value)
    {
        if (value is not null)
            LoadLogCommand.Execute(null);
    }

    [RelayCommand]
    public async Task RefreshLogFilesAsync()
    {
        var files = _logService.GetAllLogFiles();

        var previous = _selectedLogFile?.FullPath;

        LogFiles.Clear();
        foreach (var f in files)
            LogFiles.Add(new LogFileEntry(f));

        // Restore previous selection or default to newest
        var match = previous is not null
            ? LogFiles.FirstOrDefault(e => string.Equals(e.FullPath, previous, StringComparison.OrdinalIgnoreCase))
            : null;

        SelectedLogFile = match ?? LogFiles.FirstOrDefault();
    }

    [RelayCommand]
    public async Task LoadLogAsync()
    {
        if (_selectedLogFile is null)
        {
            await RefreshLogFilesAsync();
            return;
        }

        IsLoading = true;
        try
        {
            string content;
            var current = AppConstants.ResolveCurrentLogFile();
            if (string.Equals(_selectedLogFile.FullPath, current, StringComparison.OrdinalIgnoreCase))
                content = await _logService.ReadLogAsync();
            else
                content = await _logService.ReadLogFileAsync(_selectedLogFile.FullPath);

            LogContent = string.IsNullOrWhiteSpace(content) ? "(No log content)" : content;
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
        if (_selectedLogFile is null) return;
        try
        {
            await _logService.DeleteLogFileAsync(_selectedLogFile.FullPath);
            LogContent = "(Log cleared)";
            _logService.Info("Log cleared manually.");
            await RefreshLogFilesAsync();
        }
        catch (Exception ex)
        {
            LogContent = $"Error clearing log:\n{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportLogAsync()
    {
        var path = _selectedLogFile?.FullPath;
        try
        {
            if (path is null || !File.Exists(path))
            {
                var loc = LocalizationService.Instance;
                await Shell.Current.DisplayAlert(loc["Common_Info"], loc["Alert_LogExport_NoFile"], loc["Common_OK"]);
                return;
            }

            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "FestKasse Log",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc.Format("Alert_LogExport_Error", ex.Message), loc["Common_OK"]);
        }
    }
}
