using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AspNetCore.Authentication.Api.DPoP;

/// <summary>
///     Provides a factory for creating configured JwtBearerEvents instances
/// </summary>
internal abstract class DPoPEventsFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="JwtBearerEvents" /> and assigns event handlers
    ///     based on the provided <paramref name="KeyCloakOptions" />.
    /// </summary>
    /// <param name="KeyCloakOptions">The KeyCloak API options containing custom event handlers.</param>
    /// <returns>A configured <see cref="JwtBearerEvents" /> instance with integrated event handlers.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if either <paramref name="KeyCloakOptions" /> is null.
    /// </exception>
    internal static JwtBearerEvents Create(KeyCloakApiOptions? KeyCloakOptions)
    {
        ArgumentNullException.ThrowIfNull(KeyCloakOptions);

        var dPoPEventHandlers = new DPoPEventHandlers();
        return new JwtBearerEvents
        {
            OnMessageReceived =
                ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnMessageReceived,
                    dPoPEventHandlers.HandleOnMessageReceived()),
            OnTokenValidated = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnTokenValidated,
                dPoPEventHandlers.HandleOnTokenValidated()),
            OnAuthenticationFailed = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnAuthenticationFailed),
            OnChallenge = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnChallenge,
                dPoPEventHandlers.HandleOnChallenge()),
            OnForbidden = ProxyEvent(KeyCloakOptions.JwtBearerOptions?.Events?.OnForbidden)
        };
    }

    /// <summary>
    ///     Creates a composite event handler that executes an additional handler first,
    ///     followed by the original handler, if they are provided.
    /// </summary>
    /// <typeparam name="T">The type of the event context.</typeparam>
    /// <param name="originalHandler">The original event handler provided by the user.</param>
    /// <param name="additionalHandler">An additional event handler to execute before the original handler.</param>
    /// <returns>A composite event handler that executes both handlers in sequence.</returns>
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
