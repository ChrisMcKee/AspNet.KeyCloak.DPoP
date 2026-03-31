using AspNetCore.Authentication.Api;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace UnitTests;

public class AuthenticationBuilderExtensionsTest
{
    private readonly AuthenticationBuilder _authenticationBuilder;
    private readonly ServiceCollection _services;

    public AuthenticationBuilderExtensionsTest()
    {
        _services = new ServiceCollection();
        _authenticationBuilder = new AuthenticationBuilder(_services);
    }

    #region ValidateKeyCloakApiOptionsTests

    [Fact]
    public void ValidateKeyCloakApiOptions_ShouldNotThrow_When_Domain_And_Audience_Are_Set()
    {
        // Arrange
        var options = new KeyCloakApiOptions
        {
            Domain = "example.example.com",
            JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "https://api.example.com"
            }
        };

        // Act
        Action act = () => AuthenticationBuilderExtensions.ValidateKeyCloakApiOptions(options);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateKeyCloakApiOptions_ShouldThrow_When_Domain_Is_Null_Or_WhiteSpace(string? domain)
    {
        // Arrange
        var options = new KeyCloakApiOptions
        {
            Domain = domain,
            JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "https://api.example.com"
            }
        };

        // Act
        Action act = () => AuthenticationBuilderExtensions.ValidateKeyCloakApiOptions(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("KeyCloak Domain is required. Please set the Domain property in KeyCloakApiOptions.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateKeyCloakApiOptions_ShouldThrow_When_Audience_Is_Null_Or_WhiteSpace(string? audience)
    {
        // Arrange
        var options = new KeyCloakApiOptions
        {
            Domain = "example.example.com",
            JwtBearerOptions = new JwtBearerOptions
            {
                Audience = audience
            }
        };

        // Act
        Action act = () => AuthenticationBuilderExtensions.ValidateKeyCloakApiOptions(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("KeyCloak Audience is required. Please set the Audience property in KeyCloakApiOptions.");
    }

    #endregion

    #region ConfigureJwtBearerOptionsTests

    [Fact]
    public void ConfigureJwtBearerOptions_With_Null_JwtBearerOptions_ThrowsArgumentNullException()
    {
        // Arrange
        JwtBearerOptions? jwtBearerOptions = null;
        var KeyCloakOptions = new KeyCloakApiOptions();

        // Act & Assert
        Action action = () => AuthenticationBuilderExtensions.ConfigureJwtBearerOptions(jwtBearerOptions, KeyCloakOptions);
        action.Should().Throw<ArgumentNullException>().WithParameterName("jwtBearerOptions");
    }

    [Fact]
    public void ConfigureJwtBearerOptions_With_Null_KeyCloakOptions_Throws_ArgumentNullException()
    {
        // Arrange
        var jwtBearerOptions = new JwtBearerOptions();
        KeyCloakApiOptions? KeyCloakOptions = null;

        // Act & Assert
        Action action = () => AuthenticationBuilderExtensions.ConfigureJwtBearerOptions(jwtBearerOptions, KeyCloakOptions);
        action.Should().Throw<ArgumentNullException>().WithParameterName("KeyCloakApiOptions");
    }

    [Fact]
    public void ConfigureJwtBearerOptions_With_Valid_Options_Configures_All()
    {
        // Arrange
        var jwtBearerOptions = new JwtBearerOptions();
        var customConfiguration = new OpenIdConnectConfiguration();
        var customConfigurationManager =
            new ConfigurationManager<OpenIdConnectConfiguration>("https://test.com",
                new OpenIdConnectConfigurationRetriever());
        var customHandler = new HttpClientHandler();
        var customBackchannel = new HttpClient();
        Func<HttpContext, string> customSelector = _ => "custom";
        var customTokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false
        };
        var KeyCloakOptions = new KeyCloakApiOptions
        {
            Domain = "test.example.com",
            JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "test-audience",
                ClaimsIssuer = "test-issuer",
                Challenge = "test-challenge",
                SaveToken = true,
                IncludeErrorDetails = true,
                RequireHttpsMetadata = false,
                MetadataAddress = "https://test.com/.well-known/openid_configuration",
                RefreshOnIssuerKeyNotFound = false,
                MapInboundClaims = false,
                BackchannelTimeout = TimeSpan.FromSeconds(30),
                AutomaticRefreshInterval = TimeSpan.FromHours(1),
                RefreshInterval = TimeSpan.FromMinutes(30),
                UseSecurityTokenValidators = true,
                ForwardDefault = "Default",
                ForwardAuthenticate = "Authenticate",
                ForwardChallenge = "Challenge",
                ForwardForbid = "Forbid",
                ForwardSignIn = "SignIn",
                ForwardSignOut = "SignOut",
                Events = new JwtBearerEvents(),
                Configuration = customConfiguration,
                ConfigurationManager = customConfigurationManager,
                BackchannelHttpHandler = customHandler,
                Backchannel = customBackchannel,
                ForwardDefaultSelector = customSelector,
                TokenValidationParameters = customTokenValidationParameters
            }
        };

        // Act
        AuthenticationBuilderExtensions.ConfigureJwtBearerOptions(jwtBearerOptions, KeyCloakOptions);

        // Assert
        jwtBearerOptions.Authority.Should().Be("https://test.example.com");
        jwtBearerOptions.Audience.Should().Be("test-audience");
        jwtBearerOptions.ClaimsIssuer.Should().Be("test-issuer");
        jwtBearerOptions.Challenge.Should().Be("test-challenge");
        jwtBearerOptions.SaveToken.Should().BeTrue();
        jwtBearerOptions.IncludeErrorDetails.Should().BeTrue();
        jwtBearerOptions.RequireHttpsMetadata.Should().BeFalse();
        jwtBearerOptions.MetadataAddress.Should().Be("https://test.com/.well-known/openid_configuration");
        jwtBearerOptions.RefreshOnIssuerKeyNotFound.Should().BeFalse();
        jwtBearerOptions.MapInboundClaims.Should().BeFalse();
        jwtBearerOptions.BackchannelTimeout.Should().Be(TimeSpan.FromSeconds(30));
        jwtBearerOptions.AutomaticRefreshInterval.Should().Be(TimeSpan.FromHours(1));
        jwtBearerOptions.RefreshInterval.Should().Be(TimeSpan.FromMinutes(30));
        jwtBearerOptions.UseSecurityTokenValidators.Should().BeTrue();
        jwtBearerOptions.ForwardDefault.Should().Be("Default");
        jwtBearerOptions.ForwardAuthenticate.Should().Be("Authenticate");
        jwtBearerOptions.ForwardChallenge.Should().Be("Challenge");
        jwtBearerOptions.ForwardForbid.Should().Be("Forbid");
        jwtBearerOptions.ForwardSignIn.Should().Be("SignIn");
        jwtBearerOptions.ForwardSignOut.Should().Be("SignOut");
        jwtBearerOptions.Events.Should().NotBeNull();
        jwtBearerOptions.ConfigurationManager.Should().Be(customConfigurationManager);
        jwtBearerOptions.Configuration.Should().Be(customConfiguration);
        jwtBearerOptions.BackchannelHttpHandler.Should().Be(customHandler);
        jwtBearerOptions.Backchannel.Should().Be(customBackchannel);
        jwtBearerOptions.ForwardDefaultSelector.Should().Be(customSelector);
        jwtBearerOptions.TokenValidationParameters.Should().Be(customTokenValidationParameters);
    }

    [Fact]
    public void ConfigureJwtBearerOptions_With_Default_Values_Configures_Correctly()
    {
        // Arrange
        var jwtBearerOptions = new JwtBearerOptions();
        var KeyCloakOptions = new KeyCloakApiOptions
        {
            Domain = "test.example.com",
            JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "test-audience"
            }
        };

        // Act
        AuthenticationBuilderExtensions.ConfigureJwtBearerOptions(jwtBearerOptions, KeyCloakOptions);

        // Assert
        jwtBearerOptions.Authority.Should().Be("https://test.example.com");
        jwtBearerOptions.Audience.Should().Be("test-audience");
        jwtBearerOptions.ClaimsIssuer.Should().BeNull();
        jwtBearerOptions.Challenge.Should().Be("Bearer");
        jwtBearerOptions.SaveToken.Should().BeTrue();
        jwtBearerOptions.IncludeErrorDetails.Should().BeTrue();
        jwtBearerOptions.RequireHttpsMetadata.Should().BeTrue();
        jwtBearerOptions.RefreshOnIssuerKeyNotFound.Should().BeTrue();
        jwtBearerOptions.MapInboundClaims.Should().BeTrue();
    }

