using SQLite;

namespace BurnOutAdmin.Models;

[Table("ChallengeParticipants")]
public class ChallengeParticipant
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int ChallengeId { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty; // dénormalisé pour l'affichage
    public double CurrentValue { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
