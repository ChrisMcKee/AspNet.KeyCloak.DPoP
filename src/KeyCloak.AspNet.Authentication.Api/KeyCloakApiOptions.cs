using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AspNetCore.Authentication.Api;

/// <summary>
///     Configuration options for KeyCloak API authentication.
/// </summary>
public class KeyCloakApiOptions
{
    /// <summary>
    ///     KeyCloak domain name, e.g. tenant.example.com. Used to construct the Authority as https://{Domain}
    ///     unless <see cref="Authority"/> is set explicitly.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    ///     Optional explicit Authority URL (e.g. http://localhost:8080/realms/test).
    ///     When set, overrides the default https://{Domain} authority construction.
    ///     Useful for test environments where the IdP runs on HTTP.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    ///     The configuration options for JWT Bearer authentication.
    /// </summary>
    public JwtBearerOptions? JwtBearerOptions { get; set; }
}
