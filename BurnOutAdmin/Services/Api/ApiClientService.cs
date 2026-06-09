using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="IClientService"/> utilisant l'API REST BurnOut.
///
/// Endpoints utilisés :
///   GET  /users             → liste complète de tous les utilisateurs (admin/coach/client)
///   GET  /users/{id}        → fiche utilisateur
///   POST /users             → création d'un utilisateur
///   PUT  /users/{id}        → mise à jour d'un utilisateur
///   DELETE /users/{id}      → suppression
///
/// Mapping rôles/statuts API → statut app :
///   role  : admin / coach / client
///   statut: actif / inactif / suspendu / en_attente
/// </summary>
public class ApiClientService : IClientService
{
    private readonly ApiHttpClient   _api;
    private readonly IApiAuthService _auth;

    public ApiClientService(ApiHttpClient api, IApiAuthService auth)
    {
        _api  = api;
        _auth = auth;
    }

    // ── Lecture ───────────────────────────────────────────────────

    public async Task<List<Client>> GetClientsAsync()
    {
        // GET /users retourne désormais { "success":true, "data":[{id_client, prenom, statut…}] }
        // → même format que ClientListResponseDto, on réutilise le même mapping.
        try
        {
            var response = await _api.GetAsync<ClientListResponseDto>("/users");
            var data = response?.Data;
            if (data is { Count: > 0 })
            {
                Console.WriteLine($"[ApiClientService] /users OK — {data.Count} client(s)");
                var clients = data.Select(MapClientDtoToClient).ToList();

                // Résoudre l'ID SQL de l'utilisateur connecté pour les opérations qui en ont besoin
                // (ex: id_createur pour POST /programmes).
                if (_auth.CurrentUserId is null && _auth.CurrentUserEmail is { } myEmail)
                {
                    var me = clients.FirstOrDefault(c =>
                        string.Equals(c.Email, myEmail, StringComparison.OrdinalIgnoreCase));
                    if (me is not null && me.Id > 0)
                    {
                        _auth.CurrentUserId = me.Id;
                        Console.WriteLine($"[ApiClientService] CurrentUserId résolu : {me.Id} ({myEmail})");
                    }
                }

                return clients;
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientService] GetClientsAsync error: {ex.Message}");
        }

        return [];
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        // 1️⃣ GET /clients/{id} — données SQL complètes
        try
        {
            var response = await _api.GetAsync<ClientResponseDto>($"/clients/{id}");
            if (response?.Data is not null)
                return MapClientDtoToClient(response.Data);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch { /* fallback /users */ }

        // 2️⃣ Fallback GET /users/{id}
        try
        {
            var response = await _api.GetAsync<UserResponseDto>($"/users/{id}");
            if (response?.Data is not null)
                return MapUserToClient(response.Data, id);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientService] GetClientByIdAsync({id}) error: {ex.Message}");
        }

        return null;
    }

    // ── Écriture ──────────────────────────────────────────────────

    public async Task<bool> AddClientAsync(Client client)
    {
        try
        {
            var dto = new CreateClientDto
            {
                Prenom  = client.FirstName,
                Nom     = client.LastName,
                Email   = client.Email,
                Statut  = MapStatutToApi(client.Status),
                NfcUid  = string.IsNullOrWhiteSpace(client.NfcUid) ? null : client.NfcUid,
                Abonnement = client.Subscription is not null
                    ? new CreateAbonnementDto
                    {
                        Type               = MapTypeToApi(client.Subscription.Type),
                        DateDebut          = client.Subscription.StartDate.ToString("yyyy-MM-dd"),
                        DateFin            = client.Subscription.EndDate.ToString("yyyy-MM-dd"),
                        AutoRenouvellement = client.Subscription.AutoRenewal ? 1 : 0
                    }
                    : null
            };

            var result = await _api.PostAsync<CreateClientResponseDto>("/clients", dto);

            if (result?.Success == true && result.ResolvedId.HasValue)
            {
                client.Id = result.ResolvedId.Value;
                return true;
            }

            Console.WriteLine($"[ApiClientService] AddClientAsync failed: {result?.Error}");
            return false;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientService] AddClientAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>Mise à jour du statut uniquement — body minimal { "statut": "..." }.</summary>
    public async Task<bool> UpdateStatusAsync(int clientId, string newStatus)
    {
        try
        {
            var dto = new UpdateStatutDto { Statut = MapStatutToApi(newStatus) };
            var result = await _api.PutAsync<ApiSuccessDto>($"/users/{clientId}", dto);
            if (result?.Success != true)
            {
                Console.WriteLine($"[ApiClientService] UpdateStatusAsync failed: {result?.Error}");
                return false;
            }
            Console.WriteLine($"[ApiClientService] UpdateStatus {clientId} → {dto.Statut} OK");
            return true;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientService] UpdateStatusAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateClientAsync(Client client)
    {
        try
        {
            var clientDto = new UpdateClientDto
            {
                Prenom = client.FirstName,
                Nom    = client.LastName,
                Email  = client.Email,
                Statut = MapStatutToApi(client.Status),
                NfcUid = string.IsNullOrWhiteSpace(client.NfcUid) ? null : client.NfcUid
            };

            var result = await _api.PutAsync<ApiSuccessDto>($"/users/{client.Id}", clientDto);
            if (result?.Success != true)
            {
                Console.WriteLine($"[ApiClientService] UpdateClientAsync failed: {result?.Error}");
                return false;
            }

            // Mise à jour abonnement si présent
            if (client.Subscription is not null)
            {
                var abonnDto = new CreateAbonnementDto
                {
                    Type               = MapTypeToApi(client.Subscription.Type),
                    DateDebut          = client.Subscription.StartDate.ToString("yyyy-MM-dd"),
                    DateFin            = client.Subscription.EndDate.ToString("yyyy-MM-dd"),
                    AutoRenouvellement = client.Subscription.AutoRenewal ? 1 : 0
                };

                try
                {
                    await _api.PutAsync<ApiSuccessDto>($"/clients/{client.Id}/abonnement", abonnDto);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ApiClientService] UpdateAbonnement warning: {ex.Message}");
                }
            }

            return true;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientService] UpdateClientAsync error: {ex.Message}");
            return false;
        }
    }

