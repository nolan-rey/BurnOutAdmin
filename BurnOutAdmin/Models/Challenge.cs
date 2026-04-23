using SQLite;

namespace BurnOutAdmin.Models;

[Table("Challenges")]
public class Challenge
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [NotNull] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChallengeType Type { get; set; } = ChallengeType.General;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TargetGoal { get; set; }
    public string GoalUnit { get; set; } = string.Empty;
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Upcoming;
    public string RewardDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Ignore] public string StartDateFormatted => StartDate.ToString("dd/MM/yyyy");
    [Ignore] public string EndDateFormatted => EndDate.ToString("dd/MM/yyyy");
    [Ignore] public string DateRange => $"{StartDateFormatted} → {EndDateFormatted}";
    [Ignore] public string TypeText => Type.ToFrenchString();
    [Ignore] public string TypeIcon => Type.ToIcon();

    [Ignore]
    public string StatusText => Status switch
    {
        ChallengeStatus.Upcoming  => "À venir",
        ChallengeStatus.Active    => "En cours",
        ChallengeStatus.Completed => "Terminé",
        ChallengeStatus.Cancelled => "Annulé",
        _                         => "Inconnu"
    };

    [Ignore] public bool IsActive => Status == ChallengeStatus.Active;

    [Ignore]
    public int DaysLeft
    {
        get
        {
            var diff = (EndDate.Date - DateTime.Today).Days;
            return diff < 0 ? 0 : diff;
        }
    }
}

public enum ChallengeStatus
{
    Upcoming,
    Active,
    Completed,
    Cancelled
}
