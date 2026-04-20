namespace BurnOutAdmin.Models.Program;

public class ExerciseLibraryItem
{
    public int DbId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}
