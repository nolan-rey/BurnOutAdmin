using SQLite;

namespace BurnOutAdmin.Models.Program;

[Table("SavedSessions")]
public class SavedSessionEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [NotNull] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}"; // SessionModel sérialisé
    public int ExerciseCount { get; set; }
    public int CategoryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
