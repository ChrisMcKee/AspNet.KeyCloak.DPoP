using System.Runtime.CompilerServices;

using AspNetCore.Authentication.Api.DPoP;
using AspNetCore.Authentication.Api.DPoP.EventHandlers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

[assembly: InternalsVisibleTo("KeyCloak.AspNet.Authentication.Api.UnitTests")]

namespace AspNetCore.Authentication.Api;

/// <summary>
///     Provides extension methods for
///     <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">
///         AuthenticationBuilder
///     </see>
///     to simplify the registration and configuration of KeyCloak authentication.
/// </summary>
public static class AuthenticationBuilderExtensions
{
    /// <param name="builder">
    ///     The
    ///     <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">
    ///         AuthenticationBuilder
    ///     </see>
    ///     instance to configure.
    /// </param>
    extension(AuthenticationBuilder builder)
    {
        /// <summary>
        ///     Adds KeyCloak authentication for API
        /// </summary>
        /// <param name="configureOptions">
        ///     A delegate to configure the <see cref="KeyCloakApiOptions" /> for KeyCloak integration.
        /// </param>
        /// <returns>
        ///     The configured
        ///     <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationbuilder">
        ///         AuthenticationBuilder
        ///     </see>
        ///     instance.
        /// </returns>
        public KeyCloakApiAuthenticationBuilder AddKeyCloakApiAuthentication(Action<KeyCloakApiOptions>? configureOptions)
        {
            return AddKeyCloakApiAuthentication(builder, KeyCloakConstants.AuthenticationScheme.KeyCloak, configureOptions);
        }

        /// <summary>
        ///     Adds KeyCloak authentication for API
        ///     specified <see cref="AuthenticationBuilder" />.
        /// </summary>
        /// <param name="authenticationScheme">
        ///     The authentication scheme to use for KeyCloak authentication.
        /// </param>
        /// <param name="configureOptions">
        ///     A delegate used to configure the <see cref="KeyCloakApiOptions" /> for KeyCloak integration.
        /// </param>
        /// <returns>
        ///     The configured <see cref="AuthenticationBuilder" /> instance.
        /// </returns>
        public KeyCloakApiAuthenticationBuilder AddKeyCloakApiAuthentication(string authenticationScheme, Action<KeyCloakApiOptions>? configureOptions)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);
            ArgumentNullException.ThrowIfNull(configureOptions);

            var KeyCloakOptions = new KeyCloakApiOptions();

            configureOptions(KeyCloakOptions);

            ValidateKeyCloakApiOptions(KeyCloakOptions);

            builder.AddJwtBearer(
                authenticationScheme, options => ConfigureJwtBearerOptions(options, KeyCloakOptions));

