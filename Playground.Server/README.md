# Playground.Server

This is the backend ASP.NET Core Web API for testing the `AspNet.KeyCloak.DPoP` library. It runs as part of the **Aspire AppHost** alongside Keycloak and the React frontend.

## Overview

- Minimal ASP.NET Core Web API (.NET 10.0)
- KeyCloak JWT Bearer authentication with DPoP enabled
- Swagger/OpenAPI documentation
- Two sample endpoints (one open, one protected)
- CORS configured to allow requests from the Vite frontend

## Running

The recommended way to run this project is via the **Aspire AppHost**, which starts Keycloak, the backend, and the frontend together:

```bash
cd AppHost
dotnet run
```

This injects all required environment variables automatically (Keycloak domain, authority, audience, CORS origin).

To run the backend standalone (e.g. pointing at an existing Keycloak):

```bash
cd Playground.Server
dotnet run
```

Configure `appsettings.json` or environment variables before running standalone:

```json
{
  "KeyCloak": {
    "Domain": "localhost:8080/realms/test/",
    "Authority": "http://localhost:8080/realms/test",
    "Audience": "test-api"
  },
  "BACKEND_CORS_ORIGIN": "https://localhost:3000"
}
```

## Available Endpoints

### Open Endpoint

- **GET** `/open-endpoint`
- No authentication required
- Returns a plain text message

### Restricted Endpoint

- **GET** `/restricted-endpoint`
- Requires a valid JWT (Bearer or DPoP token)
- Returns a plain text message confirming authenticated access

## Testing Authentication

### Swagger UI

1. Navigate to `/swagger` (e.g. `https://localhost:3445/swagger` via Aspire)
2. Obtain a JWT token from Keycloak (use the frontend or Keycloak admin)
3. Click **Authorize** and enter `Bearer <your-jwt-token>`
4. Test the restricted endpoint

### curl

```bash
# Open endpoint (no auth)
curl -X GET "https://localhost:3445/open-endpoint"

# Restricted endpoint with Bearer token
curl -X GET "https://localhost:3445/restricted-endpoint" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Obtaining a Token from Keycloak

Use the client credentials flow (replace values as needed):

```bash
curl --request POST \
  --url 'http://localhost:8080/realms/test/protocol/openid-connect/token' \
  --header 'content-type: application/x-www-form-urlencoded' \
  --data grant_type=client_credentials \
  --data client_id=YOUR_CLIENT_ID \
  --data client_secret=YOUR_CLIENT_SECRET \
  --data audience=test-api
```

## Project Structure

```
Playground.Server/
├── Program.cs                  # Main entry point - authentication and endpoints
├── appsettings.json            # Default configuration (empty values, filled by AppHost)
├── appsettings.Development.json
├── Properties/
│   └── launchSettings.json
└── Playground.Server.csproj
```

## Troubleshooting

- **401 on restricted endpoint**: Verify the token audience matches `KeyCloak:Audience` and the issuer matches the configured authority.
- **CORS errors**: Ensure `BACKEND_CORS_ORIGIN` matches the frontend origin exactly.
- **SSL errors in development**: Trust the dev cert with `dotnet dev-certs https --trust`, or use the HTTP profile.
- **Keycloak connection errors**: The AppHost uses `WaitFor(keycloak)` to delay startup until Keycloak is ready. Standalone runs may fail until Keycloak is up.
