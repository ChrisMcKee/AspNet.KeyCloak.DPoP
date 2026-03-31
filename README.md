[![License](https://img.shields.io/:license-Apache%202.0-blue.svg?style=flat)](https://www.apache.org/licenses/LICENSE-2.0)
[![NuGet Version](https://img.shields.io/nuget/v/AspNet.KeyCloak.DPoP?style=flat&logo=nuget)](https://www.nuget.org/packages/AspNet.KeyCloak.DPoP)
![Downloads](https://img.shields.io/nuget/dt/AspNet.KeyCloak.DPoP)

A library that provides **everything the standard JWT Bearer authentication offers**, with the added power of **built-in DPoP (Demonstration of Proof-of-Possession)** support for enhanced token security. Simplify your KeyCloak JWT authentication integration for ASP.NET Core APIs with KeyCloak-specific configuration and validation.

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Migration from JWT Bearer](#migration-from-jwt-bearer)
- [Getting Started](#getting-started)
  - [Basic Configuration](#basic-configuration)
  - [Configuration Options](#configuration-options)
- [DPoP: Enhanced Token Security](#dpop-enhanced-token-security)
  - [Enabling DPoP](#enabling-dpop)
  - [DPoP Configuration Options](#dpop-configuration-options)
  - [DPoP Modes](#dpop-modes)
- [Advanced Features](#advanced-features)
  - [Using Full JWT Bearer Options](#using-full-jwt-bearer-options)
- [Examples](#examples)
- [Development](#development)
  - [Building the Project](#building-the-project)
  - [Running Tests](#running-tests)
  - [Playground Application](#playground-application)
- [Contributing](#contributing)
- [Support](#support)
- [License](#license)

## Features

This library builds on top of the standard `Microsoft.AspNetCore.Authentication.JwtBearer` package, providing:

- **Complete JWT Bearer Functionality** - All features from `Microsoft.AspNetCore.Authentication.JwtBearer` are available
- **Built-in DPoP Support** - Industry-leading proof-of-possession token security per [RFC 9449](https://datatracker.ietf.org/doc/html/rfc9449)
- **KeyCloak Optimized** - Pre-configured for KeyCloak's authentication patterns
- **Zero Lock-in** - Use standard JWT Bearer features alongside DPoP enhancements
- **Single Package** – Everything you need in one dependency
- **Flexible Configuration** – Options pattern with full access to underlying JWT Bearer configuration

## Requirements

- This library currently supports .NET 10.0 and above.

## Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package AspNet.KeyCloak.DPoP
```

Or via the Package Manager Console:

```powershell
Install-Package AspNet.KeyCloak.DPoP
```

## Migration from JWT Bearer

**Already using `Microsoft.AspNetCore.Authentication.JwtBearer`?** Great news! This library is a **drop-in replacement** with zero behavior changes.

### Quick Migration

Migrating from JWT Bearer is simple:

**Before:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["KeyCloak:Domain"]}";
        options.Audience = builder.Configuration["KeyCloak:Audience"];
    });
```

**After:**
```csharp
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = builder.Configuration["KeyCloak:Domain"];
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"]
    };
});
```

### What You Get

**Zero Breaking Changes** – All JWT Bearer functionality works identically
**5–15 Lines** – Typically only 5–15 lines of code change  
**Full Compatibility** – Custom events, validation, and policies work as-is  
**New Capabilities** – Optional DPoP support with zero refactoring

## Getting Started

### Basic Configuration

Add KeyCloak authentication to your ASP.NET Core API in `Program.cs`:

```csharp
using AspNet.KeyCloak.DPoP;

using Microsoft.AspNetCore.Authentication.JwtBearer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adds KeyCloak JWT validation to the API
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"]
    };
});

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/open-endpoint", () =>
    {
        var responseMessage = "This endpoint is available to all users.";
        return responseMessage;
    })
    .WithName("AccessOpenEndpoint")
    .WithOpenApi();

app.MapGet("/restricted-endpoint", () =>
    {
        var responseMessage = "This endpoint is available only to authenticated users.";
        return responseMessage;
    })
    .WithName("AccessRestrictedEndpoint")
    .WithOpenApi().RequireAuthorization();

app.Run();

```

> **Want more examples?** Check out [EXAMPLES.md](./EXAMPLES.md) for comprehensive code examples including authorization policies, scopes, permissions, custom handlers, and more!

### Configuration Options

Add the following settings to your `appsettings.json`:

```json
{
  "KeyCloak": {
    "Domain": "your-tenant.example.com/realms/your-realm/",
    "Audience": "your-api-identifier"
  }
}
```

**Required Settings:**

- **Domain**: Your KeyCloak domain including realm path (e.g., `my-host.example.com/realms/my-realm/`) - **without** the `https://` prefix
- **Audience**: The audience identifier for your API configured in KeyCloak

The library automatically constructs the authority URL as `https://{Domain}`.

**For local development** (where KeyCloak may run on HTTP), use the `Authority` option to override:

```csharp
options.Authority = "http://localhost:8080/realms/my-realm";
```

When `Authority` is set it takes precedence over `Domain`.

## DPoP: Enhanced Token Security

**DPoP (Demonstration of Proof-of-Possession)** is a security mechanism that binds access tokens to a cryptographic key, making them resistant to token theft and replay attacks. This library provides seamless DPoP integration for your KeyCloak-protected APIs.

**Learn more about DPoP:** [RFC 9449 - OAuth 2.0 Demonstrating Proof of Possession](https://datatracker.ietf.org/doc/html/rfc9449)

### Enabling DPoP

Enable DPoP with a single method call:

```csharp
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = builder.Configuration["KeyCloak:Domain"];
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"]
    };
}).WithDPoP(); // Enable DPoP support
```

That's it! Your API now supports DPoP tokens while maintaining backward compatibility with Bearer tokens.

### DPoP Configuration Options

For fine-grained control, configure DPoP behavior:

```csharp
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = builder.Configuration["KeyCloak:Domain"];
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"]
    };
}).WithDPoP(dpopOptions =>
{
    // Enforcement mode
    dpopOptions.Mode = DPoPModes.Required;
    
    // Time validation settings
    dpopOptions.IatOffset = 300; // Allow 300 seconds offset for 'iat' claim (default)
    dpopOptions.Leeway = 30;     // 30 seconds leeway for time-based validation (default)
});
```

### DPoP Modes

Choose the right enforcement mode for your security requirements:

| Mode                            | Description                                   |
|---------------------------------|-----------------------------------------------|
| `DPoPModes.Allowed` *(default)* | Accept both DPoP and Bearer tokens            |
| `DPoPModes.Required`            | Only accept DPoP tokens, reject Bearer tokens |
| `DPoPModes.Disabled`            | Standard JWT Bearer validation only           |

## Advanced Features

### Using Full JWT Bearer Options

Since this library provides **complete access to JWT Bearer configuration**, you can use any standard JWT Bearer option:

```csharp
builder.Services.AddKeyCloakApiAuthentication(options =>
{
    options.Domain = builder.Configuration["KeyCloak:Domain"];
    options.JwtBearerOptions = new JwtBearerOptions
    {
        Audience = builder.Configuration["KeyCloak:Audience"],
        
        // All standard JWT Bearer options are available
        RequireHttpsMetadata = true,
        SaveToken = true,
        IncludeErrorDetails = true,
        
        // Custom token validation parameters
        TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = ClaimTypes.NameIdentifier
        },
        
        // Event handlers for custom logic
        Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        }
    };
});
```

> **Looking for more advanced scenarios?** Visit [EXAMPLES.md](./EXAMPLES.md) for examples on:
> - Scope and permission-based authorization
> - Custom authorization handlers
> - Role-based access control
> - Custom JWT Bearer events
> - SignalR integration
> - Error handling and logging
> - And much more!

## Examples

For comprehensive, copy-pastable code examples covering various scenarios, see **[EXAMPLES.md](./EXAMPLES.md)**:

- **Getting Started** – Basic authentication and endpoint protection
- **Configuration** – Custom token validation and settings
- **DPoP** – All DPoP modes with practical examples
- **Authorization** – Scopes, permissions, roles, and custom handlers
- **Advanced Scenarios** – Claims, events, custom error responses
- **Integration** – SignalR and other integrations

## Development

### Building the Project

Clone the repository and build the solution:

```bash
git clone https://github.com/ChrisMcKee/aspnet-jwt-dpop.git
cd aspnet-jwt-dpop
dotnet restore AspNet.KeyCloak.DPoP.sln
dotnet build AspNet.KeyCloak.DPoP.sln --configuration Release
```

### Running Tests

Run the unit test suite (no external dependencies):

```bash
dotnet test tests/UnitTests/
```

Integration tests require a running KeyCloak instance (use the Aspire playground):

```bash
dotnet test tests/IntegrationTests/
```

### Playground Application

The repository includes a full playground using **.NET Aspire** to orchestrate all components. No manual KeyCloak setup required — the realm is imported automatically.

#### Setup

**Prerequisites**: .NET 10 SDK and Docker (for Keycloak)

1. **Run the Aspire AppHost**:
   ```bash
   cd AppHost
   dotnet run
   ```

2. **Access the components**:
   - **Aspire Dashboard**: `https://localhost:17216`
   - **Frontend** (Vite/React OIDC client): `https://localhost:3000`
   - **Backend API** (Swagger): `https://localhost:3445/swagger`
   - **Keycloak admin**: `https://localhost:3443`

The `test` realm is pre-configured with a `frontend-client` (public OIDC client with DPoP) and a `rwo-backend` API audience.

See the [Playground Server README](./Playground.Server/README.md) for endpoint details and manual testing instructions.

## Contributing

Contributions are welcome! Please ensure all tests pass and add tests for new functionality.

### Contribution Checklist

- ✅ Ensure all unit tests pass: `dotnet test tests/UnitTests/`
- ✅ Add tests for new functionality
- ✅ Update documentation as needed

## Support

If you have questions or need help:

- 📖 See [EXAMPLES.md](./EXAMPLES.md) for code examples
- 🐛 Report issues on [GitHub Issues](https://github.com/ChrisMcKee/aspnet-jwt-dpop/issues)

## License
Copyright 2026 Chris McKee
Copyright 2025 Okta, Inc.

This project is licensed under the Apache License 2.0 – see the [LICENSE](LICENSE) file for details.

Authors: Okta Inc., Chris McKee
---
