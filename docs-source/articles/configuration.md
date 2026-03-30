# Configuration Guide

This guide covers all configuration options available in the Auth0 ASP.NET Core API Authentication library.

## Basic Configuration

The most basic configuration requires only two settings:

```csharp
builder.Services.AddAuth0ApiAuthentication(options =>
{
    options.Domain = "your-tenant.auth0.com";
    options.JwtBearerOptions = new JwtBearerOptions()
    {
        Audience = "https://your-api-identifier"
    };
});
```

## Configuration Options

### Auth0ApiOptions

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Domain` | string | Yes | Your Auth0 tenant domain (e.g., "your-tenant.auth0.com") |
| `JwtBearerOptions` | JwtBearerOptions | Yes | Standard JWT Bearer options with Auth0 configurations |

### JwtBearerOptions

The library exposes all standard `JwtBearerOptions` properties from ASP.NET Core. For a complete list of available options and their descriptions, refer to the [Microsoft JwtBearerOptions API documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer.jwtbeareroptions).

## Environment-Specific Configuration

### Using Configuration Files

**appsettings.json** (shared settings):
```json
{
  "Auth0": {
    "Audience": "https://your-api-identifier"
  }
}
```

**appsettings.Development.json**:
```json
{
  "Auth0": {
    "Domain": "dev-tenant.auth0.com"
  }
}
```

**appsettings.Production.json**:
```json
{
  "Auth0": {
    "Domain": "prod-tenant.auth0.com"
  }
}
```

### Using Environment Variables

```bash
export Auth0__Domain="your-tenant.auth0.com"
export Auth0__Audience="https://your-api-identifier"
```

```csharp
builder.Services.AddAuth0ApiAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.JwtBearerOptions = new JwtBearerOptions()
    {
        Audience = builder.Configuration["Auth0:Audience"]
    };
});
```

## Next Steps

- [DPoP Overview](dpop-overview.md) - Understanding DPoP and its security benefits
- [Getting Started with DPoP](dpop-getting-started.md) - Enable DPoP in your API
- [API Reference](../api/Auth0.AspNetCore.Authentication.Api.yml) - Complete API documentation
