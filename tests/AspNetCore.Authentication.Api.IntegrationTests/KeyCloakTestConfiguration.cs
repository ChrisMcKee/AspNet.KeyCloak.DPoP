using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

/// <summary>
/// Holds a complete set of configuration values for one integration test scenario.
/// Created either from environment variables (via <see cref="KeyCloakTestConfiguration"/>)
/// or directly by <see cref="KeyCloakFixture"/> when a Keycloak container is running.
/// </summary>
public class KeyCloakScenario
{
    /// <summary>KeyCloak domain, e.g. localhost:8080/realms/test (no scheme).</summary>
    public required string Domain { get; init; }

    /// <summary>
    /// Full authority URL including scheme, e.g. http://localhost:8080/realms/test.
    /// When null the library defaults to https://{Domain}.
    /// </summary>
    public string? Authority { get; init; }

    /// <summary>Expected audience claim value.</summary>
    public required string Audience { get; init; }

    /// <summary>Client ID used for Bearer token acquisition.</summary>
    public required string ClientId { get; init; }

    /// <summary>Client secret used for Bearer token acquisition.</summary>
    public required string ClientSecret { get; init; }

    /// <summary>DPoP mode: None, Allowed, or Required.</summary>
    public string DPoPMode { get; init; } = "None";

    /// <summary>
    /// Optional separate client ID for acquiring DPoP-bound tokens.
    /// When null, <see cref="ClientId"/> is used for DPoP token requests.
    /// </summary>
    public string? DPoPClientId { get; init; }

    /// <summary>Client secret corresponding to <see cref="DPoPClientId"/>.</summary>
    public string? DPoPClientSecret { get; init; }

    public bool IsDPoPEnabled => DPoPMode != "None";
    public bool IsDPoPRequired => DPoPMode == "Required";
}

/// <summary>
/// Provides centralized configuration for integration tests.
/// Reads from client-secrets.json (local development) and environment variables (CI/CD).
/// </summary>
public static class KeyCloakTestConfiguration
{
    /// <summary>
    /// Shared configuration root. Environment variables take precedence over JSON file settings.
    /// </summary>
    public static readonly IConfigurationRoot Config = new ConfigurationBuilder()
        .AddJsonFile("client-secrets.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    /// <summary>Gets the scenario for tests without DPoP.</summary>
    public static KeyCloakScenario WithoutDPoP => CreateFromConfig("BASIC");

    /// <summary>Gets the scenario for tests with DPoP in Allowed mode.</summary>
    public static KeyCloakScenario WithDPoPAllowed => CreateFromConfig("DPOP_ALLOWED");

    /// <summary>Gets the scenario for tests with DPoP in Required mode.</summary>
    public static KeyCloakScenario WithDPoPRequired => CreateFromConfig("DPOP_REQUIRED");

    private static KeyCloakScenario CreateFromConfig(string prefix)
    {
        string Required(string suffix)
        {
            var key = $"{prefix}_{suffix}";
            return Config[key] ?? throw new InvalidOperationException(
                $"{key} configuration value is required for integration tests.");
        }

        string? Optional(string suffix) => Config[$"{prefix}_{suffix}"];

        var domain = Required("DOMAIN");
        return new KeyCloakScenario
        {
            Domain = domain,
            Authority = Optional("AUTHORITY"),
            Audience = Required("AUDIENCE"),
            ClientId = Required("CLIENT_ID"),
            ClientSecret = Required("CLIENT_SECRET"),
            DPoPMode = Optional("DPOP_MODE") ?? "None",
            DPoPClientId = Optional("DPOP_CLIENT_ID"),
            DPoPClientSecret = Optional("DPOP_CLIENT_SECRET")
        };
    }
}
