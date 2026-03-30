using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.IdentityModel.Tokens;

namespace AspNetCore.Authentication.Api.DPoPClient;

/// <summary>
/// DPoP Client for making DPoP-bound requests to KeyCloak protected APIs.
/// Demonstrates the complete DPoP flow including key generation, proof creation, and API calls.
/// </summary>
public class DPoPClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ECDsa _privateKey;
    private readonly JsonWebKey _publicKeyJwk;
    private readonly string _baseUrl;

    public DPoPClient(string baseUrl = "https://localhost:7168")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true // For local dev only
        });

        // Generate ES256 key pair
        Console.WriteLine("🔑 Generating ES256 key pair for DPoP...");
        _privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        _publicKeyJwk = CreatePublicKeyJwk(_privateKey);

        Console.WriteLine("✅ Key pair generated successfully");
        Console.WriteLine($"   Public key (JWK): {JsonSerializer.Serialize(_publicKeyJwk, new JsonSerializerOptions { WriteIndented = false })}");
    }

    /// <summary>
    /// Creates a JWK representation of the public key.
    /// </summary>
    private static JsonWebKey CreatePublicKeyJwk(ECDsa ecdsa)
    {
        var parameters = ecdsa.ExportParameters(includePrivateParameters: false);

        return new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
            Crv = "P-256",
            X = Base64UrlEncoder.Encode(parameters.Q.X!),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y!)
        };
    }

    /// <summary>
    /// Computes the JWK thumbprint (jkt) of the public key.
    /// </summary>
    private string ComputeJkt()
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
    public string CreateDPoPProof(string httpMethod, string httpUri, string? accessToken = null)
    {
        var uri = new Uri(httpUri);
        var htu = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}{uri.AbsolutePath}";

        var now = DateTimeOffset.UtcNow;
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
        var header = new JwtHeader(signingCredentials);

        // Set the typ header (this will replace the default "JWT" value)
        header["typ"] = "dpop+jwt";

        // Add the public key JWK to the header
        header["jwk"] = new Dictionary<string, string>
        {
            { "kty", _publicKeyJwk.Kty },
            { "crv", _publicKeyJwk.Crv! },
            { "x", _publicKeyJwk.X },
            { "y", _publicKeyJwk.Y }
        };

        // Create payload preserving original types (numeric iat, string jti, etc.)
        var payload = new JwtPayload();
        foreach (var claim in claims)
        {
            payload[claim.Key] = claim.Value;
        }

        var jwtSecurityToken = new JwtSecurityToken(header, payload);
        return handler.WriteToken(jwtSecurityToken);
    }

    /// <summary>
    /// Makes a request to the API with DPoP authentication.
    /// </summary>
    public async Task<HttpResponseMessage> MakeRequestAsync(
        string endpoint,
        string? accessToken = null,
        HttpMethod? method = null)
    {
        method ??= HttpMethod.Get;
        var url = $"{_baseUrl}{endpoint}";

        Console.WriteLine($"\n📡 Making {method} request to: {url}");

        // Create DPoP proof
        var dpopProof = CreateDPoPProof(method.Method, url, accessToken);
        Console.WriteLine($"🔐 DPoP proof created: {dpopProof[..Math.Min(50, dpopProof.Length)]}...");

        // Build request
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("DPoP", dpopProof);

        // Add authorization header if access token is provided
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Add("Authorization", $"DPoP {accessToken}");
            Console.WriteLine($"🎫 Access token: {accessToken[..Math.Min(50, accessToken.Length)]}...");
        }

        // Make request
        var response = await _httpClient.SendAsync(request);

        Console.WriteLine($"\n📥 Response Status: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine("📥 Response Headers:");

        if (response.Headers.Contains("WWW-Authenticate"))
        {
            Console.WriteLine($"   WWW-Authenticate: {string.Join(", ", response.Headers.GetValues("WWW-Authenticate"))}");
        }

        Console.WriteLine($"   Content-Type: {response.Content.Headers.ContentType}");

        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(content))
        {
            Console.WriteLine($"📥 Response Body: {content}");
        }

        return response;
    }

    /// <summary>
    /// Tests the open endpoint (no authentication required).
    /// </summary>
    public async Task<bool> TestOpenEndpointAsync()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("Testing OPEN endpoint (no authentication)");
        Console.WriteLine(new string('=', 60));

        var response = await MakeRequestAsync("/open-endpoint");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✅ Open endpoint access successful!");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ Open endpoint access failed with status {response.StatusCode}");
            return false;
        }
    }

    /// <summary>
    /// Tests the restricted endpoint (requires authentication).
    /// </summary>
    public async Task<bool> TestRestrictedEndpointAsync(string accessToken)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("Testing RESTRICTED endpoint (DPoP-bound authentication)");
        Console.WriteLine(new string('=', 60));

        var response = await MakeRequestAsync("/restricted-endpoint", accessToken);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✅ Restricted endpoint access successful!");
            return true;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("❌ Unauthorized - Token may be invalid or expired");
            Console.WriteLine("   Make sure you're using a valid DPoP-bound access token from KeyCloak");
            return false;
        }
        else
        {
            Console.WriteLine($"❌ Restricted endpoint access failed with status {response.StatusCode}");
            return false;
        }
    }

    /// <summary>
    /// Gets a DPoP-bound access token from
    /// </summary>
    public async Task<string> GetDPoPTokenAsync(KeyCloakConfig config)
    {
        var tokenUrl = $"https://{config.Domain}/oauth/token";

        Console.WriteLine($"🔑 Requesting DPoP-bound token from: {tokenUrl}");

        // Create DPoP proof for token endpoint
        var dpopProof = CreateDPoPProof("POST", tokenUrl);
        Console.WriteLine("   DPoP proof created for token request");

        // Build request
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Headers.Add("DPoP", dpopProof);

        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", config.ClientId },
            { "client_secret", config.ClientSecret },
            { "audience", config.Audience }
        };

        request.Content = new FormUrlEncodedContent(formData);

        // Make request
        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content);

            if (tokenResponse?.AccessToken == null)
            {
                throw new Exception("Token response did not contain an access token");
            }

            Console.WriteLine("✅ Token obtained successfully");
            Console.WriteLine($"   Token type: {tokenResponse.TokenType ?? "Bearer"}");
            Console.WriteLine($"   Expires in: {tokenResponse.ExpiresIn ?? 0} seconds");

            return tokenResponse.AccessToken;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Token request failed: {response.StatusCode}");
            Console.WriteLine($"   Response: {errorContent}");
            throw new Exception($"Failed to obtain token: {response.StatusCode}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _privateKey?.Dispose();
    }
}

/// <summary>
/// KeyCloak configuration.
/// </summary>
public record KeyCloakConfig(
    string Domain,
    string Audience,
    string ClientId,
    string ClientSecret,
    string ApiBaseUrl = "https://localhost:7168");

/// <summary>
/// Token response from
/// </summary>
internal class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }
}
