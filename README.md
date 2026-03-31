[![API Reference](https://img.shields.io/badge/API-Reference-blue)](https://KeyCloak.github.io/aspnetcore-api/index.html)
[![codecov](https://codecov.io/gh/KeyCloak/aspnetcore-api/branch/master/graph/badge.svg?token=0CF2BINXXJ)](https://codecov.io/gh/KeyCloak/aspnetcore-api)
[![License](https://img.shields.io/:license-Apache%202.0-blue.svg?style=flat)](https://www.apache.org/licenses/LICENSE-2.0)
[![NuGet Version](https://img.shields.io/nuget/v/KeyCloak.AspNetCore.Authentication.Api?style=flat&logo=nuget)](https://www.nuget.org/packages/KeyCloak.AspNetCore.Authentication.Api)
![Downloads](https://img.shields.io/nuget/dt/KeyCloak.AspNetCore.Authentication.Api)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/KeyCloak/aspnetcore-api)

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

- This library currently supports .NET 8.0 and above.

## Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package KeyCloak.AspNetCore.Authentication.Api
```

Or via the Package Manager Console:

```powershell
Install-Package KeyCloak.AspNetCore.Authentication.Api
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

### Complete Migration Guide

For detailed migration instructions including:
- 8 migration scenarios (basic to complex)
- Custom events and validation
- Multiple audiences
- Testing strategies
- Rollback procedures
- Troubleshooting (10+ common issues)

## Getting Started

### Basic Configuration

Add KeyCloak authentication to your ASP.NET Core API in `Program.cs`:

```csharp
using KeyCloak.AspNetCore.Authentication.Api;

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
    "Domain": "your-tenant.example.com",
    "Audience": "https://your-api-identifier"
  }
}
```

**Required Settings:**

- **Domain**: Your KeyCloak domain (e.g., `my-app.example.com`) - **without** the `https://` prefix
- **Audience**: The API identifier configured in your KeyCloak Dashboard

The library automatically constructs the authority URL as `https://{Domain}`.

## DPoP: Enhanced Token Security

**DPoP (Demonstration of Proof-of-Possession)** is a security mechanism that binds access tokens to a cryptographic key, making them resistant to token theft and replay attacks. This library provides seamless DPoP integration for your KeyCloak-protected APIs.

**Learn more about DPoP:** [KeyCloak DPoP Documentation](https://auth0.com/docs/secure/sender-constraining/demonstrating-proof-of-possession-dpop)

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
git clone https://github.com/ChrisMcKee/keycloak-dpo-api.git
cd aspnetcore-api
dotnet restore KeyCloak.AspNetCore.Authentication.Api.sln
dotnet build KeyCloak.AspNetCore.Authentication.Api.sln --configuration Release
```

### Running Tests

Run the unit test suite:

```bash
dotnet test tests/KeyCloak.AspNetCore.Authentication.Api.UnitTests/
```

### Playground Application

The repository includes a playground application for testing both standard JWT Bearer and **DPoP authentication**:

#### Setup

1. **Configure KeyCloak settings** in `KeyCloak.AspNetCore.Authentication.Api.Playground/appsettings.json`:
   ```json
   {
     "KeyCloak": {
       "Domain": "your-tenant.example.com",
       "Audience": "https://your-api-identifier"
     }
   }
   ```

2. **Run the playground**:
   ```bash
   cd KeyCloak.AspNetCore.Authentication.Api.Playground
   dotnet run
   ```

3. **Access the application**:
   - Swagger UI: `https://localhost:7190/swagger`
   - Open endpoint: GET `/open-endpoint` (no authentication required)
   - Protected endpoint: GET `/restricted-endpoint` (requires authentication)

#### Testing with Postman

The playground includes a pre-configured Postman collection (`KeyCloak.AspNetCore.Authentication.Api.Playground.postman_collection.json`) with ready-to-use requests:

1. Import the collection into Postman
2. Obtain a JWT token from KeyCloak
3. Set the `{{token}}` variable in your Postman environment
4. Test both endpoints with pre-configured headers

See the [Playground README](./KeyCloak.AspNetCore.Authentication.Api.Playground/README.md) for detailed testing instructions and examples.

## Contributing

We appreciate your contributions! Please review our [contribution guidelines](./.github/PULL_REQUEST_TEMPLATE.md) before submitting pull requests.

### Contribution Checklist

- ✅ Read the [KeyCloak General Contribution Guidelines](https://github.com/KeyCloak/open-source-template/blob/master/GENERAL-CONTRIBUTING.md)
- ✅ Read the [KeyCloak Code of Conduct](https://github.com/KeyCloak/open-source-template/blob/master/CODE-OF-CONDUCT.md)
- ✅ Ensure all tests pass
- ✅ Add tests for new functionality
- ✅ Update documentation as needed
- ✅ Sign all commits

## Support

If you have questions or need help:

- 📖 Check the [KeyCloak Documentation](https://KeyCloak.com/docs)
- � See [EXAMPLES.md](./EXAMPLES.md) for code examples
- 💬 Visit the [KeyCloak Community](https://community.KeyCloak.com/)
- 🐛 Report issues on [GitHub Issues](https://github.com/KeyCloak/aspnetcore-api/issues)

## License
Copyright 2026 Modified for KeyCloak by Chris McKee
Copyright 2025 Okta, Inc.

This project is licensed under the Apache License 2.0 – see the [LICENSE](LICENSE) file for details.

Authors
Okta Inc.
---
