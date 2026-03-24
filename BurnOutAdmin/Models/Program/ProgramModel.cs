namespace BurnOutAdmin.Models.Program;

public class ProgramModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SessionModel> Sessions { get; set; } = new();
}
