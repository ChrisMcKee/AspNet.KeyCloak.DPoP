using System.Text.Json.Serialization;

namespace IntegrationTests;

/// <summary>
/// Helper class to get tokens from KeyCloak for testing.
/// </summary>
public class KeyCloakTokenHelper
{
    private static readonly HttpClient HttpClient = new();
    private readonly string _domain;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public KeyCloakTokenHelper(string domain, string clientId, string clientSecret, string audience)
    {
        _domain = domain ?? throw new ArgumentNullException(nameof(domain));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        // audience is not passed to KeyCloak token request - it is enforced server-side
        // via audience mappers on the client/scope configuration in the realm
        _ = audience ?? throw new ArgumentNullException(nameof(audience));
    }

    /// <summary>
    /// Gets a valid access token from KeyCloak using client credentials flow.
    /// </summary>
    /// <returns>A valid access token.</returns>
    public async Task<string> GetAccessTokenAsync()
    {
        var tokenUrl = $"http://{_domain}/protocol/openid-connect/token";

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            })
        };

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content);

        if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain access token from KeyCloak");
        }

        return tokenResponse.AccessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
