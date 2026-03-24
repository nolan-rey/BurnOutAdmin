using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public class MockProgramAssignmentService : IProgramAssignmentService
{
    private readonly List<ClientProgramAssignment> _assignments = new();

    public Task AssignProgramAsync(ClientProgramAssignment assignment)
    {
        _assignments.Add(assignment);
        return Task.CompletedTask;
    }

    public Task<List<ClientProgramAssignment>> GetAssignmentsForClientAsync(int clientId)
    {
        var result = _assignments.Where(a => a.ClientId == clientId).ToList();
        return Task.FromResult(result);
    }
}
