using BurnOutAdmin.Models.Program;
using SQLite;

namespace BurnOutAdmin.Services.ExerciseLibrary;

public class SqliteExerciseLibraryService : IExerciseLibraryService
{
    private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "burnout_exercises.db");
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            _db = new SQLiteAsyncConnection(_dbPath);
            await _db.CreateTableAsync<ExerciseLibraryEntry>();
            await SeedDefaultsAsync();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<ExerciseLibraryEntry>> GetAllAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<ExerciseLibraryEntry>().OrderBy(e => e.CategoryName).ThenBy(e => e.Name).ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await EnsureInitializedAsync();
        var all = await _db!.Table<ExerciseLibraryEntry>().ToListAsync();
        return all.Select(e => e.CategoryName).Distinct().OrderBy(c => c).ToList();
    }

    public async Task<ExerciseLibraryEntry> AddAsync(string name, string category, string muscleGroup = "")
    {
        await EnsureInitializedAsync();
        var entry = new ExerciseLibraryEntry
        {
            Name = name,
            CategoryName = category,
            MuscleGroup = muscleGroup,
            TagsJson = "[]",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
        await _db!.InsertAsync(entry);
        return entry;
    }

    public async Task DeleteAsync(int id)
    {
        await EnsureInitializedAsync();
        await _db!.DeleteAsync<ExerciseLibraryEntry>(id);
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized) await InitializeAsync();
    }

    private async Task SeedDefaultsAsync()
    {
        var count = await _db!.Table<ExerciseLibraryEntry>().CountAsync();
        if (count > 0) return;

        var defaults = new List<ExerciseLibraryEntry>
        {
            // Échauffement
            new() { Name = "Rameur 5 min",        CategoryName = "Échauffement", MuscleGroup = "Cardio",    IsDefault = true },
            new() { Name = "Mobilité Épaules",    CategoryName = "Échauffement", MuscleGroup = "Épaules",   IsDefault = true },
            new() { Name = "Band Pull Apart",     CategoryName = "Échauffement", MuscleGroup = "Dos",       IsDefault = true },
            new() { Name = "Leg Swing",           CategoryName = "Échauffement", MuscleGroup = "Hanches",   IsDefault = true },
            new() { Name = "Hip Circle",          CategoryName = "Échauffement", MuscleGroup = "Hanches",   IsDefault = true },

            // Musculation - Jambes
            new() { Name = "Squat Barre",         CategoryName = "Musculation",  MuscleGroup = "Jambes",    IsDefault = true },
            new() { Name = "Leg Press",           CategoryName = "Musculation",  MuscleGroup = "Jambes",    IsDefault = true },
            new() { Name = "Fentes Marchées",     CategoryName = "Musculation",  MuscleGroup = "Jambes",    IsDefault = true },
            new() { Name = "Romanian Deadlift",   CategoryName = "Musculation",  MuscleGroup = "Ischio",    IsDefault = true },
            new() { Name = "Leg Curl Couché",     CategoryName = "Musculation",  MuscleGroup = "Ischio",    IsDefault = true },

            // Musculation - Poitrine
            new() { Name = "Développé Couché",    CategoryName = "Musculation",  MuscleGroup = "Pectoraux", IsDefault = true },
            new() { Name = "Dips",                CategoryName = "Musculation",  MuscleGroup = "Pectoraux", IsDefault = true },
            new() { Name = "Écarté Haltères",     CategoryName = "Musculation",  MuscleGroup = "Pectoraux", IsDefault = true },

            // Musculation - Dos
            new() { Name = "Soulevé de Terre",    CategoryName = "Musculation",  MuscleGroup = "Dos",       IsDefault = true },
            new() { Name = "Rowing Barre",        CategoryName = "Musculation",  MuscleGroup = "Dos",       IsDefault = true },
            new() { Name = "Tractions",           CategoryName = "Musculation",  MuscleGroup = "Dos",       IsDefault = true },
            new() { Name = "Tirage Poulie Haute", CategoryName = "Musculation",  MuscleGroup = "Dos",       IsDefault = true },

            // Musculation - Épaules
            new() { Name = "Shoulder Press",      CategoryName = "Musculation",  MuscleGroup = "Épaules",   IsDefault = true },
            new() { Name = "Élévations Latérales",CategoryName = "Musculation",  MuscleGroup = "Épaules",   IsDefault = true },

            // Musculation - Bras
            new() { Name = "Curl Barre",          CategoryName = "Musculation",  MuscleGroup = "Biceps",    IsDefault = true },
            new() { Name = "Triceps Corde",       CategoryName = "Musculation",  MuscleGroup = "Triceps",   IsDefault = true },

            // Cardio
            new() { Name = "Bike Erg",            CategoryName = "Cardio",       MuscleGroup = "Cardio",    IsDefault = true },
            new() { Name = "Course 400m",         CategoryName = "Cardio",       MuscleGroup = "Cardio",    IsDefault = true },
            new() { Name = "Corde à Sauter",      CategoryName = "Cardio",       MuscleGroup = "Cardio",    IsDefault = true },
            new() { Name = "Burpees",             CategoryName = "Cardio",       MuscleGroup = "Complet",   IsDefault = true },

            // Récupération
            new() { Name = "Étirements Statiques",CategoryName = "Récupération", MuscleGroup = "Complet",   IsDefault = true },
            new() { Name = "Foam Rolling",        CategoryName = "Récupération", MuscleGroup = "Complet",   IsDefault = true },
            new() { Name = "Gainage Planche",     CategoryName = "Récupération", MuscleGroup = "Abdos",     IsDefault = true },
        };

        foreach (var entry in defaults)
            entry.CreatedAt = DateTime.UtcNow;

        await _db.InsertAllAsync(defaults);
    }
}
