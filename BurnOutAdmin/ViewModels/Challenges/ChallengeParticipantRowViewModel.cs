using BurnOutAdmin.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.ViewModels.Challenges;

public partial class ChallengeParticipantRowViewModel : ObservableObject
{
    public ChallengeParticipant Participant { get; }

    public int Rank { get; }

    private readonly int    _targetGoal;
    private readonly string _goalUnit;
    private readonly Action<ChallengeParticipantRowViewModel>? _removeAction;

    // ── Data ─────────────────────────────────────────────────────
    public int    Id           => Participant.Id;
    public int    ClientId     => Participant.ClientId;
    public string ClientName   => Participant.ClientName;
    public double CurrentValue => Participant.CurrentValue;
    public string ProgressText => $"{Participant.CurrentValue} / {_targetGoal} {_goalUnit}";

    public double ProgressRatio =>
        _targetGoal > 0 ? Math.Min(1.0, Participant.CurrentValue / _targetGoal) : 0;

    public string PercentText => $"{(int)(ProgressRatio * 100)} %";
    public string RankDisplay => Rank.ToString();

    public string Initials
    {
        get
        {
            var parts = ClientName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}"
                : ClientName.Length > 0 ? ClientName[0].ToString().ToUpper() : "?";
        }
    }

    // ── Couleurs podium ───────────────────────────────────────────
    public Color RankBadgeBackground => Rank switch
    {
        1 => Color.FromArgb("#F59E0B"), // or
        2 => Color.FromArgb("#94A3B8"), // argent
        3 => Color.FromArgb("#CD7C2F"), // bronze
        _ => Color.FromArgb("#E2E8F0")  // neutre
    };

    public Color RankBadgeTextColor => Rank switch
    {
        1 or 2 or 3 => Colors.White,
        _            => Color.FromArgb("#64748B")
    };

    public Color RowBackground => Rank switch
    {
        1 => Color.FromArgb("#FFFBEB"), // fond doré très clair
        2 => Color.FromArgb("#F8FAFC"), // fond argenté très clair
        3 => Color.FromArgb("#FFF7ED"), // fond bronze très clair
        _ => Colors.White
    };

    public Color RankAccentColor => Rank switch
    {
        1 => Color.FromArgb("#F59E0B"),
        2 => Color.FromArgb("#94A3B8"),
        3 => Color.FromArgb("#CD7C2F"),
        _ => Color.FromArgb("#E2E8F0")
    };

    public Color ProgressBarColor => Rank switch
    {
        1 => Color.FromArgb("#F59E0B"),
        2 => Color.FromArgb("#94A3B8"),
        3 => Color.FromArgb("#B45309"),
        _ => Color.FromArgb("#512BD4")
    };

    // Icône trophée pour le top 3
    public bool   IsTopThree   => Rank <= 3;
    public string TopThreeIcon => "\uEBD2"; // emoji_events (trophy MaterialIcons)

    // ─────────────────────────────────────────────────────────────
    public ChallengeParticipantRowViewModel(
        ChallengeParticipant participant,
        int rank,
        int targetGoal,
        string goalUnit,
        Action<ChallengeParticipantRowViewModel>? removeAction = null)
    {
        Participant  = participant;
        Rank         = rank;
        _targetGoal  = targetGoal;
        _goalUnit    = goalUnit;
        _removeAction = removeAction;
    }

    [RelayCommand] private void Remove() => _removeAction?.Invoke(this);
}
