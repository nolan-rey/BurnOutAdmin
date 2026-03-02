using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ChallengesViewModel : BaseViewModel
{
    private readonly IChallengeService _challengeService;

    [ObservableProperty]
    private ObservableCollection<Challenge> _challenges = new();

    [ObservableProperty]
    private Challenge? _selectedChallenge;

    public ChallengesViewModel(IChallengeService challengeService)
    {
        _challengeService = challengeService;
        Title = "Challenges";
        
        LoadChallengesCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LoadChallengesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var challenges = await _challengeService.GetChallengesAsync();
            
            Challenges.Clear();
            foreach (var challenge in challenges)
            {
                Challenges.Add(challenge);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading challenges: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddChallengeAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Info", 
            "Création de challenge - Fonctionnalité à venir", 
            "OK");
    }

    [RelayCommand]
    private async Task ViewChallengeDetailsAsync(Challenge challenge)
    {
        await Application.Current!.MainPage!.DisplayAlert(
            challenge.Name, 
            $"Participants: {challenge.ParticipantsCount}\nObjectif: {challenge.TargetGoal} {challenge.GoalUnit}\nStatut: {challenge.StatusText}", 
            "OK");
    }
}
