using System.Net;
using System.Text;

using AspNet.KeyCloak.DPoP;
using AspNet.KeyCloak.DPoP.DPoP;
using AspNet.KeyCloak.DPoP.DPoP.EventHandlers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

public class ChallengeHandlerTests
{
    private readonly ChallengeHandler _handler = new();

    [Theory]
    [InlineData("DPoP", "invalid_token", "The token is invalid",
        "DPoP error=\"invalid_token\", error_description=\"The token is invalid\"")]
    [InlineData("Bearer", "invalid_request", null, "Bearer error=\"invalid_request\"")]
    [InlineData("DPoP", null, "Missing DPoP proof", "DPoP error_description=\"Missing DPoP proof\"")]
    [InlineData("Bearer", null, null, "Bearer")]
    [InlineData("DPoP", "", "", "DPoP")]
    public void PopulateWwwAuthenticateHeader_With_Various_Input_Combinations_Produces_Expected_Output(
        string initialValue, string? errorCode, string? errorDescription, string expectedResult)
    {
        var handler = new ChallengeHandler();
        var builder = new StringBuilder(initialValue);

        handler.PopulateWwwAuthenticateHeader(builder, errorCode, errorDescription);

        builder.ToString().Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("Bearer", "invalid\rtoken", null, "Bearer error=\"invalid token\"")]
    [InlineData("DPoP", null, "Token\nis\ninvalid", "DPoP error_description=\"Token is invalid\"")]
    [InlineData("Bearer", "invalid\ttoken", null, "Bearer error=\"invalid token\"")]
    [InlineData("DPoP", null, "Token \"expired\"", "DPoP error_description=\"Token 'expired'\"")]
    [InlineData("Bearer", "invalid\r\n\t\"token", "Description\r\n\t\"here",
        "Bearer error=\"invalid   'token\", error_description=\"Description   'here\"")]
    public void PopulateWwwAuthenticateHeader_Sanitizes_Special_Characters(
        string initialValue, string? errorCode, string? errorDescription, string expectedResult)
    {
        var handler = new ChallengeHandler();
        var builder = new StringBuilder(initialValue);

        handler.PopulateWwwAuthenticateHeader(builder, errorCode, errorDescription);

        builder.ToString().Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("DPoP", "   ", null, "DPoP")]
    [InlineData("Bearer", null, "   ", "Bearer")]
    public void PopulateWwwAuthenticateHeader_With_Whitespace_Values_Does_Not_Append(
        string initialValue, string? errorCode, string? errorDescription, string expectedResult)
    {
        var handler = new ChallengeHandler();
        var builder = new StringBuilder(initialValue);

        handler.PopulateWwwAuthenticateHeader(builder, errorCode, errorDescription);

        builder.ToString().Should().Be(expectedResult);
    }

    [Fact]
    public void PopulateWwwAuthenticateHeader_Preserves_Existing_Builder_Content()
    {
        var handler = new ChallengeHandler();
        var builder = new StringBuilder("DPoP algs=\"ES256\"");

        handler.PopulateWwwAuthenticateHeader(builder, "use_dpop_nonce", "Nonce required");

        builder.ToString().Should()
            .Be("DPoP algs=\"ES256\" error=\"use_dpop_nonce\", error_description=\"Nonce required\"");
    }

    [Fact]
    public void PopulateWwwAuthenticateHeader_With_Object_ErrorCode_Converts_ToString()
    {
        var handler = new ChallengeHandler();
        var builder = new StringBuilder("Bearer");
        var errorCode = new { Code = "invalid_token" };

        handler.PopulateWwwAuthenticateHeader(builder, errorCode, null);

        builder.ToString().Should().Contain("error=\"");
    }

    [Fact]
    public void PopulateWwwAuthenticateHeader_With_Object_Description_Converts_ToString()
    {
        var handler = new ChallengeHandler();
        var builder = new StringBuilder("DPoP");
        var description = new { Message = "Token expired" };

        handler.PopulateWwwAuthenticateHeader(builder, null, description);

        builder.ToString().Should().Contain("error_description=\"");
    }

