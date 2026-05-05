using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IProgrammeService
{
    Task<List<Programme>> GetProgrammesAsync();
    Task<Programme?> GetProgrammeByIdAsync(int id);
    Task<int> GetActiveProgrammesCountAsync();

    /// <summary>Crée un programme via l'API. Retourne le programme avec son Id renseigné, ou null si échec.</summary>
    Task<Programme?> CreateProgrammeAsync(Programme programme);

    /// <summary>Met à jour un programme existant via l'API.</summary>
    Task<bool> UpdateProgrammeAsync(Programme programme);

    /// <summary>Supprime un programme via l'API.</summary>
    Task<bool> DeleteProgrammeAsync(int id);
}
