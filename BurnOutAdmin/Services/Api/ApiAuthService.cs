using System.Net.Http.Json;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

public class ApiAuthService : IApiAuthService
{
    private const string PrefKeyToken  = "api_jwt_token";
    private const string PrefKeyExpiry = "api_jwt_expiry";
    private const string PrefKeyEmail  = "api_user_email";
    private const string PrefKeyUserId = "api_user_id";

    private readonly HttpClient _http;

    public ApiAuthService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(AppConfiguration.ApiBaseUrl),
            Timeout     = TimeSpan.FromSeconds(15)
        };
    }

    public bool IsAuthenticated
    {
        get
        {
            var token  = Preferences.Default.Get(PrefKeyToken,  string.Empty);
            var expiry = Preferences.Default.Get(PrefKeyExpiry, string.Empty);
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiry)) return false;
            if (!DateTime.TryParse(expiry, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var exp)) return false;
            return DateTime.UtcNow < exp;
        }
    }

    public string? CurrentUserEmail =>
        Preferences.Default.Get(PrefKeyEmail, string.Empty) is { Length: > 0 } e ? e : null;

    public int? CurrentUserId
    {
        get
        {
            var id = Preferences.Default.Get(PrefKeyUserId, 0);
            return id > 0 ? id : null;
        }
        set
        {
            if (value is > 0)
                Preferences.Default.Set(PrefKeyUserId, value.Value);
            else
                Preferences.Default.Remove(PrefKeyUserId);
        }
    }

    public string? GetToken()
    {
        if (!IsAuthenticated) return null;
        return Preferences.Default.Get(PrefKeyToken, string.Empty) is { Length: > 0 } t ? t : null;
    }

    // ── Login ─────────────────────────────────────────────────────

    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/users/login",
                new LoginRequestDto { Email = email, Password = password });

            var dto = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (dto is null) return "Réponse invalide du serveur.";
            if (!dto.Success || string.IsNullOrEmpty(dto.Token))
                return dto.Error ?? "Identifiants incorrects.";

            StoreToken(dto.Token, email);
            return null;
        }
        catch (HttpRequestException ex) { return $"Erreur réseau : {ex.Message}"; }
        catch (TaskCanceledException)   { return "Le serveur ne répond pas (timeout)."; }
        catch (Exception ex)            { return $"Erreur inattendue : {ex.Message}"; }
    }

    // ── Register ──────────────────────────────────────────────────

    public async Task<string?> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/users/register", request);

            // 404 = endpoint pas encore déployé
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return "L'inscription en ligne n'est pas encore disponible.\nContactez votre administrateur pour créer votre compte.";

            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                return "L'inscription n'est pas encore disponible sur ce serveur.";

            var dto = await response.Content.ReadFromJsonAsync<RegisterResponseDto>();
            if (dto is null) return "Réponse invalide du serveur.";
            if (!dto.Success) return dto.Error ?? "Erreur lors de la création du compte.";

            // Auto-login après inscription réussie
            var loginError = await LoginAsync(request.Email, request.Password);
            return loginError; // null = succès total
        }
        catch (HttpRequestException ex) { return $"Erreur réseau : {ex.Message}"; }
        catch (TaskCanceledException)   { return "Le serveur ne répond pas (timeout)."; }
        catch (Exception ex)            { return $"Erreur inattendue : {ex.Message}"; }
    }

    // ── Logout ────────────────────────────────────────────────────

    public void Logout()
    {
        Preferences.Default.Remove(PrefKeyToken);
        Preferences.Default.Remove(PrefKeyExpiry);
        Preferences.Default.Remove(PrefKeyEmail);
        Preferences.Default.Remove(PrefKeyUserId);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void StoreToken(string token, string email)
    {
        Preferences.Default.Set(PrefKeyToken,  token);
        Preferences.Default.Set(PrefKeyExpiry, DateTime.UtcNow.AddHours(1).ToString("O"));
        Preferences.Default.Set(PrefKeyEmail,  email);
    }
}
