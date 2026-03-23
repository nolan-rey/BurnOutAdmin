using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IProgramAssignmentService
{
    Task AssignProgramAsync(ClientProgramAssignment assignment);
    Task<List<ClientProgramAssignment>> GetAssignmentsForClientAsync(int clientId);
}
