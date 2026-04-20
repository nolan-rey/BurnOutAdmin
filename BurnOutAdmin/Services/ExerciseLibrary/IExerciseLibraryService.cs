using BurnOutAdmin.Models.Program;

namespace BurnOutAdmin.Services.ExerciseLibrary;

public interface IExerciseLibraryService
{
    Task InitializeAsync();
    Task<List<ExerciseLibraryEntry>> GetAllAsync();
    Task<List<string>> GetCategoriesAsync();
    Task<ExerciseLibraryEntry> AddAsync(string name, string category, string muscleGroup = "");
    Task DeleteAsync(int id);
}
