using System.Net;
using System.Security.Claims;

using AspNetCore.Authentication.Api;
using AspNetCore.Authentication.Api.DPoP;
using AspNetCore.Authentication.Api.DPoP.EventHandlers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

using Moq;

namespace UnitTests;

public class TokenValidationHandlerTests
{
    private readonly Mock<IDPoPProofValidationService> _mockValidationService = new();

    [Theory]
    [InlineData("https", "example.com", 443, "/api/values", "", "https://example.com/api/values")]
    [InlineData("http", "localhost", 8080, "/test", "", "http://localhost:8080/test")]
    [InlineData("https", "mydomain.com", null, "/secure", "/base", "https://mydomain.com/base/secure")]
    public void BuildHtu_Returns_Correct_Uri(string scheme, string host, int? port, string path, string pathBase, string expectedUri)
    {
        HttpRequest request = CreateHttpRequest(scheme, host, port, path, pathBase);
        var result = TokenValidationHandler.BuildHtu(request);
        result.Should().Be(expectedUri);
    }

    [Fact]
    public void BuildHtu_Throws_InvalidOperationException_On_Invalid_Host()
    {
        HttpRequest request = CreateHttpRequest("https", "", null, "/fail", "");
        Action act = () => TokenValidationHandler.BuildHtu(request);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid URI components*");
    }

