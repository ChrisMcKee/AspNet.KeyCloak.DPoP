using System.Net;
using System.Text.Json;
using Testcontainers.Keycloak;

namespace IntegrationTests;

/// <summary>
/// Starts a Keycloak container once for all tests in the <see cref="KeyCloakCollection"/>.
/// The realm "test" is imported from realm-test.json and contains three clients:
/// <list type="bullet">
///   <item><description>basic-client — plain Bearer (no DPoP)</description></item>
///   <item><description>dpop-client — DPoP required (dpop.bound.access.tokens = true)</description></item>
///   <item><description>dpop-required-client — DPoP required (dpop.bound.access.tokens = true)</description></item>
/// </list>
/// DPoP Allowed tests use basic-client for Bearer and dpop-client for DPoP-bound tokens.
/// DPoP Required tests use dpop-required-client for all token acquisition.
/// </summary>
public class KeyCloakFixture : IAsyncLifetime
{
    private KeycloakContainer? _container;

    /// <summary>Scenario with no DPoP, uses basic-client.</summary>
    public KeyCloakScenario WithoutDPoP { get; private set; } = null!;

    /// <summary>
    /// Scenario for DPoP Allowed mode.
    /// ClientId/ClientSecret are for Bearer tokens (basic-client).
    /// DPoPClientId/DPoPClientSecret are for DPoP-bound tokens (dpop-client).
    /// </summary>
    public KeyCloakScenario WithDPoPAllowed { get; private set; } = null!;

    /// <summary>
    /// Scenario for DPoP Required mode, uses dpop-required-client.
    /// All token requests must include a DPoP proof.
    /// </summary>
    public KeyCloakScenario WithDPoPRequired { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var realmFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, "realm-test.json"));
        if (!realmFile.Exists)
            throw new FileNotFoundException("Missing realm import file for integration tests.", realmFile.FullName);

        _container = new KeycloakBuilder("quay.io/keycloak/keycloak:26.5.6")
            .WithResourceMapping(realmFile, new FileInfo("/opt/keycloak/data/import/realm.json"))
            .WithCommand("--import-realm")
            .Build();

        await _container.StartAsync();

        // e.g. "http://localhost:49285/"
        var baseUri = _container.GetBaseAddress().TrimEnd('/');
        await WaitForImportedRealmAsync(baseUri);

        var authority = $"{baseUri}/realms/test";

        // Domain strips the scheme since the library prepends https:// by default.
        // Authority is passed explicitly, so the middleware uses the correct http:// URL.
        var domain = authority.Replace("http://", "").Replace("https://", "");

        WithoutDPoP = new KeyCloakScenario
        {
            Domain = domain,
            Authority = authority,
            Audience = "test-api",
            ClientId = "basic-client",
            ClientSecret = "basic-secret",
            DPoPMode = "None"
        };

        WithDPoPAllowed = new KeyCloakScenario
        {
            Domain = domain,
            Authority = authority,
            Audience = "test-api",
            ClientId = "basic-client",
            ClientSecret = "basic-secret",
            DPoPMode = "Allowed",
            DPoPClientId = "dpop-client",
            DPoPClientSecret = "dpop-secret"
        };

        WithDPoPRequired = new KeyCloakScenario
        {
            Domain = domain,
            Authority = authority,
            Audience = "test-api",
            ClientId = "dpop-required-client",
            ClientSecret = "dpop-required-secret",
            DPoPMode = "Required"
        };
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    private static async Task WaitForImportedRealmAsync(string baseUri)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var metadataUri = $"{baseUri}/realms/test/.well-known/openid-configuration";
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < TimeSpan.FromSeconds(45))
        {
            try
            {
                using var response = await httpClient.GetAsync(metadataUri);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    if (document.RootElement.TryGetProperty("token_endpoint", out var tokenEndpoint)
                        && tokenEndpoint.GetString()?.Contains("/realms/test/", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Keep polling while Keycloak boots.
            }

            await Task.Delay(500);
        }

        throw new InvalidOperationException(
            $"Keycloak started but realm import was not observed within timeout. Base URL: {baseUri}, expected import path: /opt/keycloak/data/import/realm-test.json");
    }
}
