using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Authentication.Api;

/// <summary>
///     Contains
///     <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection">IServiceCollection</see>
///     extension(s) for registering
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds KeyCloak API authentication to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection to add authentication to.</param>
    /// <param name="configureOptions">An action to configure the <see cref="KeyCloakApiOptions" />.</param>
    /// <returns>An <see cref="KeyCloakApiAuthenticationBuilder" /> for further configuration.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="services" /> or
    ///     <paramref name="configureOptions" /> is null.
    /// </exception>
    public static KeyCloakApiAuthenticationBuilder AddKeyCloakApiAuthentication(
        this IServiceCollection services,
        Action<KeyCloakApiOptions>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions, nameof(configureOptions));

        return services.AddKeyCloakApiAuthentication(KeyCloakConstants.AuthenticationScheme.KeyCloak, configureOptions);
    }

    /// <summary>
    ///     Adds KeyCloak API authentication to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection to add authentication to.</param>
    /// <param name="authenticationScheme">The authentication scheme to use.</param>
    /// <param name="configureOptions">An action to configure the <see cref="KeyCloakApiOptions" />.</param>
    /// <returns>An <see cref="KeyCloakApiAuthenticationBuilder" /> for further configuration.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="services" /> or
    ///     <paramref name="configureOptions" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="authenticationScheme" /> is null or empty.</exception>
    public static KeyCloakApiAuthenticationBuilder AddKeyCloakApiAuthentication(this IServiceCollection services,
        string? authenticationScheme, Action<KeyCloakApiOptions>? configureOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme, nameof(authenticationScheme));
        ArgumentNullException.ThrowIfNull(configureOptions, nameof(configureOptions));

        return services
            .AddAuthentication(options => { options.DefaultScheme = authenticationScheme; })
            .AddKeyCloakApiAuthentication(authenticationScheme, configureOptions);
    }
}
