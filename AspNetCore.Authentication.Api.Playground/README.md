# Auth0 ASP.NET Core API Playground

This is a playground application for testing and demonstrating the `Auth0.AspNetCore.Authentication.Api` library. It provides a simple ASP.NET Core Web API with Auth0 JWT authentication configured, allowing developers to quickly test changes and validate functionality.

## Overview

The playground application includes:

- A minimal ASP.NET Core Web API (.NET 8.0)
- Auth0 JWT Bearer authentication integration
- Swagger/OpenAPI documentation
- Two sample endpoints (one open, one protected)
- Pre-configured Postman collection for easy testing

## Prerequisites

- .NET 8.0 SDK
- An Auth0 account and application configured for API access
- Postman (optional, for using the pre-configured collection)

## Configuration

Before running the application, you need to configure your Auth0 settings:

1. Copy the `appsettings.json` file and update the Auth0 configuration:

```json
{
  "Auth0": {
    "Domain": "your-auth0-domain.auth0.com",
    "Audience": "your-api-identifier"
  }
}
```

### Required Auth0 Configuration

- **Domain**: Your Auth0 domain (e.g., `my-app.auth0.com`)
- **Audience**: The identifier for your API in Auth0

## Running the Application

1. Navigate to the playground directory:
   ```bash
   cd Auth0.AspNetCore.Authentication.Api.Playground
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. The application will start and be available at:
    - HTTP: `http://localhost:5059`
    - HTTPS: `https://localhost:7190` (check your `launchSettings.json` for exact ports)

4. Open Swagger UI in your browser:
    - Navigate to `/swagger` (e.g., `https://localhost:7190/swagger`)

## Available Endpoints

### Open Endpoint

- **GET** `/open-endpoint`
- **Description**: Publicly accessible endpoint that doesn't require authentication
- **Response**: Simple message confirming access

### Restricted Endpoint

- **GET** `/restricted-endpoint`
- **Description**: Protected endpoint that requires a valid JWT token
- **Authorization**: Bearer/DPoP token required
- **Response**: Message confirming authenticated access

## Testing Authentication

### Using the Pre-configured Postman Collection

The easiest way to test the API endpoints is using the provided Postman collection:

1. **Import the Collection**: Import the `Auth0-AspNetCore-API-Playground.postman_collection.json` file into Postman
2. **Authenticate**: Use your preferred Auth0 authentication method to obtain a JWT token
3. **Set Token Variable**: In Postman, set the `{{token}}` variable with your JWT token:
    - Go to the collection variables or environment variables
    - Set `token` = `your-jwt-token-here` (without the "Bearer " prefix)
4. **Test Endpoints**: Use the pre-configured GET requests:
    - **Open Endpoint**: Test the public endpoint (no authentication required)
    - **Restricted Endpoint**: Test the protected endpoint (uses the `{{token}}` variable automatically)

The collection includes:

- Pre-configured request URLs for both endpoints
- Proper headers setup for the restricted endpoint
- Environment variable support for easy token management

### Using Swagger UI

1. Obtain a JWT token from Auth0 (using your configured application)
2. In Swagger UI, click the "Authorize" button
3. Enter `Bearer <your-jwt-token>` in the authorization field
4. Test the restricted endpoint

### Using curl

```bash
# Test open endpoint
curl -X GET "https://localhost:7190/open-endpoint"

# Test restricted endpoint (replace with your actual token)
curl -X GET "https://localhost:7190/restricted-endpoint" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

### Obtaining a JWT Token

You can obtain a JWT token for testing in several ways:

1. **Auth0 Dashboard**: Use the "APIs" section to test your API
2. **Auth0 CLI**: Use the Auth0 CLI to get a token
3. **Postman**: Configure Auth0 authentication in Postman
4. **Direct API call**: Make a request to your Auth0 token endpoint

Example using curl to get a token (replace with your values):

```bash
curl --request POST \
  --url 'https://YOUR_DOMAIN.auth0.com/oauth/token' \
  --header 'content-type: application/x-www-form-urlencoded' \
  --data grant_type=client_credentials \
  --data client_id=YOUR_CLIENT_ID \
  --data client_secret=YOUR_CLIENT_SECRET \
  --data audience=YOUR_API_IDENTIFIER
```

## Development

This playground application references the main `Auth0.AspNetCore.Authentication.Api` project locally, so any changes you make to the authentication library will be immediately available for testing.

### Project Structure

```
Auth0.AspNetCore.Authentication.Api.Playground/
├── Program.cs                                        # Main application entry point
├── appsettings.json                                 # Configuration file
├── appsettings.Development.json                     # Development-specific config
├── Auth0-AspNetCore-API-Playground.postman_collection.json  # Postman collection for testing
├── Properties/
│   └── launchSettings.json                         # Launch profiles
└── *.csproj                                        # Project file
```

### Making Changes

1. Make changes to the `Auth0.AspNetCore.Authentication.Api` library
2. The playground will automatically reference the updated code
3. Restart the playground application to test your changes
4. Use the Swagger UI, Postman collection, or direct HTTP calls to validate functionality

## Troubleshooting

### Common Issues

1. **401 Unauthorized on restricted endpoint**
    - Verify your JWT token is valid and not expired
    - Check that the token audience matches your configured audience
    - Ensure the token issuer matches your Auth0 domain

2. **Configuration errors**
    - Verify all Auth0 settings in `appsettings.json` are correct
    - Ensure your Auth0 API is properly configured
    - Check that your Auth0 application has the necessary grants

3. **SSL/HTTPS issues in development**
    - Trust the development certificate: `dotnet dev-certs https --trust`
    - Or use the HTTP profile instead of HTTPS

### Debugging

- Enable detailed logging by setting the log level to `Debug` in `appsettings.Development.json`
- Check the console output for authentication-related errors
- Use the browser's developer tools to inspect request/response headers

## Contributing

This playground is designed to help with development and testing of the Auth0 ASP.NET Core API authentication library. Feel free to add additional endpoints or modify the configuration to test different scenarios.
