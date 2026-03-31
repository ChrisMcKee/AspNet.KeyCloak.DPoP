using System.Net;
using System.Net.Http.Headers;

namespace IntegrationTests;

/// <summary>
/// Integration tests for KeyCloak JWT token validation middleware.
/// </summary>
[Collection(KeyCloakCollection.Name)]
public class TokenValidationIntegrationTests : IAsyncLifetime
{
    private readonly KeyCloakFixture _fixture;
    private TestWebApplicationFactory? _factory;
    private KeyCloakTokenHelper? _tokenHelper;

    public TokenValidationIntegrationTests(KeyCloakFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var scenario = _fixture.WithoutDPoP;
        _factory = new TestWebApplicationFactory(scenario);
        _tokenHelper = new KeyCloakTokenHelper(scenario.Domain, scenario.ClientId, scenario.ClientSecret, scenario.Audience);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await (_factory?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        // Arrange
        using HttpClient client = _factory!.CreateClient();
        var accessToken = await _tokenHelper!.GetAccessTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("protected endpoint");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        using HttpClient client = _factory!.CreateClient();
        // No token is set

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        using HttpClient client = _factory!.CreateClient();
        var invalidToken = "invalid.token.here";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicEndpoint_WithoutToken_ReturnsOk()
    {
        // Arrange
        using HttpClient client = _factory!.CreateClient();
        // No token is set

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("public endpoint");
    }
}
