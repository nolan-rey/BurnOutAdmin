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
    /// <summary>Spec v2 : "id". Variante "id_exercice" en fallback.</summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("id_exercice")]
    public int? IdExercice { get; set; }

    /// <summary>id ?? id_exercice ?? 0</summary>
    [JsonIgnore]
    public int ResolvedId => Id ?? IdExercice ?? 0;

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
    /// L'API peut retourner is_default sous différentes formes selon le serveur :
    ///   - bool : true/false
    ///   - int  : 1/0   (MySQL TINYINT)
    ///   - string : "1"/"0" ou "true"/"false"
    /// On accepte les trois et on convertit dans <see cref="ResolvedIsDefault"/>.
    /// </summary>
    [JsonPropertyName("is_default")]
    public JsonElement? IsDefaultRaw { get; set; }

    [JsonIgnore]
    public bool ResolvedIsDefault
    {
        get
        {
            if (!IsDefaultRaw.HasValue) return false;
            var v = IsDefaultRaw.Value;
            return v.ValueKind switch
            {
                JsonValueKind.True   => true,
                JsonValueKind.False  => false,
                JsonValueKind.Number => v.TryGetInt32(out var n) && n != 0,
                JsonValueKind.String => v.GetString() is { } s
                                        && (s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }
    }

    /// <summary>
    /// Tags peut être null, une string JSON ou un tableau JSON selon la version API.
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

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("id_exercice")]
    public int? IdExercice { get; set; }

    [JsonIgnore]
    public int? ResolvedId => Id ?? IdExercice;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
