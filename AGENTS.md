# KeyCloak ASP.NET Core API - AI Agent Instructions

This is an **KeyCloak authentication SDK** for ASP.NET Core APIs providing **JWT Bearer authentication with DPoP (Demonstration of Proof-of-Possession) support**. It wraps `Microsoft.AspNetCore.Authentication.JwtBearer` with KeyCloak-specific configuration and RFC 9449 DPoP validation.

## Architecture Overview

### Core Design Pattern: Fluent Builder with Extension Point
- **Entry**: `ServiceCollectionExtensions.AddKeyCloakApiAuthentication()` → returns `KeyCloakApiAuthenticationBuilder`
- **DPoP**: Optional via `builder.WithDPoP()` - adds validation services and event handlers
- **Options**: `KeyCloakApiOptions` wraps `JwtBearerOptions` + `Domain`; `DPoPOptions` configures DPoP behavior
- **Validation Pipeline**: JWT Bearer events → DPoP event handlers (MessageReceived, TokenValidation, Challenge) → `DPoPProofValidationService`

### Key Components
- **`src/KeyCloak.AspNetCore.Authentication.Api/`**: Main library
  - `ServiceCollectionExtensions.cs`: Primary API surface - `AddKeyCloakApiAuthentication()`
  - `AuthenticationBuilderExtensions.cs`: DPoP enablement via `.WithDPoP()`, internal JWT Bearer setup
  - `KeyCloakApiAuthenticationBuilder.cs`: Fluent builder returned from setup methods
  - **`DPoP/`**: Complete RFC 9449 implementation
    - `DPoPProofValidationService.cs`: Core validation logic (JWK extraction, signature, claims, thumbprint binding)
    - `EventHandlers/`: Intercept JWT Bearer events to inject DPoP validation
    - `DPoPOptions.cs`: Mode (`Allowed`/`Required`/`Disabled`), timing (`IatOffset`, `Leeway`)

### DPoP Enforcement Modes
1. **Allowed** (default): Accept both Bearer and DPoP tokens - enables gradual migration
2. **Required**: Reject Bearer tokens, only accept DPoP - strict security
3. **Disabled**: Standard JWT Bearer only

## Development Workflows

### Building
```bash
dotnet restore KeyCloak.AspNetCore.Authentication.Api.sln
dotnet build KeyCloak.AspNetCore.Authentication.Api.sln --configuration Release
```

### Testing
```bash
# Unit tests (mocks, no KeyCloak connection)
dotnet test tests/AspNetCore.Authentication.Api.UnitTests/

# Integration tests (requires KeyCloak environment variables)
dotnet test tests/AspNetCore.Authentication.Api.IntegrationTests/
```

**Integration test pattern**: `TestWebApplicationFactory` creates TestServer → `KeyCloakTokenHelper` obtains real tokens → `DPoPHelper` generates DPoP proofs with EC keys

### Playground Testing
```bash
cd KeyCloak.AspNetCore.Authentication.Api.Playground
# Configure KeyCloak:Domain and KeyCloak:Audience in appsettings.json
dotnet run
# Open https://localhost:7190/swagger
```
Use `KeyCloak.AspNetCore.Authentication.Api.Playground.postman_collection.json` for pre-configured API calls

### Documentation Generation
```bash
./build-docs.sh  # Builds project + runs docfx
# View: sudo docfx serve docs → http://localhost:8080
```

## Critical Patterns & Conventions

### Options Configuration Pattern
```csharp
// ALWAYS use this pattern - KeyCloakApiOptions wraps JwtBearerOptions
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = "tenant.KeyCloak.com";  // NO https:// prefix
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = "https://api-identifier",
        // Any standard JWT Bearer option works here
    };
});
```

### DPoP Header Validation Flow
1. **MessageReceived** event: Extract DPoP proof from `DPoP` header, access token from `Authorization: DPoP <token>`
2. **TokenValidated** event: Call `DPoPProofValidationService.ValidateAsync()` with `DPoPProofValidationParameters`
3. **Validation checks**: JWK extraction → signature verification → `cnf` claim thumbprint match → `htm`/`htu`/`iat` claim validation
4. **Challenge** event: Add `DPoP` to `WWW-Authenticate` on 401 failures

### Event Handler Chaining
DPoP events **wrap** user-defined `JwtBearerEvents`. Example in `DPoPEventsFactory.Create()`:
```csharp
// Preserve user's custom event, execute DPoP handler first
Events.OnMessageReceived = async context => {
    await dpopHandler.HandleMessageReceived(context);
    if (userEvents?.OnMessageReceived != null)
        await userEvents.OnMessageReceived(context);
};
```

