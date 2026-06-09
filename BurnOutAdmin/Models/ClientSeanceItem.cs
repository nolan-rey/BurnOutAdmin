namespace BurnOutAdmin.Models;

/// <summary>
/// Une séance assignée à un client (réalisée ou à venir).
/// Représente une ligne <c>seance_builder_assignations</c> jointe au template
/// <c>seances_builder</c>.
/// </summary>
public class ClientSeanceItem
{
    public string IdAssignation { get; set; } = string.Empty;
    public int    IdSeanceBuilder { get; set; }

    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int    ExerciseCount { get; set; }
    public int    CategoryCount { get; set; }

    public DateTime? DatePrevue       { get; set; }
    public DateTime? DateRealisation  { get; set; }
    public string?   Commentaire      { get; set; }

    public bool   IsCompleted   => DateRealisation is not null;
    public string StatusText    => IsCompleted ? "Réalisée" : "À faire";
    public string DatePrevueDisplay      => DatePrevue?.ToString("dd/MM/yyyy") ?? "—";
    public string DateRealisationDisplay => DateRealisation?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—";
}
