using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponses GET /users (liste complète des utilisateurs) ────────────────────

/// <summary>
/// DTO pour un utilisateur retourné par GET /users.
///
/// L'API retourne : {"success":true,"users":[{"uid":"...","email":"...","displayName":null}]}
///
/// Champs Firebase Auth : uid (string), email, displayName
/// Champs SQL optionnels : id_user/id, prenom, nom, role, statut, nfc_uid, created_at
/// </summary>
public class UserDto
{
    // ── Firebase Auth ─────────────────────────────────────────────────────────
    /// <summary>Firebase UID (string, ex: "T3Ibaxq1aMcipHNYBBhBgZcfF3l2").</summary>
    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    /// <summary>Nom affiché Firebase — peut être null.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    // ── Identifiant SQL — optionnel selon version API ─────────────────────────
    [JsonPropertyName("id_user")]
    public int? IdUser { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    // ── Identité SQL (optionnel) ──────────────────────────────────────────────
    [JsonPropertyName("prenom")]
    public string? Prenom { get; set; }

    [JsonPropertyName("nom")]
    public string? Nom { get; set; }

    // ── Email (présent dans les deux formats) ─────────────────────────────────
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    // ── Rôle & statut SQL (optionnel) ────────────────────────────────────────
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("statut")]
    public string? Statut { get; set; }

    // ── NFC (optionnel) ───────────────────────────────────────────────────────
    [JsonPropertyName("nfc_uid")]
    public string? NfcUid { get; set; }

    // ── Dates (optionnel) ─────────────────────────────────────────────────────
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    // ── Abonnement joint (optionnel) ──────────────────────────────────────────
    [JsonPropertyName("abonnement")]
    public AbonnementDto? Abonnement { get; set; }

    // ── Helpers computed ──────────────────────────────────────────────────────

    /// <summary>ID numérique : id_user ?? id ?? 0.</summary>
    [JsonIgnore]
    public int ResolvedId => IdUser ?? Id ?? 0;

    /// <summary>
    /// Prénom résolu : champ SQL 'prenom' → partie avant espace dans displayName → partie avant '@' dans email.
    /// </summary>
    [JsonIgnore]
    public string ResolvedFirstName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Prenom)) return Prenom;
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                var parts = DisplayName.Trim().Split(' ', 2);
                return parts[0];
            }
            // Fallback : username de l'email
            var at = Email.IndexOf('@');
            return at > 0 ? Email[..at] : Email;
        }
    }

    /// <summary>
    /// Nom de famille résolu : champ SQL 'nom' → partie après espace dans displayName → "".
    /// </summary>
    [JsonIgnore]
    public string ResolvedLastName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Nom)) return Nom;
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                var parts = DisplayName.Trim().Split(' ', 2);
                return parts.Length > 1 ? parts[1] : string.Empty;
            }
            return string.Empty;
        }
    }
}

/// <summary>
/// Format enveloppé pour GET /users :
///   {"success":true,"users":[...]}
/// La clé est "users", pas "data".
/// </summary>
public class UserListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("users")]
    public List<UserDto> Users { get; set; } = new();
}

/// <summary>Format enveloppé { success, data: {...} } pour GET /users/{id}.</summary>
public class UserResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public UserDto? Data { get; set; }
}

// ── Réponses GET /clients ─────────────────────────────────────────────────────

public class ClientListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ClientDto> Data { get; set; } = new();
}

public class ClientResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ClientDto? Data { get; set; }
}

// ── DTO principal Client ──────────────────────────────────────────────────────

public class ClientDto
{
    /// <summary>Champ "id" retourné par GET /clients (spec v2).</summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    /// <summary>Champ "id_client" — variante selon implémentation serveur.</summary>
    [JsonPropertyName("id_client")]
    public int? IdClient { get; set; }

    /// <summary>Résolution : id ?? id_client ?? 0.</summary>
    [JsonIgnore]
    public int ResolvedId => Id ?? IdClient ?? 0;

    [JsonPropertyName("prenom")]
    public string Prenom { get; set; } = string.Empty;

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("statut")]
    public string Statut { get; set; } = string.Empty;

    [JsonPropertyName("nfc_uid")]
    public string? NfcUid { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("abonnement")]
    public AbonnementDto? Abonnement { get; set; }
}

// ── Abonnement imbriqué ───────────────────────────────────────────────────────

public class AbonnementDto
{
    [JsonPropertyName("id_abonnement")]
    public int IdAbonnement { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("date_debut")]
    public string? DateDebut { get; set; }

    [JsonPropertyName("date_fin")]
    public string? DateFin { get; set; }

    [JsonPropertyName("auto_renouvellement")]
    public int AutoRenouvellement { get; set; }
}

// ── Création (POST /clients) ──────────────────────────────────────────────────

public class CreateClientDto
{
    [JsonPropertyName("prenom")]
    public string Prenom { get; set; } = string.Empty;

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("statut")]
    public string Statut { get; set; } = "actif";

    [JsonPropertyName("nfc_uid")]
    public string? NfcUid { get; set; }

    [JsonPropertyName("abonnement")]
    public CreateAbonnementDto? Abonnement { get; set; }
}

public class CreateAbonnementDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("date_debut")]
    public string DateDebut { get; set; } = string.Empty;

    [JsonPropertyName("date_fin")]
    public string DateFin { get; set; } = string.Empty;

    [JsonPropertyName("auto_renouvellement")]
    public int AutoRenouvellement { get; set; }
}

// ── Mise à jour client (PUT /clients/{id}) ────────────────────────────────────

public class UpdateClientDto
{
    [JsonPropertyName("prenom")]
    public string Prenom { get; set; } = string.Empty;

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("statut")]
    public string Statut { get; set; } = string.Empty;

    [JsonPropertyName("nfc_uid")]
    public string? NfcUid { get; set; }
}

// ── Réponse création ──────────────────────────────────────────────────────────

public class CreateClientResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Spec v2 retourne "id". Variante "id_client" en fallback.</summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("id_client")]
    public int? IdClient { get; set; }

    [JsonIgnore]
    public int? ResolvedId => Id ?? IdClient;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