    #endregion

    #region AddKeyCloakApiAuthentication

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void AddKeyCloakApiAuthentication_With_Invalid_AuthenticationScheme_Should_Throw_ArgumentException(
        string scheme)
    {
        // Arrange
        Action<KeyCloakApiOptions> configureOptions = opts =>
        {
            opts.Domain = "test.example.com";
            opts.JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "test-audience"
            };
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            _authenticationBuilder.AddKeyCloakApiAuthentication(scheme, configureOptions));

        exception.ParamName.Should().Be("authenticationScheme");
    }

    [Fact]
    public void AddKeyCloakApiAuthentication_With_Null_ConfigureOptions_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            _authenticationBuilder.AddKeyCloakApiAuthentication(null));

        exception.ParamName.Should().Be("configureOptions");
    }

    [Fact]
    public void AddKeyCloakApiAuthentication_Should_Register_Configuration_Successfully()
    {
        // Arrange & Act
        _authenticationBuilder.AddKeyCloakApiAuthentication(opts =>
        {
            opts.Domain = "test.example.com";
            opts.JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "test-audience"
            };
        });

        // Assert
        ServiceProvider serviceProvider = _services.BuildServiceProvider();
        IOptionsMonitor<KeyCloakApiOptions> optionsMonitor =
            serviceProvider.GetRequiredService<IOptionsMonitor<KeyCloakApiOptions>>();
        KeyCloakApiOptions options = optionsMonitor.Get(KeyCloakConstants.AuthenticationScheme.KeyCloak);

        options.Domain.Should().Be("test.example.com");
        options.JwtBearerOptions?.Audience.Should().Be("test-audience");

        // Assert for IPostConfigureOptions<JwtBearerOptions> registration
        ServiceDescriptor? serviceDescriptor = _services.FirstOrDefault(s =>
            s.ServiceType == typeof(IPostConfigureOptions<JwtBearerOptions>) &&
            s.ImplementationType == typeof(KeyCloakJwtBearerPostConfigureOptions));

        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    #endregion
}
