namespace BurnOutAdmin.Models;

/// <summary>
/// Représente une séance attachée à un programme (jointure programmes ↔ seances).
/// </summary>
public class ProgrammeSeance
{
    /// <summary>ID SQL de la séance dans le programme (seances.id_seance).</summary>
    public int Id { get; set; }

    public int ProgrammeId { get; set; }

    /// <summary>Référence optionnelle au template d'origine (seances_builder.id_seance_builder).</summary>
    public int? SeanceBuilderId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ExerciseCount { get; set; }
    public int CategoryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
