namespace BurnOutAdmin.Models.Program;

public class ExerciseModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public double Weight { get; set; }
    public double Rpe { get; set; }
    public int Order { get; set; }
}
