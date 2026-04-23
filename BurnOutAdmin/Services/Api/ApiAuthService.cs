using System.Net.Http.Json;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de l'authentification Firebase JWT via l'API CallOfPhoenix.
///
/// Stratégie de stockage du token :
///   - Preferences (sync) pour le token brut et la date d'expiration
///   - Le token Firebase expire après 1 heure (documenté dans l'API)
///   - À l'expiration, l'utilisateur est redirigé vers la page de connexion
/// </summary>
public class ApiAuthService : IApiAuthService
{
    private const string PrefKeyToken  = "api_jwt_token";
    private const string PrefKeyExpiry = "api_jwt_expiry";
    private const string PrefKeyEmail  = "api_user_email";

    private readonly HttpClient _http;

    public ApiAuthService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(AppConfiguration.ApiBaseUrl),
            Timeout     = TimeSpan.FromSeconds(15)
        };
    }

    // ── IApiAuthService ──────────────────────────────────────────

    public bool IsAuthenticated
    {
        get
        {
            var token  = Preferences.Default.Get(PrefKeyToken, string.Empty);
            var expiry = Preferences.Default.Get(PrefKeyExpiry, string.Empty);
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiry))
                return false;

            if (!DateTime.TryParse(expiry, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var exp))
                return false;

            return DateTime.UtcNow < exp;
        }
    }

    public string? CurrentUserEmail =>
        Preferences.Default.Get(PrefKeyEmail, string.Empty) is { Length: > 0 } e ? e : null;

    public string? GetToken()
    {
        if (!IsAuthenticated) return null;
        return Preferences.Default.Get(PrefKeyToken, string.Empty) is { Length: > 0 } t ? t : null;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var body = new LoginRequestDto { Email = email, Password = password };
            var response = await _http.PostAsJsonAsync("/users/login", body);

            var dto = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (dto is null)
                return "Réponse invalide du serveur.";

            if (!dto.Success || string.IsNullOrEmpty(dto.Token))
                return dto.Error ?? "Identifiants incorrects.";

            // Stocker le token — expire dans 1 heure (Firebase standard)
            var expiry = DateTime.UtcNow.AddHours(1).ToString("O");
            Preferences.Default.Set(PrefKeyToken,  dto.Token);
            Preferences.Default.Set(PrefKeyExpiry, expiry);
            Preferences.Default.Set(PrefKeyEmail,  email);

            return null; // succès
        }
        catch (HttpRequestException ex)
        {
            return $"Erreur réseau : {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "Le serveur ne répond pas (timeout).";
        }
        catch (Exception ex)
        {
            return $"Erreur inattendue : {ex.Message}";
        }
    }

    public void Logout()
    {
        Preferences.Default.Remove(PrefKeyToken);
        Preferences.Default.Remove(PrefKeyExpiry);
        Preferences.Default.Remove(PrefKeyEmail);
    }
}
