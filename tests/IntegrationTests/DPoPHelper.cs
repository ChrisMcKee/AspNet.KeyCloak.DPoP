using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.IdentityModel.Tokens;

namespace IntegrationTests;

/// <summary>
/// Helper class for creating DPoP proofs and managing DPoP-bound tokens in integration tests.
/// </summary>
public class DPoPHelper : IDisposable
{
    private readonly ECDsa _privateKey;
    private readonly JsonWebKey _publicKeyJwk;

    public DPoPHelper()
    {
        // Generate ES256 key pair
        _privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        _publicKeyJwk = CreatePublicKeyJwk(_privateKey);
    }

    /// <summary>
    /// Creates a JWK representation of the public key.
    /// </summary>
    private static JsonWebKey CreatePublicKeyJwk(ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(includePrivateParameters: false);

        return new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
            Crv = "P-256",
            X = Base64UrlEncoder.Encode(parameters.Q.X),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y)
        };
    }

    /// <summary>
    /// Computes the JWK thumbprint (jkt) of the public key.
    /// </summary>
    public string ComputeJkt()
    {
        // Create canonical JWK (lexicographically sorted)
        var canonicalJwk = $$"""{"crv":"{{_publicKeyJwk.Crv}}","kty":"{{_publicKeyJwk.Kty}}","x":"{{_publicKeyJwk.X}}","y":"{{_publicKeyJwk.Y}}"}""";

        // Compute SHA-256 hash
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJwk));

        // Return base64url-encoded hash
        return Base64UrlEncoder.Encode(hashBytes);
    }

    /// <summary>
    /// Computes the access token hash (ath) for the DPoP proof.
    /// </summary>
    private static string ComputeAth(string accessToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken));
        return Base64UrlEncoder.Encode(hashBytes);
    }

    /// <summary>
    /// Creates a DPoP proof JWT.
    /// </summary>
    /// <param name="httpMethod">HTTP method (e.g., "GET", "POST")</param>
    /// <param name="httpUri">Full HTTP URI of the request</param>
    /// <param name="accessToken">Optional access token to bind to the proof</param>
    /// <returns>DPoP proof JWT string</returns>
    public string CreateDPoPProof(string httpMethod, string httpUri, string? accessToken = null)
    {
        var uri = new Uri(httpUri);
        var htu = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}{uri.AbsolutePath}";

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var jti = Guid.NewGuid().ToString();

        // Build claims
        var claims = new Dictionary<string, object>
        {
            { "jti", jti },
            { "htm", httpMethod.ToUpperInvariant() },
            { "htu", htu },
            { "iat", now.ToUnixTimeSeconds() }
        };

        // Add ath (access token hash) if access token is provided
        if (!string.IsNullOrEmpty(accessToken))
        {
            claims["ath"] = ComputeAth(accessToken);
        }

        // Create signing credentials
        var signingCredentials = new SigningCredentials(
            new ECDsaSecurityKey(_privateKey) { KeyId = null },
            SecurityAlgorithms.EcdsaSha256
        );

        var handler = new JwtSecurityTokenHandler();

        // Manually construct the JWT with proper headers
        var header = new JwtHeader(signingCredentials)
        {
            // Set the typ header
            ["typ"] = "dpop+jwt",
            // Add the public key JWK to the header
            ["jwk"] = new Dictionary<string, string>
            {
                { "kty", _publicKeyJwk.Kty },
                { "crv", _publicKeyJwk.Crv },
                { "x", _publicKeyJwk.X },
                { "y", _publicKeyJwk.Y }
            }
        };

        // Create payload preserving original types
        var payload = new JwtPayload();
        foreach (KeyValuePair<string, object> claim in claims)
        {
            payload[claim.Key] = claim.Value;
        }

        var jwtSecurityToken = new JwtSecurityToken(header, payload);
        return handler.WriteToken(jwtSecurityToken);
    }

    /// <summary>
    /// Gets a DPoP-bound access token from KeyCloak using client credentials flow with DPoP binding.
    /// </summary>
    /// <param name="domain">KeyCloak domain</param>
    /// <param name="clientId">KeyCloak client ID</param>
    /// <param name="clientSecret">KeyCloak client secret</param>
    /// <param name="audience">API audience</param>
    /// <returns>DPoP-bound access token</returns>
    public async Task<string> GetDPoPAccessTokenAsync(string domain, string clientId, string clientSecret, string audience)
    {
        using var httpClient = new HttpClient();
        var tokenUrl = $"http://{domain}/protocol/openid-connect/token";

        // Create DPoP proof for token endpoint
        var dpopProof = CreateDPoPProof("POST", tokenUrl);

        // Build request
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Headers.Add("DPoP", dpopProof);

        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", clientId },
            { "client_secret", clientSecret }
            // audience not sent - KeyCloak enforces it server-side via audience mappers
        };

        request.Content = new FormUrlEncodedContent(formData);

        // Make request
        HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        TokenResponse? tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("Token response did not contain an access token");
        }

        return tokenResponse.AccessToken;
    }

    public void Dispose()
    {
        _privateKey.Dispose();
    }

    /// <summary>
    /// Token response from KeyCloak's OIDC token endpoint.
    /// </summary>
    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}
