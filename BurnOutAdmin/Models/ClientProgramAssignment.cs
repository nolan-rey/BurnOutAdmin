namespace BurnOutAdmin.Models;

public class ClientProgramAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ClientId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.Now;
    public List<AssignedSession> Sessions { get; set; } = new();
}

public class AssignedSession
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<AssignedCategory> Categories { get; set; } = new();
}

public class AssignedCategory
{
    public string Name { get; set; } = string.Empty;
    public List<AssignedExercise> Exercises { get; set; } = new();
}

public class AssignedExercise
{
    public string Name { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public double Weight { get; set; }
    public double Rpe { get; set; }
}
