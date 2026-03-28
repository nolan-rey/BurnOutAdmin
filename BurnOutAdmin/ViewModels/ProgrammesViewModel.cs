using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ProgrammesViewModel : BaseViewModel
{
    private readonly IProgrammeService _programmeService;
    private readonly IAlertService _alertService;

    [ObservableProperty]
    private ObservableCollection<Programme> _programmes = new();

    [ObservableProperty]
    private Programme? _selectedProgramme;

    public ProgrammesViewModel(IProgrammeService programmeService, IAlertService alertService)
    {
        _programmeService = programmeService;
        _alertService = alertService;
        Title = "Programmes";

        LoadProgrammesCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LoadProgrammesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var programmes = await _programmeService.GetProgrammesAsync();
            
            Programmes.Clear();
            foreach (var programme in programmes)
            {
                Programmes.Add(programme);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading programmes: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddProgrammeAsync()
    {
        await _alertService.AlertAsync("Info", "Création de programme - Fonctionnalité à venir");
    }

    [RelayCommand]
    private async Task EditProgrammeAsync(Programme programme)
    {
        await _alertService.AlertAsync("Info", $"Édition du programme: {programme.Name} - Fonctionnalité à venir");
    }
}
