namespace BurnOutAdmin.Models;

public class Programme
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProgrammeType Type { get; set; }
    public int DurationWeeks { get; set; }
    public int SessionsPerWeek { get; set; }
    public ProgrammeLevel Level { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string TypeText => Type switch
    {
        ProgrammeType.Strength => "Force",
        ProgrammeType.Cardio => "Cardio",
        ProgrammeType.Flexibility => "Souplesse",
        ProgrammeType.Mixed => "Mixte",
        _ => "Inconnu"
    };
    
    public string LevelText => Level switch
    {
        ProgrammeLevel.Beginner => "Débutant",
        ProgrammeLevel.Intermediate => "Intermédiaire",
        ProgrammeLevel.Advanced => "Avancé",
        _ => "Inconnu"
    };
}

public enum ProgrammeType
{
    Strength,
    Cardio,
    Flexibility,
    Mixed
}

public enum ProgrammeLevel
{
    Beginner,
    Intermediate,
    Advanced
}
