using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Client HTTP générique pour l'API CallOfPhoenix.
///
/// Injecte automatiquement le header <c>Authorization: Bearer &lt;token&gt;</c>
/// sur chaque requête. Lève <see cref="UnauthorizedAccessException"/> si l'API
/// répond 401 (token expiré ou invalide).
/// </summary>
public class ApiHttpClient
{
    private readonly HttpClient      _http;
    private readonly IApiAuthService _auth;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiHttpClient(IApiAuthService auth)
    {
        _auth = auth;
        _http = new HttpClient
        {
            BaseAddress = new Uri(AppConfiguration.ApiBaseUrl),
            Timeout     = TimeSpan.FromSeconds(20)
        };
    }

    // ── Méthodes publiques ────────────────────────────────────────

    public Task<T?> GetAsync<T>(string path)
        => SendAsync<T>(HttpMethod.Get, path);

    public Task<T?> PostAsync<T>(string path, object body)
        => SendAsync<T>(HttpMethod.Post, path, body);

    public Task<T?> PutAsync<T>(string path, object body)
        => SendAsync<T>(HttpMethod.Put, path, body);

    public Task DeleteAsync(string path)
        => SendAsync<object>(HttpMethod.Delete, path);

    // ── Implémentation interne ────────────────────────────────────

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);

        // Injecter le token d'authentification
        var token = _auth.GetToken();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Sérialiser le body si présent
        if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"Erreur réseau : {ex.Message}", ex);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("Le serveur ne répond pas (timeout).");
        }

        // Gérer les erreurs HTTP
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException(
                "Token invalide ou expiré. Veuillez vous reconnecter.");

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new ApiException(
                $"Erreur API {(int)response.StatusCode} : {err}");
        }

        // Réponse vide (DELETE)
        if (response.Content.Headers.ContentLength == 0)
            return default;

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }
}

/// <summary>Exception levée lors d'une erreur de communication avec l'API.</summary>
public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
    public ApiException(string message, Exception inner) : base(message, inner) { }
}
