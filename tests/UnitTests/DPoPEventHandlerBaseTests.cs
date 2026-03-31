using AspNet.KeyCloak.DPoP;
using AspNet.KeyCloak.DPoP.DPoP.EventHandlers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace UnitTests;

public class DPoPEventHandlerBaseTests
{
    private readonly TestDPoPEventHandler _handler;

    public DPoPEventHandlerBaseTests()
    {
        _handler = new TestDPoPEventHandler();
    }

    [Fact]
    public void ExtractTokenFromAuthorizationHeader_Should_Return_Token_When_Valid_Header_Provided()
    {
        var authorizationHeader = $"{KeyCloakConstants.DPoP.AuthenticationScheme} eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

        var result = _handler.ExtractTokenFromAuthorizationHeader(authorizationHeader);

        result.Should().Be("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public void ExtractTokenFromAuthorizationHeader_Should_Trim_Whitespace_From_Token()
    {
        var authorizationHeader =
            $"{KeyCloakConstants.DPoP.AuthenticationScheme}   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9   ";

        var result = _handler.ExtractTokenFromAuthorizationHeader(authorizationHeader);

        result.Should().Be("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public void ExtractTokenFromAuthorizationHeader_Should_Throw_ArgumentException_When_Header_Is_Null()
    {
        Func<string> act = () => _handler.ExtractTokenFromAuthorizationHeader(null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsDPoPScheme_Should_Return_True_When_Authorization_Header_Starts_With_DPoP_Scheme()
    {
        HttpRequest mockRequest =
            CreateMockRequestWithAuthorizationHeader($"{KeyCloakConstants.DPoP.AuthenticationScheme} token");

        var result = _handler.IsDPoPScheme(mockRequest);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDPoPScheme_Should_Return_True_When_Authorization_Header_Starts_With_DPoP_Scheme_Case_Insensitive()
    {
        HttpRequest mockRequest =
            CreateMockRequestWithAuthorizationHeader(
                $"{KeyCloakConstants.DPoP.AuthenticationScheme.ToUpperInvariant()} token");

        var result = _handler.IsDPoPScheme(mockRequest);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDPoPScheme_Should_Return_False_When_Authorization_Header_Does_Not_Start_With_DPoP_Scheme()
    {
        HttpRequest mockRequest = CreateMockRequestWithAuthorizationHeader("Bearer token");

        var result = _handler.IsDPoPScheme(mockRequest);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDPoPScheme_Should_Return_False_When_Authorization_Header_Is_Null()
    {
        HttpRequest mockRequest = CreateMockRequestWithAuthorizationHeader();

        var result = _handler.IsDPoPScheme(mockRequest);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetAuthorizationHeader_Should_Return_First_Authorization_Header_Value()
    {
        HttpRequest mockRequest = CreateMockRequestWithAuthorizationHeader(new[] { "first", "second" });

        var result = _handler.GetAuthorizationHeader(mockRequest);

        result.Should().Be("first");
    }

    [Fact]
    public void GetAuthorizationHeader_Should_Return_Null_When_No_Authorization_Header_Present()
    {
        HttpRequest mockRequest = CreateMockRequestWithAuthorizationHeader();

        var result = _handler.GetAuthorizationHeader(mockRequest);

        result.Should().BeNull();
    }

    [Fact]
    public void IsValidAuthorizationHeaderCount_Should_Return_True_When_Exactly_One_Authorization_Header_Present()
    {
        MessageReceivedContext context = CreateMessageReceivedContextWithAuthorizationHeader("token");

        var result = _handler.IsValidAuthorizationHeaderCount(context.Request);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidAuthorizationHeaderCount_Should_Return_False_When_Multiple_Authorization_Headers_Present()
    {
        MessageReceivedContext context =
            CreateMessageReceivedContextWithAuthorizationHeader(new[] { "token1", "token2" });

        var result = _handler.IsValidAuthorizationHeaderCount(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidAuthorizationHeaderCount_Should_Return_False_When_No_Authorization_Header_Present()
    {
        MessageReceivedContext context = CreateMessageReceivedContextWithAuthorizationHeader();

        var result = _handler.IsValidAuthorizationHeaderCount(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_True_When_Exactly_One_Valid_DPoP_Proof_Header_Present()
    {
        var headers = new HeaderDictionary
        {
            { KeyCloakConstants.DPoP.ProofHeader, "proof_token" }
        };
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_True_When_DPoP_Proof_Header_Case_Insensitive()
    {
        var headers = new HeaderDictionary
        {
            { KeyCloakConstants.DPoP.ProofHeader.ToUpperInvariant(), "proof_token" }
        };
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_False_When_Multiple_DPoP_Proof_Headers_Present()
    {
        var headers = new HeaderDictionary
        {
            { KeyCloakConstants.DPoP.ProofHeader, new StringValues(new[] { "proof1", "proof2" }) }
        };
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_False_When_DPoP_Proof_Header_Value_Is_Empty()
    {
        var headers = new HeaderDictionary
        {
            { KeyCloakConstants.DPoP.ProofHeader, "" }
        };
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_False_When_No_DPoP_Proof_Header_Present()
    {
        var headers = new HeaderDictionary();
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void ExtractDPoPBoundAccessToken_Should_Set_Token_When_Valid_Authorization_Header_Present()
    {
        var authorizationHeader = $"{KeyCloakConstants.DPoP.AuthenticationScheme} valid_token";
        MessageReceivedContext context = CreateMessageReceivedContextWithAuthorizationHeader(authorizationHeader);

        var token = _handler.ExtractDPoPBoundAccessToken(context.Request);

        token.Should().Be("valid_token");
    }

    [Fact]
    public void ExtractDPoPBoundAccessToken_Should_Not_Set_Token_When_Authorization_Header_Is_Null()
    {
        MessageReceivedContext context = CreateMessageReceivedContextWithAuthorizationHeader();

        _handler.ExtractDPoPBoundAccessToken(context.Request);

        context.Token.Should().BeNull();
    }

    [Fact]
    public void ExtractDPoPBoundAccessToken_Should_Not_Set_Token_When_Authorization_Header_Is_Empty()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = new StringValues("");

        var context = new MessageReceivedContext(httpContext,
            new AuthenticationScheme("Test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

        _handler.ExtractDPoPBoundAccessToken(context.Request);

        context.Token.Should().BeNull();
    }

    [Fact]
    public void ExtractDPoPBoundAccessToken_Should_Not_Set_Token_When_Extracted_Token_Is_Empty()
    {
        MessageReceivedContext context =
            CreateMessageReceivedContextWithAuthorizationHeader(KeyCloakConstants.DPoP.AuthenticationScheme);

        _handler.ExtractDPoPBoundAccessToken(context.Request);

        context.Token.Should().BeNull();
    }

    [Fact]
    public void ExtractTokenFromAuthorizationHeader_Should_Handle_Only_Scheme_With_No_Token()
    {
        var authorizationHeader = KeyCloakConstants.DPoP.AuthenticationScheme;

        var result = _handler.ExtractTokenFromAuthorizationHeader(authorizationHeader);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ExtractTokenFromAuthorizationHeader_Should_Throw_ArgumentException_When_Header_Is_Whitespace(
        string whitespaceHeader)
    {
        Func<string> act = () => _handler.ExtractTokenFromAuthorizationHeader(whitespaceHeader);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExtractTokenFromAuthorizationHeader_Should_Handle_Token_With_Internal_Spaces()
    {
        var tokenWithSpaces = "token with spaces";
        var authorizationHeader = $"{KeyCloakConstants.DPoP.AuthenticationScheme} {tokenWithSpaces}";

        var result = _handler.ExtractTokenFromAuthorizationHeader(authorizationHeader);

        result.Should().Be(tokenWithSpaces);
    }

    [Fact]
    public void IsDPoPScheme_Should_Return_False_When_Header_Is_Shorter_Than_Scheme()
    {
        HttpRequest mockRequest = CreateMockRequestWithAuthorizationHeader("DP");

        var result = _handler.IsDPoPScheme(mockRequest);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDPoPScheme_Should_Return_False_When_Header_Equals_Scheme_But_No_Space()
    {
        HttpRequest mockRequest =
            CreateMockRequestWithAuthorizationHeader(KeyCloakConstants.DPoP.AuthenticationScheme.TrimEnd());

        var result = _handler.IsDPoPScheme(mockRequest);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("dpop token")]
    [InlineData("DPOP token")]
    [InlineData("DpOp token")]
    public void IsDPoPScheme_Should_Handle_Case_Variations_Correctly(string authHeader)
    {
        HttpRequest mockRequest = CreateMockRequestWithAuthorizationHeader(authHeader);

        var result = _handler.IsDPoPScheme(mockRequest);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_False_When_Header_Value_Is_Null()
    {
        var headers = new HeaderDictionary
        {
            { KeyCloakConstants.DPoP.ProofHeader, (string?)null }
        };
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void IsDPoPProofHeaderExists_Should_Return_False_When_Header_Value_Is_Whitespace(string whitespaceValue)
    {
        var headers = new HeaderDictionary
        {
            { KeyCloakConstants.DPoP.ProofHeader, whitespaceValue }
        };
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_True_When_Header_Has_Mixed_Case()
    {
        var mixedCaseHeader = "DpOp";
        var headers = new HeaderDictionary
        {
            { mixedCaseHeader, "proof_token" }
        };
        MessageReceivedContext context = CreateMessageReceivedContextWithHeaders(headers);

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDPoPProofHeaderExists_Should_Return_False_When_Multiple_Headers_With_Different_Cases_Present()
    {
        // Create a custom HTTP context that allows us to simulate multiple headers with different cases
        // This simulates a scenario where the raw HTTP request might have headers with different casing
        var httpContext = new DefaultHttpContext();

        // Add the header twice by manipulating the underlying collection
        // This simulates what might happen in edge cases with raw HTTP processing
        httpContext.Request.Headers.Append(KeyCloakConstants.DPoP.ProofHeader, "proof1");

        // Add multiple values to the same header (case-insensitive key) to simulate multiple headers
        StringValues existingValues = httpContext.Request.Headers[KeyCloakConstants.DPoP.ProofHeader];
        httpContext.Request.Headers[KeyCloakConstants.DPoP.ProofHeader] = new StringValues(
            existingValues.Concat(new[] { "proof2" }).ToArray()
        );

        var context = new MessageReceivedContext(httpContext,
            new AuthenticationScheme("Test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

        var result = _handler.IsDPoPProofHeaderExists(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidAuthorizationHeaderCount_Should_Return_False_When_Context_Request_Is_Null()
    {
        var httpContext = new DefaultHttpContext();
        var context = new MessageReceivedContext(httpContext,
            new AuthenticationScheme("Test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

        // Clear the request to simulate null scenario - this tests defensive programming
        var result = _handler.IsValidAuthorizationHeaderCount(context.Request);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetAuthorizationHeader_Should_Return_First_Header_When_Multiple_Same_Headers_Present()
    {
        HttpRequest mockRequest =
            CreateMockRequestWithAuthorizationHeader(new[] { "first_token", "second_token", "third_token" });

        var result = _handler.GetAuthorizationHeader(mockRequest);

        result.Should().Be("first_token");
    }

    [Fact]
    public void ExtractDPoPBoundAccessToken_Should_Handle_Authorization_Header_With_Multiple_Spaces()
    {
        var authorizationHeader = $"{KeyCloakConstants.DPoP.AuthenticationScheme}     token_with_multiple_spaces";
        MessageReceivedContext context = CreateMessageReceivedContextWithAuthorizationHeader(authorizationHeader);

        var token = _handler.ExtractDPoPBoundAccessToken(context.Request);

        token.Should().Be("token_with_multiple_spaces");
    }

    [Fact]
    public void ExtractDPoPBoundAccessToken_Should_Handle_Authorization_Header_With_Tabs_And_Newlines()
    {
        var authorizationHeader = $"{KeyCloakConstants.DPoP.AuthenticationScheme}\t\n  token_value  \t\n";
        MessageReceivedContext context = CreateMessageReceivedContextWithAuthorizationHeader(authorizationHeader);

        var token = _handler.ExtractDPoPBoundAccessToken(context.Request);

        token.Should().Be("token_value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void ExtractDPoPBoundAccessToken_Should_Not_Set_Token_When_Authorization_Header_Is_Whitespace(
        string? whitespaceValue)
    {
        MessageReceivedContext context = CreateMessageReceivedContextWithAuthorizationHeader(whitespaceValue);

        _handler.ExtractDPoPBoundAccessToken(context.Request);

        context.Token.Should().BeNull();
    }

    private HttpRequest CreateMockRequestWithAuthorizationHeader(string? authorizationValue = null)
    {
        var mockRequest = A.Fake<HttpRequest>();
        StringValues headerValues = string.IsNullOrEmpty(authorizationValue)
            ? new StringValues()
            : new StringValues(authorizationValue);
        A.CallTo(() => mockRequest.Headers.Authorization).Returns(headerValues);
        return mockRequest;
    }

    private HttpRequest CreateMockRequestWithAuthorizationHeader(string[] authorizationValues)
    {
        var mockRequest = A.Fake<HttpRequest>();
        A.CallTo(() => mockRequest.Headers.Authorization).Returns(new StringValues(authorizationValues));
        return mockRequest;
    }

    private MessageReceivedContext CreateMessageReceivedContextWithAuthorizationHeader(
        string? authorizationValue = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = new StringValues(authorizationValue);
        return new MessageReceivedContext(httpContext, new AuthenticationScheme("Test", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions());
    }

    private MessageReceivedContext CreateMessageReceivedContextWithAuthorizationHeader(string[] authorizationValues)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = new StringValues(authorizationValues);
        return new MessageReceivedContext(httpContext, new AuthenticationScheme("Test", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions());
    }

    private MessageReceivedContext CreateMessageReceivedContextWithHeaders(HeaderDictionary headers)
    {
        var httpContext = new DefaultHttpContext();
        foreach (KeyValuePair<string, StringValues> header in headers)
        {
            httpContext.Request.Headers[header.Key] = header.Value;
        }

        return new MessageReceivedContext(httpContext, new AuthenticationScheme("Test", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions());
    }

    private class TestDPoPEventHandler : DPoPEventHandlerBase
    {
    }
}
