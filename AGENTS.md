# AspNet.KeyCloak.DPoP - AI Agent Instructions

This is an **KeyCloak authentication SDK** for ASP.NET Core APIs providing **JWT Bearer authentication with DPoP (Demonstration of Proof-of-Possession) support**. It wraps `Microsoft.AspNetCore.Authentication.JwtBearer` with KeyCloak-specific configuration and RFC 9449 DPoP validation.

## Architecture Overview

### Core Design Pattern: Fluent Builder with Extension Point
- **Entry**: `ServiceCollectionExtensions.AddKeyCloakApiAuthentication()` → returns `KeyCloakApiAuthenticationBuilder`
- **DPoP**: Optional via `builder.WithDPoP()` - adds validation services and event handlers
- **Options**: `KeyCloakApiOptions` wraps `JwtBearerOptions` + `Domain` + optional `Authority`; `DPoPOptions` configures DPoP behavior
- **Validation Pipeline**: JWT Bearer events → DPoP event handlers (MessageReceived, TokenValidation, Challenge) → `DPoPProofValidationService`

### Key Components
- **`src/AspNet.KeyCloak.DPoP/`**: Main library (namespace: `AspNet.KeyCloak.DPoP`)
  - `ServiceCollectionExtensions.cs`: Primary API surface - `AddKeyCloakApiAuthentication()` on `IServiceCollection`
  - `AuthenticationBuilderExtensions.cs`: DPoP enablement via `.WithDPoP()`, internal JWT Bearer setup
  - `KeyCloakApiAuthenticationBuilder.cs`: Fluent builder returned from setup methods
  - **`DPoP/`**: Complete RFC 9449 implementation
    - `DPoPProofValidationService.cs`: Core validation logic (JWK extraction, signature, claims, thumbprint binding)
    - `DPoPEventsFactory.cs`: Creates JWT Bearer events with DPoP handlers wired in
    - `EventHandlers/`: `MessageReceivedHandler`, `TokenValidationHandler`, `ChallengeHandler`
    - `DPoPOptions.cs`: Mode (`Allowed`/`Required`/`Disabled`), timing (`IatOffset`, `Leeway`)
    - `InMemoryDPoPJtiCache.cs`: Default in-memory jti replay protection cache

### DPoP Enforcement Modes
1. **Allowed** (default): Accept both Bearer and DPoP tokens - enables gradual migration
2. **Required**: Reject Bearer tokens, only accept DPoP - strict security
3. **Disabled**: Standard JWT Bearer only

## Development Workflows

### Building
```bash
dotnet restore AspNet.KeyCloak.DPoP.sln
dotnet build AspNet.KeyCloak.DPoP.sln --configuration Release
```

### Testing
```bash
# Unit tests (mocks, no KeyCloak connection)
dotnet test tests/UnitTests/

# Integration tests (starts KeyCloak via Testcontainers; Docker required)
dotnet test tests/IntegrationTests/
```

**Integration test pattern**: `KeyCloakFixture` starts a shared `Testcontainers.Keycloak` container from `tests/IntegrationTests/realm-test.json` → `TestWebApplicationFactory` creates TestServer → `KeyCloakTokenHelper` obtains real tokens for the active `KeyCloakScenario` → `DPoPHelper` generates DPoP proofs with EC keys

### Playground Testing (Aspire)

The playground uses **.NET Aspire** to orchestrate all components:

```bash
cd AppHost
dotnet run
```

This starts:
- **Keycloak** (HTTPS on port 3443) with the `test` realm pre-imported from `AppHost/Realms/`
- **Playground.Server** (backend API, port 3445) - minimal API with `AddOpenApi()` + `MapScalarApiReference()`, configured via Aspire environment variables
- **Playground.Frontend** (Vite/React, port 3000) - TypeScript OIDC client demonstrating DPoP flows

The Aspire dashboard is available at `https://localhost:17216`.

## Critical Patterns & Conventions

### Options Configuration Pattern
```csharp
// ALWAYS use this pattern - KeyCloakApiOptions wraps JwtBearerOptions
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = "localhost:8080/realms/test/";  // Used to construct authority as https://{Domain}
    // OR override authority explicitly (useful for local HTTP):
    options.Authority = "http://localhost:8080/realms/test";
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = "your-api-identifier",
        // Any standard JWT Bearer option works here
    };
});
```

