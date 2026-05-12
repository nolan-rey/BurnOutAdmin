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
}
