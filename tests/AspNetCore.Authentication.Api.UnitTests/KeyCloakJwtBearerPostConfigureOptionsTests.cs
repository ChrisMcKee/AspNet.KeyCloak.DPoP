using AspNetCore.Authentication.Api;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace UnitTests;

public class KeyCloakJwtBearerPostConfigureOptionsTests
{
    [Fact]
    public void PostConfigure_Should_Add_KeyCloakClient_Header()
    {
        // Arrange
        var postConfigureOptions = new KeyCloakJwtBearerPostConfigureOptions();
        var jwtBearerOptions = new KeyCloakApiOptions
        {
            JwtBearerOptions = new JwtBearerOptions
            {
                Backchannel = new HttpClient()
            }
        };
        var expectedHeaderValue = Utils.CreateAgentString();

        // Act
        postConfigureOptions.PostConfigure(null, jwtBearerOptions.JwtBearerOptions);

        // Assert
        jwtBearerOptions.JwtBearerOptions.Backchannel.DefaultRequestHeaders.GetValues("KeyCloak-Client").First().Should()
            .Be(expectedHeaderValue);
    }
}