**Authority resolution**: `Authority` takes precedence over `Domain`. If `Authority` is null, the library constructs `https://{Domain}`.

### DPoP Header Validation Flow
1. **MessageReceived** event: Extract DPoP proof from `DPoP` header, access token from `Authorization: DPoP <token>`
2. **TokenValidated** event: Call `DPoPProofValidationService.ValidateAsync()` with `DPoPProofValidationParameters`
3. **Validation checks**: JWK extraction → signature verification → `cnf` claim thumbprint match → `htm`/`htu`/`iat` claim validation → `jti` replay check
4. **Challenge** event: Add `DPoP` to `WWW-Authenticate` on 401 failures

### Event Handler Chaining
DPoP events **wrap** user-defined `JwtBearerEvents`. `DPoPEventsFactory.Create()` sets up the chain using the options passed from the builder.

### Error Handling Convention
- DPoP errors use `KeyCloakConstants.DPoP.Error.Code.*` (e.g., `invalid_dpop_proof`, `invalid_request`)
- Always fail request with `context.Fail()` + descriptive error in `DPoPProofValidationResult`
- Log errors via `ILogger<T>` at ERROR level for failed validations

## Testing Guidelines

### Unit Tests (`tests/UnitTests/`)
- Use xUnit `[Fact]` and `[Theory]`
- Use FakeItEasy for mocking (not Moq)
- Use AwesomeAssertions for assertions (see `tests/UnitTests/GlobalUsings.cs`)
- Mock `IDPoPProofValidationService` for event handler tests
- Test each DPoP validator independently (see `DPoPProofValidationService.cs` internal methods)

### Integration Tests (`tests/IntegrationTests/`)
- Inherit from `IAsyncLifetime` for test setup/teardown
- Use `KeyCloakScenario` via `KeyCloakCollection` / `KeyCloakFixture`; the fixture shares one `Testcontainers.Keycloak` instance and seeds it from `realm-test.json`
- **Never hardcode tokens** - create `KeyCloakTokenHelper` from the active `KeyCloakScenario` (`_fixture.WithoutDPoP`, `_fixture.WithDPoPAllowed`, `_fixture.WithDPoPRequired`) and call `GetAccessTokenAsync()`
- DPoP tests must create real EC keys: `ECDsa.Create(ECCurve.NamedCurves.nistP256)` (see `DPoPHelper`)

## Common Pitfalls

1. **Authority vs Domain**: `Authority` overrides `Domain`. For local Keycloak running on HTTP, set `Authority` directly to avoid the auto-`https://` prefix.
2. **InternalsVisibleTo**: Declared in `AspNet.KeyCloak.DPoP.csproj` for both `UnitTests` and `IntegrationTests` - tests can access internal validators.
3. **DPoP mode confusion**: `Allowed` mode validates DPoP IF present, `Required` mode rejects Bearer tokens entirely.
4. **Event preservation**: `DPoPEventsFactory.Create()` preserves existing user-defined `JwtBearerEvents` from `KeyCloakApiOptions`.
5. **Token validation timing**: Use `IatOffset` (default 300s) for clock skew, `Leeway` (default 30s) for lifetime checks.
6. **jti replay cache**: `InMemoryDPoPJtiCache` is registered as singleton by `WithDPoP()`. For multi-instance deployments, replace with a distributed cache by implementing `IDPoPJtiCache`.
7. **Playground orchestration scope**: `AppHost/AppHost.cs` wires Keycloak, `Playground.Server`, and `Playground.Frontend` only. `Playground.Client` is a separate console app; run it manually with environment variables when you want scripted end-to-end DPoP calls.
8. **API docs in the playground**: `Playground.Server/Program.cs` uses `AddOpenApi()` + `MapScalarApiReference()` in development, not Swagger middleware.

## File Organization

