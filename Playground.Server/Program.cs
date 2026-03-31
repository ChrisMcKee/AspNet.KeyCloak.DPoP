using System.Net;
using System.Text.Json;

using AspNetCore.Authentication.Api;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Testcontainers.Keycloak;

// ---------------------------------------------------------------------------
// Step 1: Start Keycloak container on a fixed port so the client can be
// configured with a static address (localhost:8080/realms/test).
// ---------------------------------------------------------------------------
var realmFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, "realm-playground.json"));
if (!realmFile.Exists)
    throw new FileNotFoundException("Missing realm file. Ensure realm-playground.json is copied to the output directory.", realmFile.FullName);

Console.WriteLine("Starting Keycloak container on port 8080...");

var keycloakContainer = new KeycloakBuilder("quay.io/keycloak/keycloak:26.5.6")
    .WithPortBinding(8080, 8080)
    .WithResourceMapping(realmFile, new FileInfo("/opt/keycloak/data/import/realm-playground.json"))
    .WithCommand("--import-realm")
    .Build();

await keycloakContainer.StartAsync();
await WaitForRealmAsync("http://localhost:8080");

Console.WriteLine("Keycloak ready. Starting API server...");

// ---------------------------------------------------------------------------
// Step 2: Configure and start the web application.
// ---------------------------------------------------------------------------
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = builder.Configuration["KeyCloak:Domain"]
                     ?? throw new InvalidOperationException("KeyCloak:Domain is required");
    options.Authority = builder.Configuration["KeyCloak:Authority"]; // http://localhost:8080/realms/test
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"]
                   ?? throw new InvalidOperationException("KeyCloak:Audience is required"),
        RequireHttpsMetadata = false,
        Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                Console.WriteLine($"Token extracted? : {(!string.IsNullOrEmpty(context.Token) ? "yes" : "no")}");
                return Task.CompletedTask;
            }
        }
    };
}).WithDPoP();

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirect omitted — playground runs on plain HTTP (localhost:5059).
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/open-endpoint", () => "This endpoint is available to all users.")
    .WithName("AccessOpenEndpoint");

app.MapGet("/restricted-endpoint", () => "You are special. This endpoint is available only to select users.")
    .WithName("AccessRestrictedEndpoint")
    .RequireAuthorization();

try
{
    await app.RunAsync();
}
finally
{
    await keycloakContainer.DisposeAsync();
}

// ---------------------------------------------------------------------------
// Polls the OIDC discovery endpoint until the imported realm is visible.
// ---------------------------------------------------------------------------
static async Task WaitForRealmAsync(string baseUri)
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    var metadataUri = $"{baseUri}/realms/test/.well-known/openid-configuration";
    var deadline = DateTimeOffset.UtcNow.AddSeconds(60);

    while (DateTimeOffset.UtcNow < deadline)
    {
        try
        {
            using var response = await http.GetAsync(metadataUri);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("token_endpoint", out var ep)
                    && ep.GetString()?.Contains("/realms/test/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return;
                }
            }
        }
        catch (HttpRequestException) { /* Keycloak still booting */ }

        await Task.Delay(500);
    }

    throw new InvalidOperationException(
        "Keycloak started but the 'test' realm was not available within 60 seconds.");
}
