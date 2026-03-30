using AspNetCore.Authentication.Api;
using AspNetCore.Authentication.Api.DPoP;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace UnitTests;

public class DPoPEventsFactoryTests
{
    [Fact]
    public void Create_WithNullKeyCloakOptions_ThrowsArgumentNullException()
    {
        Func<JwtBearerEvents> act = () => DPoPEventsFactory.Create(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullKeyCloakOptionsEvents_ReturnsJwtBearerEventsWithNullHandlers()
    {
        var dPoPOptions = new DPoPOptions();
        var KeyCloakOptions = new KeyCloakApiOptions { JwtBearerOptions = new JwtBearerOptions { Events = null } };

        JwtBearerEvents result = DPoPEventsFactory.Create(KeyCloakOptions);

        result.Should().NotBeNull();
        result.OnTokenValidated.Should().NotBeNull();
        result.OnAuthenticationFailed.Should().NotBeNull();
        result.OnMessageReceived.Should().NotBeNull();
        result.OnChallenge.Should().NotBeNull();
        result.OnForbidden.Should().NotBeNull();
    }
}
