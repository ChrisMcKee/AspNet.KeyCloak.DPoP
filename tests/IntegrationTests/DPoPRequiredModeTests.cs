using System.Net;
using System.Net.Http.Headers;

namespace IntegrationTests;

/// <summary>
/// Integration tests for KeyCloak DPoP authentication in Required mode.
/// In Required mode, only DPoP-bound tokens are accepted. Regular Bearer tokens are rejected.
/// </summary>
[Collection(KeyCloakCollection.Name)]
public class DPoPRequiredModeTests : IAsyncLifetime
{
    private readonly KeyCloakFixture _fixture;
    private TestWebApplicationFactory? _factory;
    private KeyCloakTokenHelper? _tokenHelper;
    private DPoPHelper? _dpopHelper;
    private KeyCloakScenario? _scenario;

    public DPoPRequiredModeTests(KeyCloakFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _scenario = _fixture.WithDPoPRequired;
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
    public async Task ProtectedEndpoint_WithDPoPToken_ReturnsOk()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // Get DPoP-bound access token from KeyCloak
        var dpopToken = await _dpopHelper!.GetDPoPAccessTokenAsync(
            _scenario!.Domain,
            _scenario.ClientId,
            _scenario.ClientSecret,
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
    public async Task ProtectedEndpoint_WithBearerToken_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // For this test, we'll use a mock Bearer token instead of getting one from KeyCloak
        // because the KeyCloak client in Required mode won't issue Bearer tokens
        var mockBearerToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUuYXV0aDAuY29tLyIsInN1YiI6InRlc3QiLCJhdWQiOiJ0ZXN0IiwiZXhwIjoxfQ.mock";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mockBearerToken);

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert
        // In DPoP Required mode, invalid Bearer tokens return BadRequest (400) before scheme validation
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory!.CreateClient();
        // No token is set

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert
        // In DPoP Required mode, missing token returns 400 BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidDPoPToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        var invalidToken = "invalid.dpop.token.here";
        var requestUrl = $"{client.BaseAddress}api/protected";
        var dpopProof = _dpopHelper!.CreateDPoPProof("GET", requestUrl, invalidToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", invalidToken);
        client.DefaultRequestHeaders.Add("DPoP", dpopProof);

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithDPoPTokenButMissingProof_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // Get DPoP-bound access token
        var dpopToken = await _dpopHelper!.GetDPoPAccessTokenAsync(
            _scenario!.Domain,
            _scenario.ClientId,
            _scenario.ClientSecret,
            _scenario.Audience
        );

        // Set authorization header but omit DPoP proof
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", dpopToken);

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert - Should fail because DPoP proof is missing
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithDPoPTokenButInvalidProof_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // Get DPoP-bound access token from KeyCloak
        var dpopToken = await _dpopHelper!.GetDPoPAccessTokenAsync(
            _scenario!.Domain,
            _scenario.ClientId,
            _scenario.ClientSecret,
            _scenario.Audience
        );

        // Set authorization header with an invalid DPoP proof
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", dpopToken);
        client.DefaultRequestHeaders.Add("DPoP", "invalid.dpop.proof");

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert
        // Invalid DPoP proof should return 400 BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithDPoPProofForWrongMethod_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // Get DPoP-bound access token from KeyCloak
        var dpopToken = await _dpopHelper!.GetDPoPAccessTokenAsync(
            _scenario!.Domain,
            _scenario.ClientId,
            _scenario.ClientSecret,
            _scenario.Audience
        );

        // Create DPoP proof for POST but use it with GET request
        var requestUrl = $"{client.BaseAddress}api/protected";
        var dpopProof = _dpopHelper.CreateDPoPProof("POST", requestUrl, dpopToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", dpopToken);
        client.DefaultRequestHeaders.Add("DPoP", dpopProof);

        // Act
        var response = await client.GetAsync("/api/protected");

        // Assert
        // Mismatched HTTP method in proof should return 400 BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Public Endpoint Tests

    [Fact]
    public async Task PublicEndpoint_WithoutToken_ReturnsOk()
    {
        // Arrange
        using var client = _factory!.CreateClient();
        // No token is set

        // Act
        var response = await client.GetAsync("/api/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("public endpoint");
    }

    [Fact]
    public async Task PublicEndpoint_WithDPoPToken_ReturnsOk()
    {
        // Arrange
        using var client = _factory!.CreateClient();

        // Get DPoP-bound access token from KeyCloak
        var dpopToken = await _dpopHelper!.GetDPoPAccessTokenAsync(
            _scenario!.Domain,
            _scenario.ClientId,
            _scenario.ClientSecret,
            _scenario.Audience
        );

        // Create DPoP proof for the API request
        var requestUrl = $"{client.BaseAddress}api/public";
        var dpopProof = _dpopHelper.CreateDPoPProof("GET", requestUrl, dpopToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DPoP", dpopToken);
        client.DefaultRequestHeaders.Add("DPoP", dpopProof);

        // Act
        var response = await client.GetAsync("/api/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("public endpoint");
    }

    #endregion
}
