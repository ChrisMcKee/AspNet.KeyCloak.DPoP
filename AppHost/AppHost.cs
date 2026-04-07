using AppHost;

using Projects;

const int keycloakPort = 3443;
const int keycloakManagementPort = 3444;
const int backendPort = 3445;
const int frontendPort = 3000;
const int frontendTargetPort = 3000;

const string realm = "test";
const string accessTokenAudience = "rwo-backend";

const string keycloakDomain = "localhost";
const string frontendDomain = "localhost";

var authDomain = $"{keycloakDomain}:{keycloakPort}/realms/{realm}/";
var authAuthority = $"https://{keycloakDomain}:{keycloakPort}/realms/{realm}/";
var audience = $"test-api";
var corsOrigin = $"https://{frontendDomain}:{frontendPort}";
var client_id = "frontend-client";

var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder
               .AddHttpsKeycloak("keycloak", keycloakPort, keycloakManagementPort)
               // No persistent /opt/keycloak/data volume so imported realms are reapplied on fresh runs.
               .WithRealmImport("./Realms")
    ;

// Backend Api
var api = builder.AddProject<Playground_Server>("api")
                 .WithHttpsEndpoint(backendPort)
                 .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                 .WithEnvironment("DOTNET_ENVIRONMENT", Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
                 .WithEnvironment("KeyCloak__Domain", authDomain)
                 .WithEnvironment("KeyCloak__Authority", authAuthority)
                 .WithEnvironment("KeyCloak__Audience", audience)
                 .WithEnvironment("BACKEND_CORS_ORIGIN", corsOrigin)
                 .WithEnvironment("BACKEND_VALID_AUDIENCE", accessTokenAudience)
                 .WaitFor(keycloak)
                 .WithReference(keycloak)
    ;

builder.AddViteApp("frontend", "../Playground.Frontend", "dev")
       .WaitFor(api)
       .WithHttpsEndpoint(frontendPort, env: "VITE_PORT")
       .RunWithHttpsDevCertificate("CERT_PATH", "CERT_KEY_PATH")
       .WithEnvironment("BROWSER", "none")
       .WithEnvironment("OIDC_CLIENT_ID", client_id)
       .WithEnvironment("OIDC_ISSUER_URI", authAuthority)
       .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
       .WithEnvironment("VITE_PORT", frontendTargetPort.ToString())
       .WithEnvironment("PORT", frontendTargetPort.ToString())
       ;


builder.Build().Run();