```
src/AspNet.KeyCloak.DPoP/
  ├── ServiceCollectionExtensions.cs        # IServiceCollection.AddKeyCloakApiAuthentication()
  ├── AuthenticationBuilderExtensions.cs    # AuthenticationBuilder.AddKeyCloakApiAuthentication(), .WithDPoP()
  ├── KeyCloakApiAuthenticationBuilder.cs   # Fluent builder
  ├── KeyCloakApiOptions.cs                 # Domain, Authority, JwtBearerOptions wrapper
  ├── KeyCloakJwtBearerPostConfigureOptions.cs # IPostConfigureOptions - sets Authority from Domain
  ├── JwtBearerEventsFactory.cs             # Creates base JWT Bearer events from options
  ├── KeyCloakConstants.cs                  # DPoP constants (error codes, defaults, JWT typ)
  └── DPoP/
      ├── DPoPProofValidationService.cs     # Core RFC 9449 implementation
      ├── DPoPEventsFactory.cs              # Wires DPoP handlers into JWT Bearer events
      ├── DPoPModes.cs                      # Allowed / Required / Disabled enum
      ├── DPoPOptions.cs                    # Mode, IatOffset, Leeway
      ├── DPoPProofValidationParameters.cs  # Request/access-token context passed into validators
      ├── DPoPProofValidationResult.cs      # Error/result contract used by validators and handlers
      ├── DPoPEventHandlers.cs              # Coordinates the three handler types
      ├── IDPoPProofValidationService.cs    # Abstraction for validation service
      ├── IDPoPJtiCache.cs                  # Abstraction for jti replay cache
      ├── InMemoryDPoPJtiCache.cs           # Default in-memory jti cache
      └── EventHandlers/
          ├── DPoPEventHandlerBase.cs       # Shared header parsing and invalid-request handling
          ├── IDPoPEventHandler.cs          # Generic event-handler contract
          ├── MessageReceivedHandler.cs     # Extracts DPoP token from headers
          ├── TokenValidationHandler.cs     # Runs full DPoP proof validation
          └── ChallengeHandler.cs           # Adds DPoP to WWW-Authenticate

AppHost/
  ├── AppHost.cs                            # Aspire host - wires Keycloak + API + Frontend
  ├── KeycloakExtensions.cs                 # AddHttpsKeycloak() helper
  ├── DevCertHostingExtensions.cs           # RunWithHttpsDevCertificate() for Vite
  └── Realms/realm-configuration.json      # Keycloak realm import (test realm)

Playground.Server/                         # Backend ASP.NET Core API
Playground.Frontend/                       # Vite/React OIDC client (TypeScript)
Playground.Client/                         # Standalone DPoP client

tests/UnitTests/                           # Unit tests (FakeItEasy mocks)
tests/IntegrationTests/                    # Integration tests (Testcontainers Keycloak)
  ├── KeyCloakFixture.cs                   # Shared Testcontainers Keycloak bootstrap
  └── realm-test.json                      # Imported realm used by integration tests
```

## KeyCloak-Specific Behaviors

- **Authority construction**: Uses `Authority` if set; otherwise constructs `https://{Domain}` from `options.Domain`
- **Audience validation**: Uses standard JWT Bearer audience validation
- **DPoP support**: Keycloak DPoP tokens have `cnf.jkt` claim with JWK thumbprint (SHA-256 of public key)
- **Realm config**: The test realm (`AppHost/Realms/realm-configuration.json`) pre-configures clients, scopes, and DPoP settings for the playground

## When Modifying Code

### Adding New DPoP Validators
1. Create internal method in `DPoPProofValidationService.cs`
2. Call from `ValidateAsync()` pipeline
3. Set errors via `result.SetError(code, description)` using constants from `KeyCloakConstants.DPoP.Error`
4. Add unit tests in `tests/UnitTests/`

### Changing DPoP Modes
- Update `DPoPModes` enum in `DPoPModes.cs`
- Modify `MessageReceivedHandler.cs` and `TokenValidationHandler.cs` switch statements
- Add mode-specific tests in `tests/IntegrationTests/`

### Package Updates
- `PackageId` and version in `src/AspNet.KeyCloak.DPoP/AspNet.KeyCloak.DPoP.csproj`
- Central package versions in `Directory.Packages.props`
- Target framework is .NET 10.0 only (no multi-targeting)

## Key Files for Understanding Features

- **Usage patterns**: `EXAMPLES.md` - copy-paste scenarios
- **DPoP validation**: `src/AspNet.KeyCloak.DPoP/DPoP/DPoPProofValidationService.cs`
- **Setup flow**: `src/AspNet.KeyCloak.DPoP/AuthenticationBuilderExtensions.cs:ConfigureJwtBearerOptions()`
- **Aspire orchestration**: `AppHost/AppHost.cs`
- **Containerized integration setup**: `tests/IntegrationTests/KeyCloakFixture.cs`
