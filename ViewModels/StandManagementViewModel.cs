using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class StandManagementViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ILogService _log;

    [ObservableProperty]
    private ObservableCollection<Stand> _stands = new();

    [ObservableProperty]
    private Stand? _selectedStand;

    [ObservableProperty]
    private string _newStandName = string.Empty;

    [ObservableProperty]
    private string _activeStandId = string.Empty;

    public StandManagementViewModel(IDataService dataService, ILogService logService)
    {
        _dataService = dataService;
        _log = logService;
    }

    public async Task InitializeAsync()
    {
        await LoadStandsAsync();
    }

    private async Task LoadStandsAsync()
    {
        var data = await _dataService.LoadDataAsync();
        ActiveStandId = data.ActiveStandId;
        Stands.Clear();
        foreach (var stand in data.Stands)
            Stands.Add(stand);
        _log.Debug($"Stand management: {Stands.Count} stand(s) loaded, active='{data.ActiveStandId}'.");
    }

    [RelayCommand]
    private async Task AddStandAsync()
    {
        var name = NewStandName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Error"], loc["Alert_Stand_NoName"], loc["Common_OK"]);
            return;
        }

        var stand = new Stand { Name = name };
        Stands.Add(stand);
        await _dataService.SaveStandsAsync(Stands.ToList());
        _log.Info($"Stand created: '{name}'.");
        NewStandName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteStandAsync(Stand stand)
    {
        if (Stands.Count <= 1)
        {
            var loc = LocalizationService.Instance;
            await Shell.Current.DisplayAlert(loc["Common_Info"], loc["Stand_MinOne_Msg"], loc["Common_OK"]);
            return;
        }

        var loc2 = LocalizationService.Instance;
        var confirmed = await Shell.Current.DisplayAlert(
            loc2["Stand_Delete_Title"],
            loc2.Format("Stand_Delete_Msg", stand.Name),
            loc2["Stand_Delete_Yes"], loc2["Common_Cancel"]);

        if (!confirmed) return;

        _log.Info($"Stand deleted: '{stand.Name}' (ID={stand.Id}).");
        Stands.Remove(stand);

        if (ActiveStandId == stand.Id)
        {
            ActiveStandId = Stands[0].Id;
            await _dataService.SetActiveStandAsync(ActiveStandId);
        }

        await _dataService.SaveStandsAsync(Stands.ToList());
    }

    [RelayCommand]
    private async Task RenameStandAsync(Stand stand)
    {
        var loc3 = LocalizationService.Instance;
        var newName = await Shell.Current.DisplayPromptAsync(
            loc3["Stand_Rename_Title"],
            loc3["Stand_Rename_Prompt"],
            initialValue: stand.Name,
            maxLength: 50,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newName)) return;

        var oldName = stand.Name;
        stand.Name = newName.Trim();
        await _dataService.SaveStandsAsync(Stands.ToList());
        _log.Info($"Stand renamed: '{oldName}' → '{stand.Name}'.");
        var index = Stands.IndexOf(stand);
        Stands[index] = stand;
    }

    [RelayCommand]
    private async Task SelectStandAsync(Stand stand)
    {
        _log.Info($"Active stand changed to '{stand.Name}' (ID={stand.Id}).");
        ActiveStandId = stand.Id;
        await _dataService.SetActiveStandAsync(stand.Id);
        await Shell.Current.GoToAsync("//MainPage/MainPage");
    }
}
