using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponse GET /exercices ────────────────────────────────────────────────────

public class ExerciceListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ExerciceDto> Data { get; set; } = new();
}

// ── DTO principal ─────────────────────────────────────────────────────────────

public class ExerciceDto
{
    [JsonPropertyName("id_exercice")]
    public int IdExercice { get; set; }

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("url_video")]
    public string? UrlVideo { get; set; }

    [JsonPropertyName("categorie")]
    public string? Categorie { get; set; }

    [JsonPropertyName("groupe_musculaire")]
    public string? GroupeMusculaire { get; set; }

    /// <summary>
    /// L'API retourne un booléen JSON (true/false), pas un entier.
    /// </summary>
    [JsonPropertyName("is_default")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Tags peut être null, une string JSON ou un tableau JSON selon la version API.
    /// On utilise JsonElement? pour accepter n'importe quel type JSON.
    /// </summary>
    [JsonPropertyName("tags")]
    public JsonElement? Tags { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

// ── Création (POST /exercices) ────────────────────────────────────────────────

public class CreateExerciceDto
{
    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("categorie")]
    public string? Categorie { get; set; }

    [JsonPropertyName("groupe_musculaire")]
    public string? GroupeMusculaire { get; set; }
}

// ── Réponse création ──────────────────────────────────────────────────────────

public class CreateExerciceResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("id_exercice")]
    public int? IdExercice { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
