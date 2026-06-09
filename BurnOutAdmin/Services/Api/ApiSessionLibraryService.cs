using System.Text.Json;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services.Api.Dto;
using BurnOutAdmin.Services.SessionLibrary;
using Microsoft.Maui.Storage;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="ISessionLibraryService"/> utilisant l'API REST BurnOut.
///
/// L'API ne retourne pas le champ data_json dans la liste GET /seances-builder.
/// Solution : cache local persistant via Preferences MAUI.
/// La clé "session_data_{id}" est écrite lors de chaque sauvegarde/mise à jour
/// et lue lors du chargement de la liste — transparent pour l'UI.
///
/// Endpoints utilisés :
///   GET    /seances-builder          → liste des séances sauvegardées
///   POST   /seances-builder          → sauvegarder une nouvelle séance
///   PUT    /seances-builder/{id}     → mettre à jour une séance
///   DELETE /seances-builder/{id}     → supprimer une séance
/// </summary>
public class ApiSessionLibraryService : ISessionLibraryService
{
    private readonly ApiHttpClient _api;

    // Clé Preferences pour le DataJson local d'une séance
    private static string DataJsonKey(int id) => $"session_data_{id}";

    public ApiSessionLibraryService(ApiHttpClient api) => _api = api;

    // No-op : plus d'initialisation SQLite nécessaire
    public Task InitializeAsync() => Task.CompletedTask;

    // ── Lecture ───────────────────────────────────────────────────

    public async Task<List<SavedSessionEntry>> GetAllSessionsAsync()
    {
        // 1️⃣ Format enveloppé { success, data: [...] }  — format confirmé par les logs
        try
        {
            var response = await _api.GetAsync<SeanceBuilderListResponseDto>("/seances-builder");
            if (response is not null)
            {
                Console.WriteLine($"[ApiSessionLibraryService] OK — {response.Data?.Count ?? 0} séance(s)");
                return response.Data?.Select(MapToEntry).ToList() ?? [];
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex1)
        {
            Console.WriteLine($"[ApiSessionLibraryService] Format enveloppé échoué : {ex1.Message} — essai tableau brut");
        }

        // 2️⃣ Fallback format tableau brut [...]
        try
        {
            var list = await _api.GetAsync<List<SeanceBuilderDto>>("/seances-builder");
            if (list is not null)
            {
                Console.WriteLine($"[ApiSessionLibraryService] Tableau brut OK — {list.Count} séance(s)");
                return list.Select(MapToEntry).ToList();
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiSessionLibraryService] GetAllSessionsAsync error: {ex.Message}");
        }

        return [];
    }

    // ── Écriture ──────────────────────────────────────────────────

    public async Task<SavedSessionEntry> SaveSessionAsync(
        string name, string description, SessionModel session)
    {
        try
        {
            var exerciseCount = session.Categories
                .SelectMany(c => c.SubCategories)
                .SelectMany(sc => sc.Exercises)
                .Count();

            var dataJson = JsonSerializer.Serialize(session);

            var dto = new CreateSeanceBuilderDto
            {
                Nom           = name,
                Description   = description,
                ExerciseCount = exerciseCount,
                CategoryCount = session.Categories.Count,
                DataJson      = dataJson
            };

            var result = await _api.PostAsync<CreateSeanceBuilderResponseDto>(
                "/seances-builder", dto);

            var newId = result?.IdSeanceBuilder ?? 0;

            // ── Persister le DataJson localement ─────────────────────
            // L'API ne retourne pas data_json dans GET /seances-builder,
            // on le stocke dans Preferences pour pouvoir éditer plus tard.
            if (newId > 0)
            {
                Preferences.Set(DataJsonKey(newId), dataJson);
                Console.WriteLine($"[ApiSessionLibraryService] DataJson mis en cache local pour id={newId}");
            }

            return new SavedSessionEntry
            {
                Id            = newId,
                Name          = name,
                Description   = description,
                DataJson      = dataJson,
                ExerciseCount = exerciseCount,
                CategoryCount = session.Categories.Count,
                CreatedAt     = DateTime.UtcNow
            };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiSessionLibraryService] SaveSessionAsync error: {ex.Message}");
            return new SavedSessionEntry
            {
                Id            = 0,
                Name          = name,
                Description   = description,
                DataJson      = JsonSerializer.Serialize(session),
                ExerciseCount = session.Categories
                    .SelectMany(c => c.SubCategories)
                    .SelectMany(sc => sc.Exercises).Count(),
                CategoryCount = session.Categories.Count,
                CreatedAt     = DateTime.UtcNow
            };
        }
    }

