using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ChallengesViewModel : BaseViewModel
{
    private readonly IChallengeService _challengeService;
    private readonly IAlertService _alertService;

    [ObservableProperty]
    private ObservableCollection<Challenge> _challenges = new();

    [ObservableProperty]
    private Challenge? _selectedChallenge;

    public ChallengesViewModel(IChallengeService challengeService, IAlertService alertService)
    {
        _challengeService = challengeService;
        _alertService = alertService;
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
        await _alertService.AlertAsync("Info", "Création de challenge - Fonctionnalité à venir");
    }

    [RelayCommand]
    private async Task ViewChallengeDetailsAsync(Challenge challenge)
    {
        await _alertService.AlertAsync(
            challenge.Name,
            $"Participants: {challenge.ParticipantsCount}\nObjectif: {challenge.TargetGoal} {challenge.GoalUnit}\nStatut: {challenge.StatusText}");
    }
}
