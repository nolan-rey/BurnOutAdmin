using SQLite;

namespace BurnOutAdmin.Models.Program;

[Table("ExerciseLibrary")]
public class ExerciseLibraryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    [NotNull]
    public string CategoryName { get; set; } = string.Empty;

    public string MuscleGroup { get; set; } = string.Empty;

    public string TagsJson { get; set; } = "[]";

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
