using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AspNetCore.Authentication.Api;

/// <summary>
///     Provides a factory for creating configured JwtBearerEvents instances
/// </summary>
internal abstract class JwtBearerEventsFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="JwtBearerEvents" /> and assigns event handlers
    ///     based on the provided <paramref name="keyCloakOptions" />
    /// </summary>
    /// <returns>A configured <see cref="JwtBearerEvents" /> instance.</returns>
    /// <param name="keyCloakOptions">The KeyCloak API options containing custom event handlers.</param>
    internal static JwtBearerEvents Create(KeyCloakApiOptions? keyCloakOptions)
    {
        ArgumentNullException.ThrowIfNull(keyCloakOptions);

        return new JwtBearerEvents
        {
            OnTokenValidated = ProxyEvent(keyCloakOptions.JwtBearerOptions?.Events?.OnTokenValidated),
            OnAuthenticationFailed = ProxyEvent(keyCloakOptions.JwtBearerOptions?.Events?.OnAuthenticationFailed),
            OnMessageReceived = ProxyEvent(keyCloakOptions.JwtBearerOptions?.Events?.OnMessageReceived),
            OnChallenge = ProxyEvent(keyCloakOptions.JwtBearerOptions?.Events?.OnChallenge),
            OnForbidden = ProxyEvent(keyCloakOptions.JwtBearerOptions?.Events?.OnForbidden)
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
