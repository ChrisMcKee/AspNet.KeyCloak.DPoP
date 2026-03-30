using System.Net;
using System.Net.Http.Headers;

namespace IntegrationTests;

/// <summary>
/// Integration tests for KeyCloak DPoP authentication in Allowed mode.
/// In Allowed mode, both DPoP-bound tokens and regular Bearer tokens are accepted.
/// </summary>
[Collection(KeyCloakCollection.Name)]
public class DPoPAllowedModeTests : IAsyncLifetime
{
    private readonly KeyCloakFixture _fixture;
    private TestWebApplicationFactory? _factory;
    private KeyCloakTokenHelper? _tokenHelper;
    private DPoPHelper? _dpopHelper;
    private KeyCloakScenario? _scenario;

    public DPoPAllowedModeTests(KeyCloakFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _scenario = _fixture.WithDPoPAllowed;
        _factory = new TestWebApplicationFactory(_scenario);
        _tokenHelper = new KeyCloakTokenHelper(_scenario.Domain, _scenario.ClientId, _scenario.ClientSecret, _scenario.Audience);
        _dpopHelper = new DPoPHelper();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _dpopHelper?.Dispose();
        await (_factory?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ProtectedEndpoint_WithBearerToken_ReturnsOk()
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
    public async Task ProtectedEndpoint_WithDPoPToken_ReturnsOk()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // Get DPoP-bound access token from KeyCloak
        var dpopToken = await _dpopHelper!.GetDPoPAccessTokenAsync(
            _scenario!.Domain,
            _scenario.DPoPClientId ?? _scenario.ClientId,
            _scenario.DPoPClientSecret ?? _scenario.ClientSecret,
            _scenario.Audience
        );

        // Create DPoP proof for the API request
        var requestUrl = $"{client.BaseAddress}api/protected";
        var dpopProof = _dpopHelper.CreateDPoPProof("GET", requestUrl, dpopToken);

        // Set up the request with DPoP headers
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", dpopToken);
        client.DefaultRequestHeaders.Add("DPoP", dpopProof);

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("protected endpoint");
    }

    #endregion

    #region Sad Path Tests

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsBadRequest()
    {
        // Arrange
        using HttpClient client = _factory!.CreateClient();
        // No token is set

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/protected");

        // Assert
        // In DPoP Allowed mode, missing token returns 400 BadRequest instead of 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidBearerToken_ReturnsUnauthorized()
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
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        using HttpClient client = _factory!.CreateClient();

        // Use a clearly expired JWT token (exp claim in the past)
        var expiredToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUuYXV0aDAuY29tLyIsInN1YiI6InRlc3QiLCJhdWQiOiJ0ZXN0IiwiZXhwIjoxfQ.invalid";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithDPoPTokenButMissingProof_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // Get DPoP-bound access token from KeyCloak
        var dpopToken = await _dpopHelper!.GetDPoPAccessTokenAsync(
            _scenario!.Domain,
            _scenario.DPoPClientId ?? _scenario.ClientId,
            _scenario.DPoPClientSecret ?? _scenario.ClientSecret,
            _scenario.Audience
        );

        // Set authorization header but intentionally NOT adding DPoP proof header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", dpopToken);

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Public Endpoint Tests

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

    [Fact]
    public async Task PublicEndpoint_WithBearerToken_ReturnsOk()
    {
        // Arrange
        using HttpClient client = _factory!.CreateClient();
        var accessToken = await _tokenHelper!.GetAccessTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        HttpResponseMessage response = await client.GetAsync("/api/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("public endpoint");
    }

    #endregion
}
