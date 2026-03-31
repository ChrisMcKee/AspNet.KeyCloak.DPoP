using AspNet.KeyCloak.DPoP;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddKeyCloakApiAuthentication_With_Null_ConfigureOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddKeyCloakApiAuthentication(null));
        exception.ParamName.Should().Be("configureOptions");
    }

    [Fact]
    public void AddKeyCloakApiAuthentication_With_NullOrEmpty_AuthenticationScheme_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<KeyCloakApiOptions> configureOptions = _ => { };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            services.AddKeyCloakApiAuthentication("", configureOptions));
        exception.ParamName.Should().Be("authenticationScheme");

        exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddKeyCloakApiAuthentication(null, configureOptions));
        exception.ParamName.Should().Be("authenticationScheme");
    }

    [Fact]
    public void AddKeyCloakApiAuthentication_With_Valid_Parameters_Returns_KeyCloakApiAuthenticationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<KeyCloakApiOptions> configureOptions = options =>
        {
            options.Domain = "example.example.com";
            options.JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "https://api.example.com"
            };
        };

        // Act
        KeyCloakApiAuthenticationBuilder result = services.AddKeyCloakApiAuthentication(configureOptions);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<KeyCloakApiAuthenticationBuilder>();
    }

    [Fact]
    public void AddKeyCloakApiAuthentication_WithScheme_With_Valid_Parameters_Returns_KeyCloakApiAuthenticationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var authenticationScheme = "TestScheme";
        Action<KeyCloakApiOptions> configureOptions = options =>
        {
            options.Domain = "example.example.com";
            options.JwtBearerOptions = new JwtBearerOptions
            {
                Audience = "https://api.example.com"
            };
        };
        // Act
        KeyCloakApiAuthenticationBuilder result =
            services.AddKeyCloakApiAuthentication(authenticationScheme, configureOptions);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<KeyCloakApiAuthenticationBuilder>();
    }
}
