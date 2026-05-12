using BurnOutAdmin.Models;
using BurnOutAdmin.Models.Program;

namespace BurnOutAdmin.Services;

/// <summary>
/// Gestion des séances attachées à un programme.
///   GET    /programmes/{id}/seances             → liste des séances du programme
///   POST   /programmes/{id}/seances             → attacher une séance au programme
///   DELETE /programmes/{id}/seances/{seanceId}  → détacher une séance
/// </summary>
public interface IProgrammeSeanceService
{
    Task<List<ProgrammeSeance>> GetSeancesAsync(int programmeId);
    Task<ProgrammeSeance?> AddSeanceFromTemplateAsync(int programmeId, SavedSessionEntry template, int order);
    Task<bool> RemoveSeanceAsync(int programmeId, int seanceId);

    /// <summary>Met à jour l'ordre et/ou le nom/description d'une séance attachée.</summary>
    Task<bool> UpdateSeanceAsync(int programmeId, int seanceId, int? order, string? name, string? description);
}
