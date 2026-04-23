using BurnOutAdmin.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.Challenges;

public partial class ChallengeParticipantRowViewModel : ObservableObject
{
    public ChallengeParticipant Participant { get; }

    private readonly int    _targetGoal;
    private readonly string _goalUnit;
    private readonly Action<ChallengeParticipantRowViewModel>? _updateAction;
    private readonly Action<ChallengeParticipantRowViewModel>? _removeAction;

    // ── Exposed data ──────────────────────────────────────────────
    public int    Id           => Participant.Id;
    public string ClientName   => Participant.ClientName;
    public double CurrentValue => Participant.CurrentValue;
    public string ProgressText => $"{Participant.CurrentValue} / {_targetGoal} {_goalUnit}";
    public double ProgressRatio =>
        _targetGoal > 0 ? Math.Min(1.0, Participant.CurrentValue / _targetGoal) : 0;
    public string PercentText => $"{(int)(ProgressRatio * 100)} %";

    // Initials for avatar
    public string Initials
    {
        get
        {
            var parts = ClientName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}"
                : (ClientName.Length > 0 ? ClientName[0].ToString().ToUpper() : "?");
        }
    }

    public ChallengeParticipantRowViewModel(
        ChallengeParticipant participant,
        int targetGoal,
        string goalUnit,
        Action<ChallengeParticipantRowViewModel>? updateAction = null,
        Action<ChallengeParticipantRowViewModel>? removeAction = null)
    {
        Participant = participant;
        _targetGoal = targetGoal;
        _goalUnit = goalUnit;
        _updateAction = updateAction;
        _removeAction = removeAction;
    }

    [RelayCommand] private void Update() => _updateAction?.Invoke(this);
    [RelayCommand] private void Remove() => _removeAction?.Invoke(this);
}