    [Fact]
    public void BuildAuthenticateHeader_WithValidErrorCodeAndDescription_SetsStatusCodeAndAppendsHeader()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_token";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorDescription] = "The token is invalid";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(401);
        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].ToString()
            .Should().Contain("DPoP error=\"invalid_token\", error_description=\"The token is invalid\"");
    }

    [Fact]
    public void BuildAuthenticateHeader_WithErrorCodeOnly_SetsStatusCodeAndAppendsHeaderWithoutDescription()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_token";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(401);
        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].ToString()
            .Should().Contain("DPoP error=\"invalid_token\"")
            .And.NotContain("error_description");
    }

    [Fact]
    public void BuildAuthenticateHeader_WithErrorDescriptionOnly_SetsStatusCodeAndAppendsHeaderWithoutErrorCode()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorDescription] = "The token is invalid";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(401);
        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].ToString()
            .Should().Contain("DPoP error_description=\"The token is invalid\"")
            .And.NotContain("error=\"");
    }

    [Fact]
    public void BuildAuthenticateHeader_WithNoErrorsAndIncludeRealmTrue_AppendsRealmToHeader()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "Bearer",
            KeyCloakConstants.DPoP.BearerErrorCode,
            KeyCloakConstants.DPoP.BearerErrorDescription,
            KeyCloakConstants.DPoP.BearerStatusCode,
            true
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(401);
        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].ToString()
            .Should().Contain($"Bearer {KeyCloakConstants.DPoP.Error.DefaultRealm}");
    }

    [Fact]
    public void BuildAuthenticateHeader_WithErrorsAndIncludeRealmTrue_DoesNotAppendRealm()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerErrorCode] = "invalid_token";
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "Bearer",
            KeyCloakConstants.DPoP.BearerErrorCode,
            KeyCloakConstants.DPoP.BearerErrorDescription,
            KeyCloakConstants.DPoP.BearerStatusCode,
            true
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(401);
        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].ToString()
            .Should().NotContain(KeyCloakConstants.DPoP.Error.DefaultRealm);
    }

    [Fact]
    public void BuildAuthenticateHeader_WithNoErrorsAndIncludeRealmFalse_DoesNotAppendRealm()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(401);
        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].ToString()
            .Should().NotContain(KeyCloakConstants.DPoP.Error.DefaultRealm);
    }

    [Fact]
    public void BuildAuthenticateHeader_WithNullStatusCode_DefaultsToBadRequest()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_token";

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public void BuildAuthenticateHeader_WithInvalidStatusCodeType_DefaultsToBadRequest()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_token";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = "not_a_status_code";

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public void BuildAuthenticateHeader_CallsHandleResponse()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_token";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Handled.Should().BeTrue();
    }

    [Fact]
    public void BuildAuthenticateHeader_WithMultipleErrorsInItems_OnlyUsesConfiguredKeys()
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "dpop_error";
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerErrorCode] = "bearer_error";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorDescription] = "DPoP description";
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerErrorDescription] = "Bearer description";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = HttpStatusCode.Unauthorized;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "Bearer",
            KeyCloakConstants.DPoP.BearerErrorCode,
            KeyCloakConstants.DPoP.BearerErrorDescription,
            KeyCloakConstants.DPoP.BearerStatusCode,
            true
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].ToString()
            .Should().Contain("bearer_error")
            .And.Contain("Bearer description")
            .And.NotContain("dpop_error")
            .And.NotContain("DPoP description");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.Forbidden, 403)]
    [InlineData(HttpStatusCode.BadRequest, 400)]
    [InlineData(HttpStatusCode.InternalServerError, 500)]
    public void BuildAuthenticateHeader_WithVariousStatusCodes_SetsCorrectStatusCode(HttpStatusCode statusCode,
        int expectedCode)
    {
        JwtBearerChallengeContext context = CreateChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_token";
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPStatusCode] = statusCode;

        var config = new ChallengeHandler.AuthenticateHeaderConfig(
            "DPoP",
            KeyCloakConstants.DPoP.DPoPErrorCode,
            KeyCloakConstants.DPoP.DPoPErrorDescription,
            KeyCloakConstants.DPoP.DPoPStatusCode,
            false
        );

        _handler.BuildAuthenticateHeader(context, config);

        context.Response.StatusCode.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData(DPoPModes.Disabled)]
    [InlineData(DPoPModes.Allowed)]
    [InlineData(DPoPModes.Required)]
    public async Task Handle_should_route_to_correct_mode_handler(DPoPModes mode)
    {
        JwtBearerChallengeContext context = CreateJwtBearerChallengeContext();
        var dPoPOptions = new DPoPOptions { Mode = mode };
        IServiceProvider serviceProvider = CreateServiceProvider(dPoPOptions);
        context.HttpContext.RequestServices = serviceProvider;

        await _handler.Handle(context);

        context.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_should_throw_ArgumentNullException_when_context_is_null()
    {
        Func<Task> act = async () => await _handler.Handle(null);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HandleAllowedMode_should_always_append_default_DPoP_header()
    {
        JwtBearerChallengeContext context = CreateJwtBearerChallengeContext();

        await _handler.HandleAllowedMode(context);

        context.Response.Headers.Should().ContainKey(KeyCloakConstants.DPoP.WWWAuthenticateHeader);
        context.Response.Headers[KeyCloakConstants.DPoP.WWWAuthenticateHeader].Should()
            .Contain($"DPoP {KeyCloakConstants.DPoP.Error.DefaultDPoPAlgs}");
    }

    [Fact]
    public async Task HandleAllowedMode_should_build_bearer_header_when_bearer_error_exists()
    {
        JwtBearerChallengeContext context = CreateJwtBearerChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.BearerErrorCode] = "invalid_token";
        var mockHandler = A.Fake<ChallengeHandler>(o => o.CallsBaseMethods());
        A.CallTo(() => mockHandler.BuildAuthenticateHeader(A<JwtBearerChallengeContext>._, A<ChallengeHandler.AuthenticateHeaderConfig>._)).DoesNothing();

        await mockHandler.HandleAllowedMode(context);

        A.CallTo(() => mockHandler.BuildAuthenticateHeader(
                A<JwtBearerChallengeContext>._,
                A<ChallengeHandler.AuthenticateHeaderConfig>.That.Matches(c => c.Scheme == KeyCloakConstants.DPoP.Error.BearerScheme)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task HandleAllowedMode_should_build_DPoP_header_when_DPoP_error_exists()
    {
        JwtBearerChallengeContext context = CreateJwtBearerChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_dpop_proof";
        var mockHandler = A.Fake<ChallengeHandler>(o => o.CallsBaseMethods());
        A.CallTo(() => mockHandler.BuildAuthenticateHeader(A<JwtBearerChallengeContext>._, A<ChallengeHandler.AuthenticateHeaderConfig>._)).DoesNothing();

        await mockHandler.HandleAllowedMode(context);

        A.CallTo(() => mockHandler.BuildAuthenticateHeader(
                A<JwtBearerChallengeContext>._,
                A<ChallengeHandler.AuthenticateHeaderConfig>.That.Matches(c => c.Scheme == KeyCloakConstants.DPoP.Error.DPoPScheme)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task HandleAllowedMode_should_not_build_additional_headers_when_no_errors_exist()
    {
        JwtBearerChallengeContext context = CreateJwtBearerChallengeContext();
        var mockHandler = A.Fake<ChallengeHandler>(o => o.CallsBaseMethods());
        A.CallTo(() => mockHandler.BuildAuthenticateHeader(A<JwtBearerChallengeContext>._, A<ChallengeHandler.AuthenticateHeaderConfig>._)).DoesNothing();

        await mockHandler.HandleAllowedMode(context);

        A.CallTo(() => mockHandler.BuildAuthenticateHeader(A<JwtBearerChallengeContext>._, A<ChallengeHandler.AuthenticateHeaderConfig>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task HandleRequiredMode_should_build_dpop_header_when_dpop_error_exists()
    {
        JwtBearerChallengeContext context = CreateJwtBearerChallengeContext();
        context.HttpContext.Items[KeyCloakConstants.DPoP.DPoPErrorCode] = "invalid_token";
        var mockHandler = A.Fake<ChallengeHandler>(o => o.CallsBaseMethods());
        A.CallTo(() => mockHandler.BuildAuthenticateHeader(A<JwtBearerChallengeContext>._, A<ChallengeHandler.AuthenticateHeaderConfig>._)).DoesNothing();

        await mockHandler.HandleRequiredMode(context);

        A.CallTo(() => mockHandler.BuildAuthenticateHeader(
                A<JwtBearerChallengeContext>._,
                A<ChallengeHandler.AuthenticateHeaderConfig>.That.Matches(c => c.Scheme == KeyCloakConstants.DPoP.Error.DPoPScheme)))
            .MustHaveHappenedOnceExactly();
    }

    private JwtBearerChallengeContext CreateJwtBearerChallengeContext()
    {
        var httpContext = new DefaultHttpContext();
        var authScheme = new AuthenticationScheme("KeyCloak", "KeyCloak", typeof(JwtBearerHandler));
        var authProperties = new AuthenticationProperties();
        return new JwtBearerChallengeContext(httpContext, authScheme, new JwtBearerOptions(), authProperties);
    }

    private IServiceProvider CreateServiceProvider(DPoPOptions dPoPOptions)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dPoPOptions);
        return services.BuildServiceProvider();
    }

    private JwtBearerChallengeContext CreateChallengeContext()
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme("TestScheme", "TestScheme", typeof(JwtBearerHandler));
        var options = new JwtBearerOptions();

        return new JwtBearerChallengeContext(httpContext, scheme, options, new AuthenticationProperties());
    }
}
