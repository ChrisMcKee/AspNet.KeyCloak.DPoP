using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Authentication.Api;

/// <summary>
///     Builder to add functionality on top of KeyCloak API authentication.
/// </summary>
public class KeyCloakApiAuthenticationBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KeyCloakApiAuthenticationBuilder" /> class.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="IServiceCollection" /> instance used to register authentication services.
    /// </param>
    /// <param name="options">
    ///     The <see cref="KeyCloakApiOptions" /> containing configuration options for KeyCloak authentication.
    /// </param>
    public KeyCloakApiAuthenticationBuilder(IServiceCollection services, KeyCloakApiOptions options) : this(services,
        KeyCloakConstants.AuthenticationScheme.KeyCloak, options)
    {
    }

    /// <summary>
    ///     Constructs an instance of <see cref="KeyCloakApiAuthenticationBuilder" />.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="IServiceCollection" /> instance used to register authentication services.
    /// </param>
    /// <param name="authenticationScheme">
    ///     The authentication scheme to use for the KeyCloak authentication handler.
    /// </param>
    /// <param name="options">
    ///     The <see cref="KeyCloakApiOptions" /> containing configuration options for KeyCloak authentication.
    /// </param>
    public KeyCloakApiAuthenticationBuilder(IServiceCollection services, string authenticationScheme,
        KeyCloakApiOptions options)
    {
        Services = services;
        Options = options;
        AuthenticationScheme = authenticationScheme;
    }

    public string AuthenticationScheme { get; }
    public KeyCloakApiOptions Options { get; }
    public IServiceCollection Services { get; }
}