    // TODO API : endpoint à implémenter côté Slim : PUT /clients/{id}/totem  body { "totem_rang": int|null }
    public async Task<bool> UpdateClientTotemAsync(int clientId, int? totemRang)
    {
        try
        {
            var body = new { totem_rang = totemRang };
            var result = await _api.PutAsync<ApiSuccessDto>($"/clients/{clientId}/totem", body);
            if (result?.Success != true)
            {
                Console.WriteLine($"[ApiClientService] UpdateClientTotemAsync failed: {result?.Error}");
                return false;
            }
            return true;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientService] UpdateClientTotemAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteClientAsync(int id)
    {
        try
        {
            await _api.DeleteAsync($"/clients/{id}");
            return true;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientService] DeleteClientAsync({id}) error: {ex.Message}");
            return false;
        }
    }

    // ── Mapping ClientDto → Client (source : GET /clients) ──────────

    private static Client MapClientDtoToClient(ClientDto dto) => new()
    {
        Id           = dto.ResolvedId,
        FirstName    = dto.Prenom,
        LastName     = dto.Nom,
        Email        = dto.Email,
        Status       = MapStatutToLocal(dto.Statut),
        NfcUid       = string.IsNullOrWhiteSpace(dto.NfcUid) ? null : dto.NfcUid,
        Subscription = dto.Abonnement is not null
            ? MapToSubscription(dto.Abonnement)
            : null
    };

    // ── Mapping UserDto → Client (fallback : GET /users) ──────────

    /// <param name="index">Index 1-based dans la liste — utilisé comme Id quand aucun int n'est disponible.</param>
    private static Client MapUserToClient(UserDto dto, int index) => new()
    {
        Id           = dto.ResolvedId != 0 ? dto.ResolvedId : index,
        FirebaseUid  = dto.Uid,   // conservé pour résoudre l'ID SQL au moment des écritures
        FirstName    = dto.ResolvedFirstName,
        LastName     = dto.ResolvedLastName,
        Email        = dto.Email,
        Status       = ResolveStatus(dto.Statut, dto.Role),
        NfcUid       = string.IsNullOrWhiteSpace(dto.NfcUid) ? null : dto.NfcUid,
        Subscription = dto.Abonnement is not null
            ? MapToSubscription(dto.Abonnement)
            : null
    };

    /// <summary>
    /// Détermine le statut affiché en combinant 'statut' et 'role'.
    /// - Si statut est renseigné → on le traduit
    /// - Sinon → on dérive du role (coach/admin → "Actif", client → "Actif")
    /// </summary>
    private static string ResolveStatus(string? statut, string? role)
    {
        if (!string.IsNullOrWhiteSpace(statut))
            return MapStatutToLocal(statut);

        // Pas de statut : on dérive du rôle
        return role?.ToLowerInvariant() switch
        {
            "admin"  => "Admin",
            "coach"  => "Coach",
            "client" => "Actif",
            _        => "Actif"
        };
    }

    private static Subscription MapToSubscription(AbonnementDto dto) => new()
    {
        Type        = MapTypeToLocal(dto.Type),
        StartDate   = TryParseDate(dto.DateDebut) ?? DateTime.Today,
        EndDate     = TryParseDate(dto.DateFin)   ?? DateTime.Today.AddMonths(1),
        AutoRenewal = dto.AutoRenouvellement == 1
    };

    // ── Helpers statut ────────────────────────────────────────────

    private static string MapStatutToLocal(string s) => s switch
    {
        "actif"      => "Actif",
        "inactif"    => "Inactif",
        "expire"     => "Expiré",
        "en_attente" => "En attente",
        "suspendu"   => "Suspendu",
        _            => s
    };

    private static string MapStatutToApi(string s) => s switch
    {
        "Actif"      => "actif",
        "Inactif"    => "inactif",
        "Expiré"     => "expire",
        "En attente" => "en_attente",
        "Suspendu"   => "suspendu",
        _            => s.ToLowerInvariant()
    };

    private static string MapTypeToLocal(string s) => s switch
    {
        "annuel"      => "Annuel",
        "mensuel"     => "Mensuel",
        "trimestriel" => "Trimestriel",
        _             => s
    };

    private static string MapTypeToApi(string s) => s switch
    {
        "Annuel"      => "annuel",
        "Mensuel"     => "mensuel",
        "Trimestriel" => "trimestriel",
        _             => s.ToLowerInvariant()
    };

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