### Error Handling Convention
- DPoP errors use `KeyCloakConstants.DPoP.Error.Code.*` (e.g., `invalid_dpop_proof`, `invalid_request`)
- Always fail request with `context.Fail()` + descriptive error in `DPoPProofValidationResult`
- Log errors via `ILogger<T>` at ERROR level for failed validations

## Testing Guidelines

### Unit Tests
- Use xUnit `[Fact]` and `[Theory]`
- Mock `IDPoPProofValidationService` for event handler tests
- Test each DPoP validator independently (see `DPoPProofValidationService.cs` internal methods)

### Integration Tests
- Inherit from `IAsyncLifetime` for test setup/teardown
- Use `KeyCloakScenario` enum to configure different test environments (Basic, DPoPAllowed, DPoPRequired)
- **Never hardcode tokens** - use `KeyCloakTokenHelper.GetClientCredentialsTokenAsync()` with environment variables
- DPoP tests must create real EC keys: `ECDsa.Create(ECCurve.NamedCurves.nistP256)`

## Common Pitfalls

1. **Domain format**: Must be `tenant.KeyCloak.com` NOT `https://tenant.KeyCloak.com` - code auto-prepends `https://`
2. **InternalsVisibleTo**: Tests access internal validators - declared in `.csproj` `<InternalsVisibleTo>`
3. **DPoP mode confusion**: `Allowed` mode validates DPoP IF present, `Required` mode rejects Bearer tokens entirely
4. **Event preservation**: When modifying `AuthenticationBuilderExtensions.cs`, ALWAYS preserve existing user events via `JwtBearerEventsFactory.CreateBaseEvents()`
5. **Token validation timing**: Use `IatOffset` (default 300s) for clock skew, `Leeway` (default 30s) for lifetime checks

## File Organization

```
src/KeyCloak.AspNetCore.Authentication.Api/
  ├── ServiceCollectionExtensions.cs        # IServiceCollection.AddKeyCloakApiAuthentication()
  ├── AuthenticationBuilderExtensions.cs    # AuthenticationBuilder.AddKeyCloakApiAuthentication(), .WithDPoP()
  ├── KeyCloakApiAuthenticationBuilder.cs      # Fluent builder
  ├── KeyCloakApiOptions.cs                    # Domain + JwtBearerOptions wrapper
  ├── KeyCloakJwtBearerPostConfigureOptions.cs # IPostConfigureOptions - sets Authority from Domain
  └── DPoP/
      ├── DPoPProofValidationService.cs     # Core RFC 9449 implementation
      ├── DPoPOptions.cs                    # Mode, IatOffset, Leeway
      ├── DPoPEventHandlers.cs              # Coordinates MessageReceived, TokenValidated, Challenge
      └── EventHandlers/                    # Individual event handler implementations
```

## KeyCloak-Specific Behaviors

- **Authority construction**: Automatically creates `https://{Domain}` from `options.Domain`
- **Audience validation**: Uses standard JWT Bearer audience validation (not KeyCloak-specific)
- **Scope claims**: KeyCloak includes scopes in `scope` claim as space-separated string (see `EXAMPLES.md`)
- **DPoP support**: KeyCloak DPoP tokens have `cnf.jkt` claim with JWK thumbprint (SHA-256 of public key)

## When Modifying Code

### Adding New DPoP Validators
1. Create internal method in `DPoPProofValidationService.cs`
2. Call from `ValidateAsync()` pipeline
3. Set errors via `result.SetError(code, description)` using constants from `KeyCloakConstants.DPoP.Error`
4. Add unit tests in `KeyCloak.AspNetCore.Authentication.Api.UnitTests`

### Changing DPoP Modes
- Update `DPoPModes` enum
- Modify `MessageReceivedHandler.cs` and `TokenValidationHandler.cs` switch statements
- Add mode-specific tests in `tests/KeyCloak.AspNetCore.Authentication.Api.IntegrationTests/`

### Package Updates
- Version in `Directory.Build.props` (`<VersionPrefix>`)
- Release notes URL in `KeyCloak.AspNetCore.Authentication.Api.csproj` (`<PackageReleaseNotes>`)
- Target framework is .NET 8.0+ only (no multi-targeting)

## Key Files for Understanding Features

- **Migration scenarios**: `MIGRATION.md` - 8 before/after examples
- **Usage patterns**: `EXAMPLES.md` - 16 copy-paste scenarios
- **DPoP validation**: `src/KeyCloak.AspNetCore.Authentication.Api/DPoP/DPoPProofValidationService.cs` (515 lines)
- **Setup flow**: `src/KeyCloak.AspNetCore.Authentication.Api/AuthenticationBuilderExtensions.cs:ConfigureJwtBearerOptions()`
