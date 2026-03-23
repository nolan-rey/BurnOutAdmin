using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IProgrammeService
{
    Task<List<Programme>> GetProgrammesAsync();
    Task<Programme?> GetProgrammeByIdAsync(int id);
    Task<int> GetActiveProgrammesCountAsync();
}
