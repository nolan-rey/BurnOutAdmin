using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponses GET /challenges ──────────────────────────────────────────────────

public class ChallengeListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ChallengeDto> Data { get; set; } = new();
}

public class ChallengeResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ChallengeDto? Data { get; set; }
}

// ── DTO principal Challenge ───────────────────────────────────────────────────

public class ChallengeDto
{
    [JsonPropertyName("id_challenge")]
    public int IdChallenge { get; set; }

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("objectif")]
    public int Objectif { get; set; }

    [JsonPropertyName("unite_objectif")]
    public string? UniteObjectif { get; set; }

    [JsonPropertyName("statut")]
    public string Statut { get; set; } = string.Empty;

    [JsonPropertyName("recompense_description")]
    public string? RecompenseDescription { get; set; }

    [JsonPropertyName("date_debut")]
    public string? DateDebut { get; set; }

    [JsonPropertyName("date_fin")]
    public string? DateFin { get; set; }

    [JsonPropertyName("id_ligue")]
    public int? IdLigue { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("participants_count")]
    public int ParticipantsCount { get; set; }
}

// ── Participants ──────────────────────────────────────────────────────────────

public class ParticipantListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ParticipantDto> Data { get; set; } = new();
}

public class ParticipantDto
{
    [JsonPropertyName("id_participant")]
    public int IdParticipant { get; set; }

    [JsonPropertyName("id_challenge")]
    public int IdChallenge { get; set; }

    [JsonPropertyName("id_client")]
    public int IdClient { get; set; }

    [JsonPropertyName("nom_client")]
    public string NomClient { get; set; } = string.Empty;

    [JsonPropertyName("valeur_actuelle")]
    public double ValeurActuelle { get; set; }

    [JsonPropertyName("joined_at")]
    public string? JoinedAt { get; set; }
}

// ── Création (POST /challenges) ───────────────────────────────────────────────

public class CreateChallengeDto
{
    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("objectif")]
    public int Objectif { get; set; }

    [JsonPropertyName("unite_objectif")]
    public string? UniteObjectif { get; set; }

    [JsonPropertyName("statut")]
    public string Statut { get; set; } = "a_venir";

    [JsonPropertyName("recompense_description")]
    public string? RecompenseDescription { get; set; }

    [JsonPropertyName("date_debut")]
    public string DateDebut { get; set; } = string.Empty;

    [JsonPropertyName("date_fin")]
    public string DateFin { get; set; } = string.Empty;
}

// ── Mise à jour (PUT /challenges/{id}) ───────────────────────────────────────

public class UpdateChallengeDto
{
    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("objectif")]
    public int Objectif { get; set; }

    [JsonPropertyName("unite_objectif")]
    public string? UniteObjectif { get; set; }

    [JsonPropertyName("statut")]
    public string Statut { get; set; } = string.Empty;

    [JsonPropertyName("recompense_description")]
    public string? RecompenseDescription { get; set; }

    [JsonPropertyName("date_debut")]
    public string DateDebut { get; set; } = string.Empty;

    [JsonPropertyName("date_fin")]
    public string DateFin { get; set; } = string.Empty;
}

// ── Ajout participant (POST /challenges/{id}/participants) ────────────────────

public class AddParticipantDto
{
    [JsonPropertyName("id_client")]
    public int IdClient { get; set; }

    [JsonPropertyName("nom_client")]
    public string NomClient { get; set; } = string.Empty;
}

// ── Mise à jour progression ───────────────────────────────────────────────────

public class UpdateProgressDto
{
    [JsonPropertyName("valeur_actuelle")]
    public double ValeurActuelle { get; set; }
}

// ── Réponse création challenge ────────────────────────────────────────────────

public class CreateChallengeResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("id_challenge")]
    public int? IdChallenge { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
