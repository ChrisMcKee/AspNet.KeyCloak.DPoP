using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace AspNet.KeyCloak.DPoP;

/// <summary>
///     Post-configures <see cref="JwtBearerOptions" />
/// </summary>
internal class KeyCloakJwtBearerPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        options.Backchannel?.DefaultRequestHeaders.Add("KeyCloak-Client", Utils.CreateAgentString());
    }
}
