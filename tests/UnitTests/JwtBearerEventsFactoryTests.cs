using AspNet.KeyCloak.DPoP;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace UnitTests;

public class JwtBearerEventsFactoryTests
{
    [Fact]
    public void Create_WithNullKeyCloakOptions_ThrowsArgumentNullException()
    {
        var jwtOptions = new JwtBearerOptions();

        Func<JwtBearerEvents> act = () => JwtBearerEventsFactory.Create(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullKeyCloakOptionsEvents_ReturnsJwtBearerEventsWithNullHandlers()
    {
        var KeyCloakOptions = new KeyCloakApiOptions { JwtBearerOptions = new JwtBearerOptions { Events = null } };

        JwtBearerEvents result = JwtBearerEventsFactory.Create(KeyCloakOptions);

        result.Should().NotBeNull();
        result.OnTokenValidated.Should().NotBeNull();
        result.OnAuthenticationFailed.Should().NotBeNull();
        result.OnMessageReceived.Should().NotBeNull();
        result.OnChallenge.Should().NotBeNull();
        result.OnForbidden.Should().NotBeNull();
    }
}
