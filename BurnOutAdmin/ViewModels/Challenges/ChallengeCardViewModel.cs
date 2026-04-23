using BurnOutAdmin.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.ViewModels.Challenges;

public partial class ChallengeCardViewModel : ObservableObject
{
    public Challenge Challenge { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private int _participantCount;

    private readonly Action<ChallengeCardViewModel>? _selectAction;
    private readonly Action<ChallengeCardViewModel>? _editAction;
    private readonly Action<ChallengeCardViewModel>? _deleteAction;

    // ── Forwarded from Challenge ──────────────────────────────────
    public int Id                  => Challenge.Id;
    public string Name             => Challenge.Name;
    public string Description      => Challenge.Description;
    public string TypeIcon         => Challenge.TypeIcon;
    public string TypeText         => Challenge.TypeText;
    public string StatusText       => Challenge.StatusText;
    public string DateRange        => Challenge.DateRange;
    public int DaysLeft            => Challenge.DaysLeft;
    public string GoalText         => $"{Challenge.TargetGoal} {Challenge.GoalUnit}";
    public string RewardDescription => Challenge.RewardDescription;

    public string ParticipantLabel =>
        $"{ParticipantCount} participant{(ParticipantCount > 1 ? "s" : "")}";

    // ── Apparence dynamique ───────────────────────────────────────
    public Color StatusColor => Challenge.Status switch
    {
        ChallengeStatus.Active    => Color.FromArgb("#22C55E"),
        ChallengeStatus.Upcoming  => Color.FromArgb("#F59E0B"),
        ChallengeStatus.Completed => Color.FromArgb("#6366F1"),
        ChallengeStatus.Cancelled => Color.FromArgb("#EF4444"),
        _                         => Color.FromArgb("#94A3B8")
    };

    public Color CardBackground  => IsSelected ? Color.FromArgb("#EFF6FF") : Colors.White;
    public Color CardBorderColor => IsSelected ? Color.FromArgb("#60A5FA") : Color.FromArgb("#E2E8F0");
    public Color CardNameColor   => IsSelected ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#1E293B");

    public ChallengeCardViewModel(
        Challenge challenge,
        int participantCount = 0,
        Action<ChallengeCardViewModel>? selectAction = null,
        Action<ChallengeCardViewModel>? editAction = null,
        Action<ChallengeCardViewModel>? deleteAction = null)
    {
        Challenge = challenge;
        _participantCount = participantCount;
        _selectAction = selectAction;
        _editAction = editAction;
        _deleteAction = deleteAction;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBackground));
        OnPropertyChanged(nameof(CardBorderColor));
        OnPropertyChanged(nameof(CardNameColor));
    }

    partial void OnParticipantCountChanged(int value)
    {
        OnPropertyChanged(nameof(ParticipantLabel));
    }

    [RelayCommand] private void Select() => _selectAction?.Invoke(this);
    [RelayCommand] private void Edit()   => _editAction?.Invoke(this);
    [RelayCommand] private void Delete() => _deleteAction?.Invoke(this);
}
