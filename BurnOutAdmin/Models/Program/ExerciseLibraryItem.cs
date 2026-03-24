namespace BurnOutAdmin.Models.Program;

public class ExerciseLibraryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}
