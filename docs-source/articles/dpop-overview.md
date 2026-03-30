# DPoP Overview

This guide introduces DPoP (Demonstration of Proof-of-Possession) support in the Auth0 ASP.NET Core API Authentication library.

## What is DPoP?

DPoP (Demonstration of Proof-of-Possession) is an OAuth 2.0 security enhancement that binds access tokens to specific client instances, providing stronger security guarantees than traditional bearer tokens.

To learn more about DPoP, how it works, and its security benefits, refer to the [Auth0 DPoP Documentation](https://auth0.com/docs/secure/sender-constraining/demonstrating-proof-of-possession-dpop).

## DPoP in This Library

This library provides comprehensive DPoP validation for your ASP.NET Core APIs:

### Core Capabilities

- ✅ **Complete DPoP Validation** - Validates proof token structure, signature, and claims according to OAuth 2.0 DPoP specification
- ✅ **Token Binding Verification** - Ensures the DPoP proof matches the access token's `cnf` claim
- ✅ **Request Binding** - Validates `htm` (HTTP method) and `htu` (HTTP URI) claims
- ✅ **Replay Protection** - Validates `iat` (issued at) and `jti` (JWT ID) claims with configurable time windows
- ✅ **Flexible Modes** - Support for Required, Allowed, and Disabled modes

### Validation Process

When a request comes in with DPoP, the library:

1. **Extracts Headers** - Retrieves the `Authorization` header (access token) and `DPoP` header (proof token)
2. **Validates Proof Structure** - Ensures the DPoP proof is a valid JWT with required claims
3. **Verifies Signature** - Validates the proof signature using the embedded public key (JWK)
4. **Checks Token Binding** - Compares JWK thumbprint with the `cnf` claim in the access token
5. **Validates Request Binding** - Ensures `htm` matches HTTP method and `htu` matches request URI
6. **Checks Freshness** - Validates `iat` is within acceptable time window
7. **Returns Result** - Allows or denies the request based on validation outcome

## DPoP Modes

The library supports three enforcement modes:

### Allowed (Default)

```csharp
.WithDPoP(options =>
{
    options.Mode = DPoPModes.Allowed;
});
```

- DPoP tokens are validated if the `DPoP` header is present
- Standard bearer tokens are also accepted
- Best for gradual migration or mixed environments

### Required

```csharp
.WithDPoP(options =>
{
    options.Mode = DPoPModes.Required;
});
```

- Only DPoP-bound tokens are accepted
- Requests without valid DPoP proofs are rejected
- Maximum security for sensitive APIs

### Disabled

```csharp
.WithDPoP(options =>
{
    options.Mode = DPoPModes.Disabled;
});
```

- DPoP validation is completely disabled
- Only standard JWT Bearer authentication is performed
- Useful for temporarily disabling DPoP

## Next Steps

- [Getting Started with DPoP](dpop-getting-started.md) - Enable DPoP in your API
- [DPoP Configuration](dpop-configuration.md) - Detailed configuration options
- [Getting Started](getting-started.md) - Basic Auth0 API setup

## Resources

- [OAuth 2.0 DPoP Specification (RFC 9449)](https://datatracker.ietf.org/doc/html/rfc9449)
- [Auth0 DPoP Documentation](https://auth0.com/docs/secure/tokens/token-best-practices/proof-of-possession)
- [IETF OAuth Working Group](https://datatracker.ietf.org/wg/oauth/about/)
