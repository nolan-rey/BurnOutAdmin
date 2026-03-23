using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ProgrammesViewModel : BaseViewModel
{
    private readonly IProgrammeService _programmeService;

    [ObservableProperty]
    private ObservableCollection<Programme> _programmes = new();

    [ObservableProperty]
    private Programme? _selectedProgramme;

    public ProgrammesViewModel(IProgrammeService programmeService)
    {
        _programmeService = programmeService;
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
        await Application.Current!.MainPage!.DisplayAlert(
            "Info", 
            "Création de programme - Fonctionnalité à venir", 
            "OK");
    }

    [RelayCommand]
    private async Task EditProgrammeAsync(Programme programme)
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Info", 
            $"Édition du programme: {programme.Name} - Fonctionnalité à venir", 
            "OK");
    }
}