    [Theory]
    [InlineData("InvalidToken", "Token is invalid", HttpStatusCode.Unauthorized, "Token is invalid")]
    [InlineData("InvalidToken", null, HttpStatusCode.BadRequest, "InvalidToken")]
    [InlineData(null, null, HttpStatusCode.BadRequest, KeyCloakConstants.DPoP.Error.Description.UnknownError)]
    public void SetErrorContext_Sets_All_Context_Items_And_Fails_Correctly(string errorCode, string errorDescription, HttpStatusCode statusCode, string expectedFailureMessage)
    {
        // Arrange
        var context = CreateTokenValidatedContext();
        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError(errorCode, errorDescription);

        // Act
        TokenValidationHandler.SetErrorContext(context, validationResult, statusCode);

        // Assert
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode].Should().Be(errorCode);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode].Should().Be(statusCode);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorDescription].Should().Be(errorDescription);

        context.Result.Failure.Should().NotBeNull();
        context.Result.Failure.Message.Should().Be(expectedFailureMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetErrorContext_Throws_ArgumentNullException_When_Parameter_Is_Null(bool contextIsNull)
    {
        TokenValidatedContext? context = contextIsNull ? null : CreateTokenValidatedContext();
        DPoPProofValidationResult? validationResult = contextIsNull ? new DPoPProofValidationResult() : null;

        if (contextIsNull)
        {
            validationResult!.SetError("InvalidToken", "Token is invalid");
        }

        Action act = () =>
            TokenValidationHandler.SetErrorContext(context, validationResult, HttpStatusCode.Unauthorized);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CaptureErrors_ShouldReturn_WhenValidationResultHasNoError()
    {
        TokenValidatedContext context = CreateTokenValidatedContext();
        var validationResult = new DPoPProofValidationResult();
        TokenValidationHandler handler = CreateHandler();

        handler.CaptureErrors(context, validationResult, _ => throw new Exception("Should not be called"));

        context.HttpContext.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(KeyCloakConstants.DPoP.Error.Code.InvalidToken, HttpStatusCode.Unauthorized)]
    [InlineData(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof, HttpStatusCode.BadRequest)]
    public void CaptureErrors_ShouldSetErrorContext_WhenValidationResultHasSpecificError(string errorCode, HttpStatusCode expectedStatusCode)
    {
        TokenValidatedContext context = CreateTokenValidatedContext();
        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError(errorCode, errorCode);
        TokenValidationHandler handler = CreateHandler();

        handler.CaptureErrors(context, validationResult, _ => throw new Exception("Should not be called"));

        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode].Should().Be(errorCode);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode].Should().Be(expectedStatusCode);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorDescription].Should().Be(errorCode);
    }

    [Fact]
    public void CaptureErrors_ShouldInvokeHandleInvalidRequest_WhenErrorIsInvalidRequest()
    {
        TokenValidatedContext context = CreateTokenValidatedContext();
        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError(KeyCloakConstants.DPoP.Error.Code.InvalidRequest,
            KeyCloakConstants.DPoP.Error.Code.InvalidRequest);

        TokenValidationHandler handler = CreateHandler();
        var wasCalled = false;

        handler.CaptureErrors(context, validationResult, _ => wasCalled = true);

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public void CaptureErrors_ShouldNotThrow_WhenValidationResultIsNull()
    {
        TokenValidatedContext context = CreateTokenValidatedContext();
        TokenValidationHandler handler = CreateHandler();

        Action act = () => handler.CaptureErrors(context, null, _ => throw new Exception("Should not be called"));

        act.Should().NotThrow();
        context.HttpContext.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("DPoP", "proof-token", "proof-token")]
    [InlineData("dpop", "case-insensitive-token", "case-insensitive-token")]
    [InlineData("OtherHeader", "value", "")]
    public void GetDPoPProofToken_ReturnsExpectedValue(string headerName, string headerValue, string expectedResult)
    {
        // Arrange
        TokenValidatedContext context = CreateTokenValidatedContextWithHeader(headerName, headerValue);

        // Act
        var result = CreateHandler().GetDPoPProofToken(context);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void CreateDPoPProofValidationParameters_ReturnsParameters_WhenOptionsConfigured()
    {
        // Arrange
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();

        TokenValidationHandler handler = CreateHandler();

        // Act
        DPoPProofValidationParameters result = handler.CreateDPoPProofValidationParameters(context);

        // Assert
        result.Should().NotBeNull();
        result.Options.Should().NotBeNull();
        result.ProofToken.Should().Be("dpop-proof-token");
        result.AccessToken.Should().Be("dpop-access-token");
        result.Htm.Should().Be("GET");
        result.Htu.Should().Be("https://example.com/api/resource");
        result.AccessTokenClaims.Should().NotBeNull();
    }

    [Fact]
    public void CreateDPoPProofValidationParameters_Throws_WhenOptionsNotConfigured()
    {
        // Arrange
        TokenValidatedContext context = CreateTokenValidatedContextWithoutOptions();
        TokenValidationHandler handler = CreateHandler();

        // Act
        Action act = () => handler.CreateDPoPProofValidationParameters(context);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*KeyCloak DPoP options not configured*");
    }

    [Fact]
    public void CreateDPoPProofValidationParameters_Handles_NullClaims()
    {
        // Arrange
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        context.Principal = null;
        TokenValidationHandler handler = CreateHandler();

        // Act
        DPoPProofValidationParameters result = handler.CreateDPoPProofValidationParameters(context);

        // Assert
        result.AccessTokenClaims.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsyncInternal_ReturnsValidationResult_WhenServiceReturnsResult()
    {
        var serviceMock = new Mock<IDPoPProofValidationService>();
        var handler = new TokenValidationHandler(serviceMock.Object);
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        var expectedResult = new DPoPProofValidationResult();
        serviceMock.Setup(
                s => s.ValidateAsync(It.IsAny<DPoPProofValidationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        DPoPProofValidationResult? result = await handler.ValidateAsyncInternal(context);

        result.Should().BeSameAs(expectedResult);
    }

    [Fact]
    public async Task ValidateAsyncInternal_ThrowsInvalidOperationException_WhenOptionsNotConfigured()
    {
        var serviceMock = new Mock<IDPoPProofValidationService>();
        var handler = new TokenValidationHandler(serviceMock.Object);
        TokenValidatedContext context = CreateTokenValidatedContextWithoutOptions();

        Func<Task> act = async () => await handler.ValidateAsyncInternal(context);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*KeyCloak DPoP options not configured*");
    }

    [Fact]
    public async Task ValidateAsyncInternal_PassesCorrectParameters_ToValidationService()
    {
        var serviceMock = new Mock<IDPoPProofValidationService>();
        var handler = new TokenValidationHandler(serviceMock.Object);
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        DPoPProofValidationParameters? capturedParams = null;
        serviceMock.Setup(
                s => s.ValidateAsync(It.IsAny<DPoPProofValidationParameters>(), It.IsAny<CancellationToken>()))
            .Callback<DPoPProofValidationParameters, CancellationToken>((p, _) => capturedParams = p)
            .ReturnsAsync(new DPoPProofValidationResult());

        await handler.ValidateAsyncInternal(context);

        capturedParams.Should().NotBeNull();
        capturedParams.ProofToken.Should().NotBeNull();
        capturedParams.AccessToken.Should().NotBeNull();
        capturedParams.Htm.Should().Be(context.Request.Method);
        capturedParams.Htu.Should().NotBeNull();
        capturedParams.Options.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAllowedMode_DPoPSchemeWithValidationResultNull_ShouldNotFail()
    {
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        TokenValidationHandler handler = CreateHandlerWithValidationResult(null);

        await handler.HandleAllowedMode(context);
    }

    [Fact]
    public async Task HandleAllowedMode_DPoPSchemeWithNoProofHeader_ShouldFail()
    {
        TokenValidatedContext context = CreateTokenValidationContextWithDPoPSchemeAndNoProofHeader();
        TokenValidationHandler handler = CreateHandlerWithValidationResult(null);

        await handler.HandleAllowedMode(context);

        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerErrorCode]
            .Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerStatusCode]
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleAllowedMode_DPoPSchemeWithValidationResultInvalidToken_ShouldSetUnauthorizedErrorContext()
    {
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError(KeyCloakConstants.DPoP.Error.Code.InvalidToken,
            KeyCloakConstants.DPoP.Error.Code.InvalidToken);
        TokenValidationHandler handler = CreateHandlerWithValidationResult(validationResult);

        await handler.HandleAllowedMode(context);

        AssertDPoPErrorContext(context, KeyCloakConstants.DPoP.Error.Code.InvalidToken, HttpStatusCode.Unauthorized, KeyCloakConstants.DPoP.Error.Code.InvalidToken);
    }

    [Fact]
    public async Task HandleAllowedMode_DPoPSchemeWithValidationResultInvalidRequest_ShouldInvokeInvalidRequestHandler()
    {
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError(KeyCloakConstants.DPoP.Error.Code.InvalidRequest,
            KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        TokenValidationHandler handler = CreateHandlerWithValidationResult(validationResult);

        await handler.HandleAllowedMode(context);

        // InvalidRequest in allowed mode should set BearerErrorCode, not DPoPErrorCode
        AssertBearerErrorContext(context, KeyCloakConstants.DPoP.Error.Code.InvalidRequest, HttpStatusCode.BadRequest);

        // Should NOT set DPoP error codes
        context.HttpContext.Items.ContainsKey(KeyCloakConstants.DPoP.DPoPErrorCode).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAllowedMode_BearerSchemeWithProofHeaderAndBoundToken_ShouldFailWithInvalidToken()
    {
        TokenValidatedContext context = CreateTokenValidationContextWithBearerSchemeAndProofHeader();
        TokenValidationHandler handler = CreateHandlerWithValidationResult(null);

        await handler.HandleAllowedMode(context);

        AssertBearerErrorContext(context, KeyCloakConstants.DPoP.Error.Code.InvalidToken, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandleAllowedMode_BearerSchemeWithNoProofHeaderAndBoundToken_ShouldReturn()
    {
        TokenValidatedContext context = CreateTokenValidationContextWithBearerScheme();
        TokenValidationHandler handler = CreateHandlerWithValidationResult(null);

        await handler.HandleAllowedMode(context);

        AssertNoErrorsInContext(context);
    }

    [Fact]
    public async Task HandleAllowedMode_DPoPSchemeWithValidationResultInvalidDPoPProof_ShouldSetBadRequestErrorContext()
    {
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof,
            KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        TokenValidationHandler handler = CreateHandlerWithValidationResult(validationResult);

        await handler.HandleAllowedMode(context);

        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode]
            .Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode]
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleRequiredMode_ValidationServiceReturnsNull_NoErrorsCaptured()
    {
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        var mockValidationService = new Mock<IDPoPProofValidationService>();
        mockValidationService
            .Setup(s => s.ValidateAsync(It.IsAny<DPoPProofValidationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DPoPProofValidationResult?)null);

        var handler = new TokenValidationHandler(mockValidationService.Object);
        await handler.HandleRequiredMode(context);

        context.HttpContext.Items.ContainsKey(KeyCloakConstants.DPoP.DPoPErrorCode).Should().BeFalse();
        context.HttpContext.Items.ContainsKey(KeyCloakConstants.DPoP.BearerErrorCode).Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequiredMode_ValidationResultInvalidTokenWithoutDescription_SetsUnauthorizedErrorContext()
    {
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();
        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError(KeyCloakConstants.DPoP.Error.Code.InvalidToken, null);

        var mockValidationService = new Mock<IDPoPProofValidationService>();
        mockValidationService.Setup(s =>
                s.ValidateAsync(It.IsAny<DPoPProofValidationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var handler = new TokenValidationHandler(mockValidationService.Object);
        await handler.HandleRequiredMode(context);

        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode].Should()
            .Be(KeyCloakConstants.DPoP.Error.Code.InvalidToken);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode].Should()
            .Be(HttpStatusCode.Unauthorized);
        context.Result.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequiredMode_ValidationResultUnhandledErrorCode_DoesNotSetErrorContext()
    {
        TokenValidatedContext context = CreateTokenValidatedContextWithOptions();

        var validationResult = new DPoPProofValidationResult();
        validationResult.SetError("some_unhandled_error", "unexpected");

        var mockValidationService = new Mock<IDPoPProofValidationService>();
        mockValidationService.Setup(s =>
                s.ValidateAsync(It.IsAny<DPoPProofValidationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var handler = new TokenValidationHandler(mockValidationService.Object);
        await handler.HandleRequiredMode(context);

        context.HttpContext.Items.ContainsKey(KeyCloakConstants.DPoP.DPoPErrorCode).Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequiredMode_WithBearerScheme_FailsWithError()
    {
        TokenValidatedContext context = CreateTokenValidationContextWithBearerScheme();

        TokenValidationHandler handler = CreateHandler();
        await handler.HandleRequiredMode(context);

        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode].Should()
            .Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode].Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void HandleDisabledMode_returns_completed_task()
    {
        var handler = new TokenValidationHandler(new Mock<IDPoPProofValidationService>().Object);
        TokenValidatedContext context = CreateTokenValidatedContext();

        Task task = handler.HandleDisabledMode(context);

        task.Should().BeSameAs(Task.CompletedTask);
    }

    private static TokenValidatedContext CreateContext(DPoPModes mode)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DPoPOptions { Mode = mode });
        ServiceProvider provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };

        var scheme = new AuthenticationScheme("Test", null, typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();

        return new TokenValidatedContext(httpContext, scheme, options);
    }

    [Fact]
    public void Handle_null_context_throws_ArgumentNullException()
    {
        var svc = new Mock<IDPoPProofValidationService>();
        var handler = new TestTokenValidationHandler(svc.Object);

        Exception? ex = null;
        try
        {
            handler.Handle(null);
        }
        catch (Exception? e)
        {
            ex = e;
        }

        ex.Should().NotBeNull();
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Theory]
    [InlineData(DPoPModes.Disabled, true, false, false)]
    [InlineData(DPoPModes.Allowed, false, true, false)]
    [InlineData(DPoPModes.Required, false, false, true)]
    public async Task Handle_InvokesCorrectHandler_BasedOnMode(DPoPModes mode, bool expectedDisabledCalled, bool expectedAllowedCalled, bool expectedRequiredCalled)
    {
        var svc = new Mock<IDPoPProofValidationService>();
        var handler = new TestTokenValidationHandler(svc.Object);
        TokenValidatedContext context = CreateContext(mode);

        Task task = handler.Handle(context);
        await task;

        handler.DisabledCalled.Should().Be(expectedDisabledCalled);
        handler.AllowedCalled.Should().Be(expectedAllowedCalled);
        handler.RequiredCalled.Should().Be(expectedRequiredCalled);
    }

    [Fact]
    public async Task Handle_unrecognized_mode_returns_completed_task_without_invoking_handlers()
    {
        var svc = new Mock<IDPoPProofValidationService>();
        var handler = new TestTokenValidationHandler(svc.Object);
        TokenValidatedContext context = CreateContext((DPoPModes)999);

        Task task = handler.Handle(context);
        await task;

        task.IsCompleted.Should().BeTrue();
        handler.DisabledCalled.Should().BeFalse();
        handler.AllowedCalled.Should().BeFalse();
        handler.RequiredCalled.Should().BeFalse();
    }

    private TokenValidationHandler CreateHandlerWithValidationResult(DPoPProofValidationResult? validationResult)
    {
        _mockValidationService.Setup(s => s.ValidateAsync(It.IsAny<DPoPProofValidationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        return new TokenValidationHandler(_mockValidationService.Object);
    }

    private TokenValidatedContext CreateTokenValidatedContext(
        bool withOptions = false,
        string authorizationScheme = "DPoP",
        string? accessToken = "dpop-access-token",
        bool withProofHeader = true,
        string? proofToken = "dpop-proof-token",
        Dictionary<string, string>? customHeaders = null,
        ClaimsPrincipal? principal = null)
    {
        var httpContext = new DefaultHttpContext();

        if (withOptions)
        {
            var dPoPOptions = new DPoPOptions();
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(dPoPOptions)
                .BuildServiceProvider();
        }
        else
        {
            httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        }

        // Set up request properties
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.Path = "/api/resource";

        // Set authorization header
        if (!string.IsNullOrEmpty(accessToken))
        {
            httpContext.Request.Headers.Authorization = new StringValues($"{authorizationScheme} {accessToken}");
        }

        // Set DPoP proof header
        if (withProofHeader && !string.IsNullOrEmpty(proofToken))
        {
            httpContext.Request.Headers[KeyCloakConstants.DPoP.ProofHeader] = proofToken;
        }

        // Set custom headers
        if (customHeaders != null)
        {
            foreach (var header in customHeaders)
            {
                httpContext.Request.Headers[header.Key] = header.Value;
            }
        }

        // Set up principal
        var contextPrincipal = principal ?? new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(KeyCloakConstants.DPoP.Cnf, "some-cnf"),
            new Claim("sub", "user-id")
        }));

        var scheme = new AuthenticationScheme("KeyCloak", null, typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();

        var context = new Mock<TokenValidatedContext>(httpContext, scheme, options)
        {
            Object = { Principal = contextPrincipal }
        };

        return context.Object;
    }

    private TokenValidatedContext CreateTokenValidationContextWithBearerScheme()
    {
        return CreateTokenValidatedContext(
            authorizationScheme: "Bearer",
            withProofHeader: false,
            principal: new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", "user-id") // No cnf claim - not DPoP bound
            })));
    }

    private TokenValidatedContext CreateTokenValidationContextWithBearerSchemeAndProofHeader()
    {
        return CreateTokenValidatedContext(
            withOptions: true,
            authorizationScheme: "Bearer");
    }

    private TokenValidatedContext CreateTokenValidationContextWithDPoPSchemeAndNoProofHeader()
    {
        return CreateTokenValidatedContext(
            withOptions: true,
            withProofHeader: false);
    }

    private TokenValidatedContext CreateTokenValidatedContextWithOptions()
    {
        return CreateTokenValidatedContext(withOptions: true);
    }

    private TokenValidatedContext CreateTokenValidatedContextWithoutOptions()
    {
        return CreateTokenValidatedContext(withOptions: false);
    }

    private TokenValidatedContext CreateTokenValidatedContextWithHeader(string headerName, string headerValue)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Clear();
        httpContext.Request.Headers[headerName] = headerValue;

        return new TokenValidatedContext(httpContext,
            new AuthenticationScheme("KeyCloak", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions());
    }

    private TokenValidatedContext CreateTokenValidatedContext()
    {
        return CreateTokenValidatedContext(withOptions: false, accessToken: null, withProofHeader: false);
    }

    private TokenValidationHandler CreateHandler()
    {
        return new TokenValidationHandler(_mockValidationService.Object);
    }

    private static void AssertDPoPErrorContext(TokenValidatedContext context, string expectedErrorCode, HttpStatusCode expectedStatusCode, string expectedErrorDescription)
    {
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode].Should().Be(expectedErrorCode);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode].Should().Be(expectedStatusCode);
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorDescription].Should().Be(expectedErrorDescription);
    }

    private static void AssertBearerErrorContext(TokenValidatedContext context, string expectedErrorCode, HttpStatusCode expectedStatusCode)
    {
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerErrorCode].Should().Be(expectedErrorCode);
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerStatusCode].Should().Be(expectedStatusCode);
    }

    private static void AssertNoErrorsInContext(TokenValidatedContext context)
    {
        // Check that error codes are either not present or are null
        if (context.HttpContext.Items.ContainsKey(KeyCloakConstants.DPoP.DPoPErrorCode))
        {
            context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode].Should().BeNull();
        }

        if (context.HttpContext.Items.ContainsKey(KeyCloakConstants.DPoP.BearerErrorCode))
        {
            context.HttpContext.Items[KeyCloakConstants.DPoP.BearerErrorCode].Should().BeNull();
        }
    }

    private static DefaultHttpContext CreateHttpContext(
        string scheme, string host, int? port, string path, string pathBase)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = scheme,
                Host = port.HasValue ? new HostString(host, port.Value) : new HostString(host),
                Path = path,
                PathBase = pathBase
            }
        };
        return context;
    }

    private static HttpRequest CreateHttpRequest(string scheme, string host, int? port, string path, string pathBase)
    {
        DefaultHttpContext context = CreateHttpContext(scheme, host, port, path, pathBase);
        return context.Request;
    }
}


internal class TestTokenValidationHandler : TokenValidationHandler
{
    public bool DisabledCalled { get; private set; }
    public bool AllowedCalled { get; private set; }
    public bool RequiredCalled { get; private set; }

    internal TestTokenValidationHandler(IDPoPProofValidationService svc) : base(svc) { }

    internal override Task HandleDisabledMode(TokenValidatedContext context)
    {
        DisabledCalled = true;
        return Task.CompletedTask;
    }

    internal override Task HandleAllowedMode(TokenValidatedContext context)
    {
        AllowedCalled = true;
        return Task.CompletedTask;
    }

    internal override Task HandleRequiredMode(TokenValidatedContext context)
    {
        RequiredCalled = true;
        return Task.CompletedTask;
    }
}
