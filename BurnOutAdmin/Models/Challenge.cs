namespace BurnOutAdmin.Models;

public class Challenge
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ParticipantsCount { get; set; }
    public int TargetGoal { get; set; }
    public string GoalUnit { get; set; } = string.Empty;
    public ChallengeStatus Status { get; set; }
    public string? RewardDescription { get; set; }
    
    public string StartDateFormatted => StartDate.ToString("dd/MM/yyyy");
    public string EndDateFormatted => EndDate.ToString("dd/MM/yyyy");
    
    public string StatusText => Status switch
    {
        ChallengeStatus.Upcoming => "À venir",
        ChallengeStatus.Active => "En cours",
        ChallengeStatus.Completed => "Terminé",
        ChallengeStatus.Cancelled => "Annulé",
        _ => "Inconnu"
    };
    
    public bool IsActive => Status == ChallengeStatus.Active;
}

public enum ChallengeStatus
{
    Upcoming,
    Active,
    Completed,
    Cancelled
}
