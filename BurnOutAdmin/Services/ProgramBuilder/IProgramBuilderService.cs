using BurnOutAdmin.Models.Program;

namespace BurnOutAdmin.Services.ProgramBuilder;

public interface IProgramBuilderService
{
    Task<ProgramModel> GetSampleProgramAsync();
}
