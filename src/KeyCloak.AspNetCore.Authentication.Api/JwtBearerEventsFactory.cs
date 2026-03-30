using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AspNetCore.Authentication.Api;

/// <summary>
///     Provides a factory for creating configured JwtBearerEvents instances
/// </summary>
internal abstract class JwtBearerEventsFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="JwtBearerEvents" /> and assigns event handlers
    ///     based on the provided <paramref name="KeyCloakOptions" />
    /// </summary>
    /// <returns>A configured <see cref="JwtBearerEvents" /> instance.</returns>
    /// <param name="KeyCloakOptions">The KeyCloak API options containing custom event handlers.</param>
    internal static JwtBearerEvents Create(KeyCloakApiOptions? KeyCloakOptions)
    {
        ArgumentNullException.ThrowIfNull(KeyCloakOptions);

        return new JwtBearerEvents
        {
            OnTokenValidated = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnTokenValidated),
            OnAuthenticationFailed = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnAuthenticationFailed),
            OnMessageReceived = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnMessageReceived),
            OnChallenge = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnChallenge),
            OnForbidden = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnForbidden)
        };
    }

    private static Func<T, Task> ProxyEvent<T>(Func<T, Task>? originalHandler, Func<T, Task>? additionalHandler = null)
    {
        return async context =>
        {
            if (additionalHandler != null)
            {
                await additionalHandler(context);
            }

            if (originalHandler != null)
            {
                await originalHandler(context);
            }
        };
    }
}
