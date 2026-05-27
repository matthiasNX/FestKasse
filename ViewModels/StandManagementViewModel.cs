using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FestKasse.Models;
using FestKasse.Services;

namespace FestKasse.ViewModels;

public partial class StandManagementViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    [ObservableProperty]
    private ObservableCollection<Stand> _stands = new();

    [ObservableProperty]
    private Stand? _selectedStand;

    [ObservableProperty]
    private string _newStandName = string.Empty;

    [ObservableProperty]
    private string _activeStandId = string.Empty;

    public StandManagementViewModel(IDataService dataService)
    {
        _dataService = dataService;
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
    }

    [RelayCommand]
    private async Task AddStandAsync()
    {
        var name = NewStandName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await Shell.Current.DisplayAlert("Fehler", "Bitte einen Namen eingeben.", "OK");
            return;
        }

        var stand = new Stand { Name = name };
        Stands.Add(stand);
        await _dataService.SaveStandsAsync(Stands.ToList());
        NewStandName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteStandAsync(Stand stand)
    {
        if (Stands.Count <= 1)
        {
            await Shell.Current.DisplayAlert("Hinweis", "Mindestens ein Stand muss vorhanden sein.", "OK");
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Stand löschen",
            $"Stand \"{stand.Name}\" wirklich löschen? Alle zugehörigen Artikel gehen verloren.",
            "Ja, löschen", "Abbrechen");

        if (!confirmed) return;

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
        var newName = await Shell.Current.DisplayPromptAsync(
            "Stand umbenennen",
            "Neuer Name:",
            initialValue: stand.Name,
            maxLength: 50,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newName)) return;

        stand.Name = newName.Trim();
        await _dataService.SaveStandsAsync(Stands.ToList());
        // Refresh list to reflect name change
        var index = Stands.IndexOf(stand);
        Stands[index] = stand;
    }

    [RelayCommand]
    private async Task SelectStandAsync(Stand stand)
    {
        ActiveStandId = stand.Id;
        await _dataService.SetActiveStandAsync(stand.Id);
        await Shell.Current.GoToAsync("//MainPage/MainPage");
    }
}
