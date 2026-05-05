using System.Text.Json;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services.Api.Dto;
using BurnOutAdmin.Services.ExerciseLibrary;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="IExerciseLibraryService"/> utilisant l'API REST BurnOut.
/// Remplace SqliteExerciseLibraryService — aucune donnée locale.
///
/// Les 27 exercices par défaut (is_default = 1) proviennent de la BDD via la migration.
///
/// Endpoints utilisés :
///   GET    /exercices          → liste complète
///   POST   /exercices          → ajouter un exercice personnalisé
///   DELETE /exercices/{id}     → supprimer un exercice
/// </summary>
public class ApiExerciseLibraryService : IExerciseLibraryService
{
    private readonly ApiHttpClient _api;

    public ApiExerciseLibraryService(ApiHttpClient api) => _api = api;

    // No-op : plus d'initialisation SQLite nécessaire
    public Task InitializeAsync() => Task.CompletedTask;

    // ── Lecture ───────────────────────────────────────────────────

    public async Task<List<ExerciseLibraryEntry>> GetAllAsync()
    {
        // 1️⃣ Format enveloppé { success, data: [...] }
        // IMPORTANT : si la désérialisation réussit (même liste vide), on retourne ici.
        // On ne tombe dans le 2ème try QUE si une exception est levée.
        try
        {
            var response = await _api.GetAsync<ExerciceListResponseDto>("/exercices");
            if (response is not null)
            {
                Console.WriteLine($"[ApiExerciseLibraryService] Format enveloppé OK — {response.Data?.Count ?? 0} exercice(s)");
                return response.Data?.Select(MapToEntry).ToList() ?? [];
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex1)
        {
            Console.WriteLine($"[ApiExerciseLibraryService] Format enveloppé échoué : {ex1.Message} — essai tableau brut");
        }

        // 2️⃣ Format tableau brut [...]
        try
        {
            var list = await _api.GetAsync<List<ExerciceDto>>("/exercices");
            Console.WriteLine($"[ApiExerciseLibraryService] Format tableau brut OK — {list?.Count ?? 0} exercice(s)");
            return list?.Select(MapToEntry).ToList() ?? [];
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiExerciseLibraryService] GetAllAsync error: {ex.Message}");
            return [];
        }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var all = await GetAllAsync();
        return all
            .Select(e => e.CategoryName)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    // ── Écriture ──────────────────────────────────────────────────

    public async Task<ExerciseLibraryEntry> AddAsync(
        string name, string category, string muscleGroup = "")
    {
        try
        {
            var dto = new CreateExerciceDto
            {
                Nom             = name,
                Categorie       = category,
                GroupeMusculaire = muscleGroup
            };

            var result = await _api.PostAsync<CreateExerciceResponseDto>("/exercices", dto);

            return new ExerciseLibraryEntry
            {
                Id           = result?.IdExercice ?? 0,
                Name         = name,
                CategoryName = category,
                MuscleGroup  = muscleGroup,
                TagsJson     = "[]",
                IsDefault    = false,
                CreatedAt    = DateTime.UtcNow
            };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiExerciseLibraryService] AddAsync error: {ex.Message}");
            return new ExerciseLibraryEntry
            {
                Name         = name,
                CategoryName = category,
                MuscleGroup  = muscleGroup,
                TagsJson     = "[]",
                IsDefault    = false,
                CreatedAt    = DateTime.UtcNow
            };
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            await _api.DeleteAsync($"/exercices/{id}");
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiExerciseLibraryService] DeleteAsync({id}) error: {ex.Message}");
        }
    }

    // ── Mapping ───────────────────────────────────────────────────

    private static ExerciseLibraryEntry MapToEntry(ExerciceDto dto) => new()
    {
        Id           = dto.IdExercice,
        Name         = dto.Nom,
        CategoryName = dto.Categorie ?? "Musculation",
        MuscleGroup  = dto.GroupeMusculaire ?? string.Empty,
        TagsJson     = ResolveTagsJson(dto.Tags),
        IsDefault    = dto.IsDefault,
        CreatedAt    = TryParseDate(dto.CreatedAt) ?? DateTime.UtcNow
    };

    /// <summary>
    /// L'API peut retourner tags sous trois formes : null, string JSON, ou tableau JSON.
    /// On normalise toujours en string JSON valide "[]".
    /// </summary>
    private static string ResolveTagsJson(JsonElement? tags)
    {
        if (!tags.HasValue || tags.Value.ValueKind == JsonValueKind.Null)
            return "[]";
        // Tableau JSON → on le re-sérialise en string
        if (tags.Value.ValueKind == JsonValueKind.Array)
            return tags.Value.GetRawText();
        // String → on la retourne telle quelle
        if (tags.Value.ValueKind == JsonValueKind.String)
            return tags.Value.GetString() ?? "[]";
        return "[]";
    }

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
