using AspNet.KeyCloak.DPoP;
using AspNet.KeyCloak.DPoP.DPoP;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IntegrationTests;

/// <summary>
/// Test server factory for integration tests using TestServer.
/// </summary>
public class TestWebApplicationFactory : IAsyncDisposable
{
    private readonly KeyCloakScenario _scenario;
    private readonly IHost _host;

    public TestWebApplicationFactory(KeyCloakScenario scenario)
    {
        _scenario = scenario;

        // Create and start the host once during construction
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();

                webBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["KeyCloak:Domain"] = _scenario.Domain,
                        ["KeyCloak:Authority"] = _scenario.Authority,
                        ["KeyCloak:Audience"] = _scenario.Audience
                    });
                });

                webBuilder.ConfigureServices((context, services) =>
                {
                    // Add KeyCloak JWT validation
                    var authBuilder = services.AddKeyCloakApiAuthentication(options =>
                    {
                        options.Domain = context.Configuration["KeyCloak:Domain"]
                                       ?? throw new InvalidOperationException("KeyCloak:Domain is required");
                        options.Authority = context.Configuration["KeyCloak:Authority"]; // null = use https://{Domain}
                        options.JwtBearerOptions = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions
                        {
                            Audience = context.Configuration["KeyCloak:Audience"]
                                     ?? throw new InvalidOperationException("KeyCloak:Audience is required"),
                            RequireHttpsMetadata = false
                        };
                    });

                    // Configure DPoP based on scenario
                    if (_scenario.IsDPoPEnabled)
                    {
                        authBuilder.WithDPoP(dpopOptions =>
                        {
                            dpopOptions.Mode = _scenario.IsDPoPRequired
                                ? DPoPModes.Required
                                : DPoPModes.Allowed;
                        });
                    }

                    services.AddAuthorization();
                    services.AddRouting();
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseEndpoints(endpoints =>
                    {
                        // Open endpoint - no authentication required
                        endpoints.MapGet("/api/public", () => new { message = "This is a public endpoint" })
                           .WithName("PublicEndpoint");

                        // Protected endpoint - authentication required
                        endpoints.MapGet("/api/protected", () => new { message = "This is a protected endpoint" })
                           .WithName("ProtectedEndpoint")
                           .RequireAuthorization();
                    });
                });
            })
            .Build();

        _host.Start();
    }

    /// <summary>
    /// Creates a new HttpClient instance for a test.
    /// Each client is isolated with its own headers.
    /// </summary>
    public HttpClient CreateClient()
    {
        return _host.GetTestServer().CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