            builder.Services.Configure(authenticationScheme, configureOptions);
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, KeyCloakJwtBearerPostConfigureOptions>());

            return new KeyCloakApiAuthenticationBuilder(builder.Services, authenticationScheme, KeyCloakOptions);
        }
    }


    /// <param name="builder">
    ///     The <see cref="KeyCloakApiAuthenticationBuilder" /> instance to configure.
    /// </param>
    extension(KeyCloakApiAuthenticationBuilder builder)
    {
        /// <summary>
        ///     Enables DPoP (Demonstration of Proof-of-Possession) support
        ///     with default configuration using the default KeyCloak authentication scheme.
        /// </summary>
        /// <returns>
        ///     The configured <see cref="KeyCloakApiAuthenticationBuilder" /> instance.
        /// </returns>
        public KeyCloakApiAuthenticationBuilder WithDPoP()
        {
            return WithDPoP(builder, KeyCloakConstants.AuthenticationScheme.KeyCloak);
        }

        /// <summary>
        ///     Enables DPoP (Demonstration of Proof-of-Possession) support
        ///     with default configuration using a specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">
        ///     The authentication scheme to use for DPoP integration.
        /// </param>
        /// <returns>
        ///     The configured <see cref="KeyCloakApiAuthenticationBuilder" /> instance.
        /// </returns>
        public KeyCloakApiAuthenticationBuilder WithDPoP(string authenticationScheme)
        {
            return WithDPoP(builder, authenticationScheme, _ => { });
        }

        /// <summary>
        ///     Enables DPoP (Demonstration of Proof-of-Possession) support
        ///     using the default KeyCloak authentication scheme.
        /// </summary>
        /// <param name="configureDPoPOptions">
        ///     A delegate to configure the <see cref="DPoPOptions" /> for DPoP integration.
        /// </param>
        /// <returns>
        ///     The configured <see cref="KeyCloakApiAuthenticationBuilder" /> instance.
        /// </returns>
        public KeyCloakApiAuthenticationBuilder WithDPoP(Action<DPoPOptions> configureDPoPOptions)
        {
            return WithDPoP(builder, KeyCloakConstants.AuthenticationScheme.KeyCloak, configureDPoPOptions);
        }

        /// <summary>
        ///     Enables DPoP (Demonstration of Proof-of-Possession) support for the KeyCloak API authentication builder
        ///     using a specified authentication scheme.
        /// </summary>
        /// <param name="authenticationScheme">
        ///     The authentication scheme to use for DPoP integration.
        /// </param>
        /// <param name="configureDPoPOptions">
        ///     A delegate to configure the <see cref="DPoPOptions" /> for DPoP integration.
        /// </param>
        /// <returns>
        ///     The configured <see cref="KeyCloakApiAuthenticationBuilder" /> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder" /> or
        ///     <paramref name="configureDPoPOptions" /> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="authenticationScheme" /> is empty or null.
        /// </exception>
        public KeyCloakApiAuthenticationBuilder WithDPoP(string authenticationScheme,
            Action<DPoPOptions> configureDPoPOptions)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);
            ArgumentNullException.ThrowIfNull(configureDPoPOptions);

            var dPoPOptions = new DPoPOptions();
            configureDPoPOptions(dPoPOptions);

            builder.Services.Configure<JwtBearerOptions>(builder.AuthenticationScheme,
                jwtBearerOptions => { jwtBearerOptions.Events = DPoPEventsFactory.Create(builder.Options); });

            builder.Services.TryAddSingleton(dPoPOptions);
            builder.Services.TryAddScoped<IDPoPProofValidationService, DPoPProofValidationService>();
            builder.Services.TryAddScoped<MessageReceivedHandler>();
            builder.Services.TryAddScoped<TokenValidationHandler>();
            builder.Services.TryAddScoped<ChallengeHandler>();
            return builder;
        }
    }

    /// <summary>
    ///     Configures the <see cref="JwtBearerOptions" /> instance using the provided <see cref="KeyCloakApiOptions" />.
    /// </summary>
    internal static void ConfigureJwtBearerOptions(JwtBearerOptions? jwtBearerOptions, KeyCloakApiOptions? KeyCloakApiOptions)
    {
        ArgumentNullException.ThrowIfNull(jwtBearerOptions);
        ArgumentNullException.ThrowIfNull(KeyCloakApiOptions);
        ArgumentNullException.ThrowIfNull(KeyCloakApiOptions.JwtBearerOptions);

        jwtBearerOptions.ClaimsIssuer = KeyCloakApiOptions.JwtBearerOptions.ClaimsIssuer;
        jwtBearerOptions.TimeProvider = KeyCloakApiOptions.JwtBearerOptions.TimeProvider;

        jwtBearerOptions.Authority = KeyCloakApiOptions.Authority ?? $"https://{KeyCloakApiOptions.Domain}";
        jwtBearerOptions.Audience = KeyCloakApiOptions.JwtBearerOptions.Audience;
        jwtBearerOptions.Challenge = KeyCloakApiOptions.JwtBearerOptions.Challenge;
        jwtBearerOptions.SaveToken = KeyCloakApiOptions.JwtBearerOptions.SaveToken;
        jwtBearerOptions.IncludeErrorDetails = KeyCloakApiOptions.JwtBearerOptions.IncludeErrorDetails;
        jwtBearerOptions.RequireHttpsMetadata = KeyCloakApiOptions.JwtBearerOptions.RequireHttpsMetadata;
        jwtBearerOptions.MetadataAddress = KeyCloakApiOptions.JwtBearerOptions.MetadataAddress;
        jwtBearerOptions.Configuration = KeyCloakApiOptions.JwtBearerOptions.Configuration;
        jwtBearerOptions.ConfigurationManager = KeyCloakApiOptions.JwtBearerOptions.ConfigurationManager;
        jwtBearerOptions.RefreshOnIssuerKeyNotFound = KeyCloakApiOptions.JwtBearerOptions.RefreshOnIssuerKeyNotFound;
        jwtBearerOptions.MapInboundClaims = KeyCloakApiOptions.JwtBearerOptions.MapInboundClaims;
        jwtBearerOptions.BackchannelTimeout = KeyCloakApiOptions.JwtBearerOptions.BackchannelTimeout;
        jwtBearerOptions.BackchannelHttpHandler = KeyCloakApiOptions.JwtBearerOptions.BackchannelHttpHandler;
        jwtBearerOptions.Backchannel = KeyCloakApiOptions.JwtBearerOptions.Backchannel;
        jwtBearerOptions.AutomaticRefreshInterval = KeyCloakApiOptions.JwtBearerOptions.AutomaticRefreshInterval;
        jwtBearerOptions.RefreshInterval = KeyCloakApiOptions.JwtBearerOptions.RefreshInterval;
        jwtBearerOptions.UseSecurityTokenValidators = KeyCloakApiOptions.JwtBearerOptions.UseSecurityTokenValidators;

        jwtBearerOptions.ForwardDefault = KeyCloakApiOptions.JwtBearerOptions.ForwardDefault;
        jwtBearerOptions.ForwardAuthenticate = KeyCloakApiOptions.JwtBearerOptions.ForwardAuthenticate;
        jwtBearerOptions.ForwardChallenge = KeyCloakApiOptions.JwtBearerOptions.ForwardChallenge;
        jwtBearerOptions.ForwardForbid = KeyCloakApiOptions.JwtBearerOptions.ForwardForbid;
        jwtBearerOptions.ForwardSignIn = KeyCloakApiOptions.JwtBearerOptions.ForwardSignIn;
        jwtBearerOptions.ForwardSignOut = KeyCloakApiOptions.JwtBearerOptions.ForwardSignOut;
        jwtBearerOptions.ForwardDefaultSelector = KeyCloakApiOptions.JwtBearerOptions.ForwardDefaultSelector;

        jwtBearerOptions.TokenValidationParameters = KeyCloakApiOptions.JwtBearerOptions.TokenValidationParameters;
        jwtBearerOptions.Events = JwtBearerEventsFactory.Create(KeyCloakApiOptions);
    }

    /// <summary>
    ///     Validates the KeyCloak configuration options.
    /// </summary>
    /// <param name="options">The <see cref="KeyCloakApiOptions" /> to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when required KeyCloak configuration is missing or invalid.</exception>
    internal static void ValidateKeyCloakApiOptions(KeyCloakApiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Domain))
        {
            throw new InvalidOperationException(
                "KeyCloak Domain is required. Please set the Domain property in KeyCloakApiOptions.");
        }

        if (string.IsNullOrWhiteSpace(options.JwtBearerOptions?.Audience))
        {
            throw new InvalidOperationException(
                "KeyCloak Audience is required. Please set the Audience property in KeyCloakApiOptions.");
        }
    }
}