    // ── Mise à jour ───────────────────────────────────────────────

    public async Task UpdateSessionAsync(int id, string name, string description, SessionModel session)
    {
        try
        {
            var exerciseCount = session.Categories
                .SelectMany(c => c.SubCategories)
                .SelectMany(sc => sc.Exercises)
                .Count();

            var dataJson = JsonSerializer.Serialize(session);

            var dto = new UpdateSeanceBuilderDto
            {
                Nom           = name,
                Description   = description,
                ExerciseCount = exerciseCount,
                CategoryCount = session.Categories.Count,
                DataJson      = dataJson
            };

            await _api.PutAsync<CreateSeanceBuilderResponseDto>($"/seances-builder/{id}", dto);

            // ── Mettre à jour le cache local ──────────────────────────
            Preferences.Set(DataJsonKey(id), dataJson);
            Console.WriteLine($"[ApiSessionLibraryService] UpdateSessionAsync({id}) OK — cache mis à jour");
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiSessionLibraryService] UpdateSessionAsync({id}) error: {ex.Message}");
        }
    }

    // ── Suppression ───────────────────────────────────────────────

    public async Task DeleteSessionAsync(int id)
    {
        try
        {
            await _api.DeleteAsync($"/seances-builder/{id}");
            // Nettoyer le cache local
            Preferences.Remove(DataJsonKey(id));
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiSessionLibraryService] DeleteSessionAsync({id}) error: {ex.Message}");
        }
    }

    // ── Chargement du modèle complet (édition / visualisation) ───
    // TODO API : endpoint à implémenter côté Slim : GET /seances-builder/{id}/full
    // Retourne le SessionModel reconstitué (catégories, sous-catégories, exercices).
    public async Task<SessionModel?> LoadSessionModelAsync(int builderId)
    {
        if (builderId <= 0) return null;
        try
        {
            // Tentative simple : décoder data_json côté GET /seances-builder/{id}.
            var dto = await _api.GetAsync<SeanceBuilderDto>($"/seances-builder/{builderId}");
            if (dto is null || string.IsNullOrWhiteSpace(dto.DataJson) || dto.DataJson == "{}")
            {
                Console.WriteLine($"[ApiSessionLibraryService] LoadSessionModelAsync({builderId}) : data_json absent — stub vide");
                return null;
            }
            return System.Text.Json.JsonSerializer.Deserialize<SessionModel>(dto.DataJson);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiSessionLibraryService] LoadSessionModelAsync({builderId}) error: {ex.Message}");
            return null;
        }
    }

    // ── Mapping ───────────────────────────────────────────────────

    private static SavedSessionEntry MapToEntry(SeanceBuilderDto dto)
    {
        // L'API ne retourne pas data_json dans la liste → lire depuis le cache local
        var dataJson = dto.DataJson;
        if (string.IsNullOrWhiteSpace(dataJson) || dataJson == "{}")
        {
            var cached = Preferences.Get(DataJsonKey(dto.IdSeanceBuilder), string.Empty);
            if (!string.IsNullOrWhiteSpace(cached) && cached != "{}")
            {
                dataJson = cached;
                Console.WriteLine($"[ApiSessionLibraryService] DataJson restauré depuis cache local pour id={dto.IdSeanceBuilder}");
            }
        }

        return new SavedSessionEntry
        {
            Id            = dto.IdSeanceBuilder,
            Name          = dto.Nom,
            Description   = dto.Description ?? string.Empty,
            DataJson      = dataJson,
            ExerciseCount = dto.ExerciseCount,
            CategoryCount = dto.CategoryCount,
            CreatedAt     = TryParseDate(dto.CreatedAt) ?? DateTime.UtcNow
        };
    }

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
