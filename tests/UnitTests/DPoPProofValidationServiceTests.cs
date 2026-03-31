using System.Security.Claims;
using System.Text.Json;

using AspNetCore.Authentication.Api;
using AspNetCore.Authentication.Api.DPoP;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace UnitTests;

public class DPoPProofValidationServiceTests
{
    private readonly DPoPProofValidationService _service = new();

    [Theory]
    [InlineData("http")]
    [InlineData("HTTP")]
    [InlineData("Http")]
    [InlineData("https")]
    [InlineData("HTTPS")]
    [InlineData("Https")]
    public void IsValidScheme_WithValidHttpSchemes_ShouldReturnTrue(string scheme)
    {
        var result = _service.IsValidScheme(scheme);
        result.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(GetInvalidSchemes))]
    public void IsValidScheme_WithInvalidSchemes_ShouldReturnFalse(string scheme)
    {
        var result = _service.IsValidScheme(scheme);
        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(GetMatchingUris))]
    public void CompareUrisStructurally_WhenUrisMatchByStructure_ReturnsTrue(string requestUri, string htuValue)
    {
        var result = _service.CompareUrisStructurally(requestUri, htuValue);
        result.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(GetDifferingUris))]
    public void CompareUrisStructurally_WhenUrisDiffer_ReturnsFalse(string requestUri, string htuValue)
    {
        var result = _service.CompareUrisStructurally(requestUri, htuValue);
        result.Should().BeFalse();
    }

    [Fact]
    public void CompareUrisStructurally_WhenExceptionThrownDuringComparison_ReturnsFalse()
    {
        var invalidUri = new string('a', 65520);

        var result = _service.CompareUrisStructurally(invalidUri, "https://example.com/api");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateHtuClaimValue_WhenRequestUriIsNullOrWhiteSpace_ReturnsFalse(string? requestUri)
    {
        var result = _service.ValidateHtuClaimValue(requestUri, "https://example.com/api");
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateHtuClaimValue_WhenHtuValueIsNullOrWhiteSpace_ReturnsFalse(string? htuValue)
    {
        var result = _service.ValidateHtuClaimValue("https://example.com/api", htuValue);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(123, 123L)]
    [InlineData(456L, 456L)]
    [InlineData(789.0, 789L)]
    [InlineData(789.9, 789L)]
    public void ExtractIssuedAt_Should_Return_Correct_Long_Value_For_Valid_Numeric_Types(object input, long expected)
    {
        var result = _service.ExtractIssuedAt(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("not a number")]
    [InlineData(true)]
    [InlineData(123.456f)]
    public void ExtractIssuedAt_Should_Return_Null_For_Invalid_Types(object input)
    {
        var result = _service.ExtractIssuedAt(input);
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractIssuedAt_Should_Return_Null_For_Null_Input()
    {
        var result = _service.ExtractIssuedAt(null);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(int.MaxValue, (long)int.MaxValue)]
    [InlineData(int.MinValue, (long)int.MinValue)]
    [InlineData(0, 0L)]
    public void ExtractIssuedAt_Should_Handle_Integer_Boundary_Values(int input, long expected)
    {
        var result = _service.ExtractIssuedAt(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(long.MaxValue, long.MaxValue)]
    [InlineData(long.MinValue, long.MinValue)]
    public void ExtractIssuedAt_Should_Handle_Long_Boundary_Values(long input, long expected)
    {
        var result = _service.ExtractIssuedAt(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void ExtractIssuedAt_Should_Handle_Double_Special_Values(double input)
    {
        var result = _service.ExtractIssuedAt(input);
        result.Should().Be((long)input);
    }

    [Theory]
    [InlineData(1234567890.0, 1234567890L)]
    [InlineData(1234567890.1, 1234567890L)]
    [InlineData(1234567890.9, 1234567890L)]
    [InlineData(-1234567890.9, -1234567890L)]
    public void ExtractIssuedAt_Should_Truncate_Double_Values_To_Long(double input, long expected)
    {
        var result = _service.ExtractIssuedAt(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void ValidateIatClaim_Should_Return_False_When_ProofClaims_Is_Null()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult { ProofClaims = null };

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateIatClaim_Should_Return_False_When_Iat_Claim_Is_Missing()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>()
        };

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData(true)]
    public void ValidateIatClaim_Should_Return_False_When_Iat_Value_Is_Invalid(object invalidIat)
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(invalidIat);

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(55)]
    public void ValidateIatClaim_Should_Return_True_When_Iat_Is_Within_Future_Leeway(int secondsInFuture)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var futureIat = now + secondsInFuture;
        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithOffset(leeway: 60);
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(futureIat);

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIatClaim_Should_Return_False_When_Iat_Exceeds_Future_Offset()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var futureIat = now + 120; // 2 minutes in future
        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithOffset(iatOffset: 60);
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(futureIat);

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(295)]
    public void ValidateIatClaim_Should_Return_True_When_Iat_Is_Within_Past_Offset(int secondsInPast)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pastIat = now - secondsInPast;
        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithOffset(iatOffset: 300);
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(pastIat);

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIatClaim_Should_Return_False_When_Iat_Exceeds_Past_Leeway()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pastIat = now - 600; // 10 minutes in past
        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithOffset(leeway: 300);
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(pastIat);

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateIatClaim_Should_Use_Default_Options_When_No_Custom_Values_Provided()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(now);

        var result = _service.ValidateIatClaim(validationParameters, validationResult);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_True_When_Htu_Claim_Exists_And_Matches_Expected_Value()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "https://api.example.com/users" }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Htu_Claim_Does_Not_Exist()
    {
        var proofClaims = new Dictionary<string, object>();
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Htu_Claim_Is_Null()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, null }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Htu_Claim_Is_Not_String()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, 12345 }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Htu_Values_Do_Not_Match()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "https://api.example.com/products" }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_True_When_Htu_Values_Match_Case_Insensitively()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "https://API.EXAMPLE.COM/users" }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_True_When_Htu_Values_Match_Structurally()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "https://api.example.com:443/users" }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Expected_Htu_Is_Null()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "https://api.example.com/users" }
        };

        var result = _service.ValidateHtuClaim(null, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Expected_Htu_Is_Empty()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "https://api.example.com/users" }
        };

        var result = _service.ValidateHtuClaim(string.Empty, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Expected_Htu_Is_Whitespace()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "https://api.example.com/users" }
        };

        var result = _service.ValidateHtuClaim("   ", proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Htu_Claim_Is_Empty_String()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, string.Empty }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtuClaim_Should_Return_False_When_Htu_Claim_Is_Whitespace()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htu, "   " }
        };
        var expectedHtu = "https://api.example.com/users";

        var result = _service.ValidateHtuClaim(expectedHtu, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_True_When_Htm_Matches_Expected_Value()
    {
        var expectedHtm = "POST";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "POST" }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_True_When_Htm_Matches_Expected_Value_Case_Insensitive()
    {
        var expectedHtm = "post";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "POST" }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_False_When_Htm_Claim_Missing()
    {
        var expectedHtm = "POST";
        var proofClaims = new Dictionary<string, object>();

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_False_When_Htm_Value_Is_Null()
    {
        var expectedHtm = "POST";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, null }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_False_When_Htm_Value_Is_Not_String()
    {
        var expectedHtm = "POST";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, 123 }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_False_When_Htm_Values_Do_Not_Match()
    {
        var expectedHtm = "POST";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "GET" }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("GET", "get")]
    [InlineData("POST", "post")]
    [InlineData("PUT", "put")]
    [InlineData("DELETE", "delete")]
    [InlineData("PATCH", "patch")]
    public void ValidateHtmClaim_Should_Handle_All_Http_Methods_Case_Insensitive(string expectedHtm, string actualHtm)
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, actualHtm }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_False_When_Expected_Htm_Is_Empty_String()
    {
        var expectedHtm = "";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "POST" }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_False_When_Htm_Claim_Is_Empty_String()
    {
        var expectedHtm = "POST";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "" }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHtmClaim_Should_Return_False_When_Htm_Claim_Is_Whitespace()
    {
        var expectedHtm = "POST";
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "   " }
        };

        var result = _service.ValidateHtmClaim(expectedHtm, proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJtiClaim_should_return_true_when_jti_is_valid_string()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Jti, "valid-jti-value" }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateJtiClaim_should_return_false_when_jti_is_not_string()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Jti, 12345 }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJtiClaim_should_return_false_when_jti_is_object()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Jti, new { value = "test" } }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJtiClaim_should_return_true_when_jti_has_valid_guid_format()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Jti, Guid.NewGuid().ToString() }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateJtiClaim_should_return_true_when_jti_has_special_characters()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Jti, "jti-with-special-chars_123.456" }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateJtiClaim_should_return_false_when_jti_contains_invalid_values(
        string? invalidValue)
    {
        var proofClaims = new Dictionary<string, object?>
        {
            { KeyCloakConstants.DPoP.Jti, invalidValue }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJtiClaim_should_return_false_when_jti_value_is_missing()
    {
        var proofClaims = new Dictionary<string, object>();

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateDPoPPayloadAsync_ShouldSetError_WhenJtiIsReplayed()
    {
        var replayingCache = A.Fake<IDPoPJtiCache>();
        A.CallTo(() => replayingCache.TryAddAsync(A<string>._, A<TimeSpan>._, A<CancellationToken>._))
            .Returns(false); // every jti is a replay

        var service = new DPoPProofValidationService(replayingCache);
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, service.ComputeAccessTokenHash("dummy-access-token") },
                { KeyCloakConstants.DPoP.Jti, "replay-jti" },
                { KeyCloakConstants.DPoP.Htm, "GET" },
                { KeyCloakConstants.DPoP.Htu, "https://example.com/api" },
                { KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            }
        };

        await service.ValidateDPoPPayloadAsync(parameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
    }

    [Fact]
    public async Task ValidateDPoPPayloadAsync_ShouldNotSetError_WhenJtiIsFirstSeen()
    {
        var freshCache = A.Fake<IDPoPJtiCache>();
        A.CallTo(() => freshCache.TryAddAsync(A<string>._, A<TimeSpan>._, A<CancellationToken>._))
            .Returns(true); // every jti is new

        var service = new DPoPProofValidationService(freshCache);
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, service.ComputeAccessTokenHash("dummy-access-token") },
                { KeyCloakConstants.DPoP.Jti, "fresh-jti" },
                { KeyCloakConstants.DPoP.Htm, "GET" },
                { KeyCloakConstants.DPoP.Htu, "https://example.com/api" },
                { KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            }
        };

        await service.ValidateDPoPPayloadAsync(parameters, validationResult);

        validationResult.HasError.Should().BeFalse();
    }

    [Fact]
    public void ComputeAccessTokenHash_ValidAccessToken_ReturnsBase64UrlEncodedSha256Hash()
    {
        var accessToken = "valid.jwt.token";

        var result = _service.ComputeAccessTokenHash(accessToken);

        result.Should().NotBeNullOrEmpty();
        result.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void ComputeAccessTokenHash_SameTokenTwice_ReturnsSameHash()
    {
        var accessToken = "test.token.value";

        var firstHash = _service.ComputeAccessTokenHash(accessToken);
        var secondHash = _service.ComputeAccessTokenHash(accessToken);

        firstHash.Should().Be(secondHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("test.token")]
    [InlineData("eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9")]
    public void ComputeAccessTokenHash_VariousInputs_ProducesConsistentLength(string accessToken)
    {
        var result = _service.ComputeAccessTokenHash(accessToken);

        result.Should().HaveLength(43);
    }

    [Fact]
    public void TryValidateThumbprintBinding_Should_Return_True_When_Thumbprints_Match()
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { KeyCloakConstants.DPoP.JwkThumbprint, JsonSerializer.SerializeToElement("expected_thumbprint") }
        };
        var expectedThumbprint = "expected_thumbprint";

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryValidateThumbprintBinding_Should_Return_False_When_Thumbprints_Do_Not_Match()
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { KeyCloakConstants.DPoP.JwkThumbprint, JsonSerializer.SerializeToElement("actual_thumbprint") }
        };
        var expectedThumbprint = "expected_thumbprint";

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidateThumbprintBinding_Should_Return_False_When_JwkThumbprint_Key_Missing()
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { "other_key", JsonSerializer.SerializeToElement("some_value") }
        };
        var expectedThumbprint = "expected_thumbprint";

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidateThumbprintBinding_Should_Return_False_When_CnfJson_Is_Empty()
    {
        var cnfJson = new Dictionary<string, JsonElement>();
        var expectedThumbprint = "expected_thumbprint";

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TryValidateThumbprintBinding_Should_Return_False_When_Expected_Thumbprint_Is_Null_Or_Whitespace(
        string? expectedThumbprint)
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { KeyCloakConstants.DPoP.JwkThumbprint, JsonSerializer.SerializeToElement("actual_thumbprint") }
        };

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeFalse();
    }

    [Fact]
    public void
        TryValidateThumbprintBinding_Should_Return_True_When_JsonElement_Contains_Empty_String_And_Expected_Is_Empty()
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { KeyCloakConstants.DPoP.JwkThumbprint, JsonSerializer.SerializeToElement("") }
        };
        var expectedThumbprint = "";

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryValidateThumbprintBinding_Should_Handle_JsonElement_With_Null_Value()
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { KeyCloakConstants.DPoP.JwkThumbprint, JsonSerializer.SerializeToElement((string)null) }
        };
        var expectedThumbprint = "expected_thumbprint";

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidateThumbprintBinding_Should_Be_Case_Sensitive()
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { KeyCloakConstants.DPoP.JwkThumbprint, JsonSerializer.SerializeToElement("Expected_Thumbprint") }
        };
        var expectedThumbprint = "expected_thumbprint";

        var result = _service.TryValidateThumbprintBinding(cnfJson, expectedThumbprint);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAccessTokenHash_should_return_false_when_access_token_is_null_empty_or_whitespace(
        string? accessToken)
    {
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResult();

        var result = _service.ValidateAccessTokenHash(accessToken, validationResult);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessTokenHash_should_return_false_when_proof_claims_is_null()
    {
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = null
        };

        var result = _service.ValidateAccessTokenHash("valid-token", validationResult);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateAccessTokenHash_should_return_false_when_ath_claim_is_missing_null_or_empty(object? athValue)
    {
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResult();

        if (athValue is null)
        {
            validationResult.ProofClaims = new Dictionary<string, object>();
        }
        else
        {
            validationResult.ProofClaims = new Dictionary<string, object?>
            {
                { KeyCloakConstants.DPoP.Ath, athValue }
            };
        }

        var result = _service.ValidateAccessTokenHash("valid-token", validationResult);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessTokenHash_should_return_true_when_computed_hash_matches_ath_claim()
    {
        const string accessToken = "test-access-token";
        var expectedHash = _service.ComputeAccessTokenHash(accessToken);
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResult();
        validationResult.ProofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Ath, expectedHash }
        };

        var result = _service.ValidateAccessTokenHash(accessToken, validationResult);

        result.Should().BeTrue();
        validationResult.AccessTokenHash.Should().Be(expectedHash);
    }

    [Fact]
    public void ValidateAccessTokenHash_should_return_false_when_computed_hash_does_not_match_ath_claim()
    {
        const string accessToken = "test-access-token";
        const string incorrectHash = "incorrect-hash-value";
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResult();
        validationResult.ProofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Ath, incorrectHash }
        };

        var result = _service.ValidateAccessTokenHash(accessToken, validationResult);

        result.Should().BeFalse();
        validationResult.AccessTokenHash.Should().Be(incorrectHash);
    }

    [Fact]
    public void ValidateAccessTokenHash_should_set_access_token_hash_property_from_ath_claim()
    {
        const string accessToken = "test-access-token";
        const string athValue = "test-ath-value";
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResult();
        validationResult.ProofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Ath, athValue }
        };

        _service.ValidateAccessTokenHash(accessToken, validationResult);

        validationResult.AccessTokenHash.Should().Be(athValue);
    }

    [Theory]
    [InlineData(123)]
    [InlineData(123.456)]
    [InlineData(true)]
    public void ValidateAccessTokenHash_should_convert_non_string_ath_claim_to_string(object athValue)
    {
        const string accessToken = "test-access-token";
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResult();
        validationResult.ProofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Ath, athValue }
        };

        _service.ValidateAccessTokenHash(accessToken, validationResult);

        validationResult.AccessTokenHash.Should().Be(athValue.ToString());
    }

    [Fact]
    public void TryParseCnfClaim_ValidJsonObject_ReturnsTrueAndParsedDictionary()
    {
        var validJsonObject = """{"jkt":"abc123","other":"value"}""";

        var result = _service.TryParseCnfClaim(validJsonObject, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeTrue();
        cnfJson.Should().NotBeNull();
        cnfJson.Should().ContainKey("jkt");
        cnfJson!["jkt"].GetString().Should().Be("abc123");
        cnfJson.Should().ContainKey("other");
        cnfJson["other"].GetString().Should().Be("value");
    }

    [Fact]
    public void TryParseCnfClaim_ValidEmptyJsonObject_ReturnsTrueAndEmptyDictionary()
    {
        var emptyJsonObject = "{}";

        var result = _service.TryParseCnfClaim(emptyJsonObject, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeTrue();
        cnfJson.Should().NotBeNull();
        cnfJson.Should().BeEmpty();
    }

    [Fact]
    public void TryParseCnfClaim_ValidJsonWithNestedObjects_ReturnsTrueAndParsedDictionary()
    {
        var nestedJsonObject = """{"jkt":"abc123","nested":{"key":"value"},"array":[1,2,3]}""";

        var result = _service.TryParseCnfClaim(nestedJsonObject, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeTrue();
        cnfJson.Should().NotBeNull();
        cnfJson.Should().ContainKey("jkt");
        cnfJson.Should().ContainKey("nested");
        cnfJson.Should().ContainKey("array");
        cnfJson!["jkt"].GetString().Should().Be("abc123");
        cnfJson["nested"].ValueKind.Should().Be(JsonValueKind.Object);
        cnfJson["array"].ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void TryParseCnfClaim_InvalidJson_ReturnsFalseAndNullDictionary()
    {
        var invalidJson = """{"jkt":"abc123",}""";

        var result = _service.TryParseCnfClaim(invalidJson, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeFalse();
        cnfJson.Should().BeNull();
    }

    [Fact]
    public void TryParseCnfClaim_MalformedJson_ReturnsFalseAndNullDictionary()
    {
        var malformedJson = """{"jkt":"abc123""";

        var result = _service.TryParseCnfClaim(malformedJson, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeFalse();
        cnfJson.Should().BeNull();
    }

    [Fact]
    public void TryParseCnfClaim_NonJsonString_ReturnsFalseAndNullDictionary()
    {
        var nonJsonString = "not json at all";

        var result = _service.TryParseCnfClaim(nonJsonString, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeFalse();
        cnfJson.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void TryParseCnfClaim_WhitespaceOrEmptyString_ReturnsFalseAndNullDictionary(string input)
    {
        var result = _service.TryParseCnfClaim(input, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeFalse();
        cnfJson.Should().BeNull();
    }

    [Fact]
    public void TryParseCnfClaim_JsonArray_ReturnsFalseAndNullDictionary()
    {
        var jsonArray = """["jkt","abc123"]""";

        var result = _service.TryParseCnfClaim(jsonArray, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeFalse();
        cnfJson.Should().BeNull();
    }

    [Fact]
    public void TryParseCnfClaim_JsonPrimitive_ReturnsFalseAndNullDictionary()
    {
        var jsonPrimitive = "\"just a string\"";

        var result = _service.TryParseCnfClaim(jsonPrimitive, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeFalse();
        cnfJson.Should().BeNull();
    }

    [Fact]
    public void TryParseCnfClaim_JsonNull_ReturnsFalseAndNullDictionary()
    {
        var jsonNull = "null";

        var result = _service.TryParseCnfClaim(jsonNull, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeFalse();
        cnfJson.Should().BeNull();
    }

    [Fact]
    public void TryParseCnfClaim_ValidJsonWithSpecialCharacters_ReturnsTrueAndParsedDictionary()
    {
        var jsonWithSpecialChars = """{"jkt":"abc/+123=","special":"value with spaces & symbols!@#$%"}""";

        var result = _service.TryParseCnfClaim(jsonWithSpecialChars, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeTrue();
        cnfJson.Should().NotBeNull();
        cnfJson!["jkt"].GetString().Should().Be("abc/+123=");
        cnfJson["special"].GetString().Should().Be("value with spaces & symbols!@#$%");
    }

    [Fact]
    public void TryParseCnfClaim_ValidJsonWithEscapedCharacters_ReturnsTrueAndParsedDictionary()
    {
        var jsonWithEscapedChars = """{"jkt":"abc\"123","newline":"line1\nline2"}""";

        var result = _service.TryParseCnfClaim(jsonWithEscapedChars, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeTrue();
        cnfJson.Should().NotBeNull();
        cnfJson!["jkt"].GetString().Should().Be("abc\"123");
        cnfJson["newline"].GetString().Should().Be("line1\nline2");
    }

    [Fact]
    public async Task ValidateTokenSignature_Should_Not_Set_ProofClaims_When_TokenValidationResult_Has_Exception()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithJsonWebKey();
        var tokenValidationResult = new TokenValidationResult
        {
            Exception = new SecurityTokenValidationException("Invalid token")
        };

        A.CallTo(() => mockTokenHandler.ValidateTokenAsync(validationParameters.ProofToken, A<TokenValidationParameters>._))
            .Returns(tokenValidationResult);

        await service.ValidateTokenSignature(validationParameters, validationResult);

        validationResult.ProofClaims.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenSignature_Should_Not_Set_ProofClaims_When_TokenValidationResult_Is_Null()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithJsonWebKey();

        A.CallTo(() => mockTokenHandler.ValidateTokenAsync(validationParameters.ProofToken, A<TokenValidationParameters>._))
            .Returns((TokenValidationResult?)null);

        await service.ValidateTokenSignature(validationParameters, validationResult);

        validationResult.ProofClaims.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenSignature_Should_Not_Throw_When_TokenHandler_Throws_Exception()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithJsonWebKey();

        A.CallTo(() => mockTokenHandler.ValidateTokenAsync(validationParameters.ProofToken, A<TokenValidationParameters>._))
            .Returns(Task.FromException<TokenValidationResult>(new SecurityTokenException("Token validation failed")));

        Func<Task> act = async () => await service.ValidateTokenSignature(validationParameters, validationResult);

        await act.Should().NotThrowAsync();
        validationResult.ProofClaims.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenSignature_Should_Not_Throw_When_JsonWebKey_Creation_Throws_Exception()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            JsonWebKey = "invalid-jwk-json"
        };

        Func<Task> act = async () => await service.ValidateTokenSignature(validationParameters, validationResult);

        await act.Should().NotThrowAsync();
        validationResult.ProofClaims.Should().BeNull();
    }


    [Fact]
    public void TryCreateJsonWebKey_WithValidJwkJson_CreatesCorrectJsonWebKey()
    {
        var validJwkJson = """{"kty":"EC","crv":"P-256","x":"test","y":"test"}""";

        var result = _service.TryCreateJsonWebKey(validJwkJson, out JsonWebKey? jwk);

        result.Should().BeTrue();
        jwk.Should().NotBeNull();
        jwk!.Kty.Should().Be("EC");
        jwk.Crv.Should().Be("P-256");
    }

    [Theory]
    [InlineData("", "empty string")]
    [InlineData("   ", "whitespace")]
    [InlineData("{invalid json}", "invalid JSON")]
    [InlineData(null, "null string")]
    public void TryCreateJsonWebKey_WithInvalidInput_ReturnsFalse(string jwkJson, string scenario)
    {
        var result = _service.TryCreateJsonWebKey(jwkJson, out JsonWebKey? jwk);

        result.Should().BeFalse($"because {scenario} should not create a valid JWK");
        jwk.Should().BeNull($"because {scenario} should not produce a JWK instance");
    }

    [Fact]
    public void TryCreateJsonWebKey_WithExtraProperties_ReturnsTrue()
    {
        var jwkJsonWithExtras = """{"kty":"RSA","n":"test","e":"AQAB","use":"sig","alg":"RS256"}""";

        var result = _service.TryCreateJsonWebKey(jwkJsonWithExtras, out JsonWebKey? jwk);

        result.Should().BeTrue();
        jwk.Should().NotBeNull();
        jwk!.Use.Should().Be("sig");
        jwk.Alg.Should().Be("RS256");
    }

    [Fact]
    public void TryExtractJsonWebKey_WhenTokenIsNull_ReturnsFalseAndEmptyJwkJson()
    {
        var result = _service.TryExtractJsonWebKey(null, out string jwkJson);

        result.Should().BeFalse();
        jwkJson.Should().BeEmpty();
    }

    [Fact]
    public void TryExtractJsonWebKey_WhenTokenDoesNotContainJwkHeader_ReturnsFalseAndEmptyJwkJson()
    {
        JsonWebToken tokenWithoutJwk = TestUtilities.CreateTokenWithoutJwkHeader();

        var result = _service.TryExtractJsonWebKey(tokenWithoutJwk, out var jwkJson);

        result.Should().BeFalse();
        jwkJson.Should().BeEmpty();
    }

    [Fact]
    public void TryExtractJsonWebKey_WhenTokenContainsValidJwkHeader_ReturnsTrueAndSerializedJwk()
    {
        JsonWebToken tokenWithJwk = TestUtilities.CreateTokenWithJwkHeader();

        var result = _service.TryExtractJsonWebKey(tokenWithJwk, out var jwkJson);

        result.Should().BeTrue();
        jwkJson.Should().NotBeEmpty();
        jwkJson.Should().Contain("RSA");
        jwkJson.Should().Contain("sig");
    }

    [Fact]
    public void TryExtractJsonWebKey_WhenTokenContainsComplexJwkObject_ReturnsTrueAndCorrectSerialization()
    {
        JsonWebToken tokenWithComplexJwk = TestUtilities.CreateTokenWithComplexJwkHeader();

        var result = _service.TryExtractJsonWebKey(tokenWithComplexJwk, out var jwkJson);

        result.Should().BeTrue();
        jwkJson.Should().Contain("RSA");
        jwkJson.Should().Contain("test-key-id");
        jwkJson.Should().Contain("sample-modulus");
        jwkJson.Should().Contain("AQAB");
    }

    [Fact]
    public void TryExtractJsonWebKey_WhenTokenContainsEmptyJwkObject_ReturnsTrueAndSerializesEmptyObject()
    {
        JsonWebToken tokenWithEmptyJwk = TestUtilities.CreateTokenWithEmptyJwkHeader();

        var result = _service.TryExtractJsonWebKey(tokenWithEmptyJwk, out var jwkJson);

        result.Should().BeTrue();
        jwkJson.Should().NotBeEmpty();
        jwkJson.Should().Be("{}");
    }

    [Fact]
    public void TryParseProofToken_ValidToken_ReturnsTrueAndParsedToken()
    {
        var validToken = TestUtilities.CreateValidJwtToken();

        var result = _service.TryParseProofToken(validToken, out JsonWebToken? token);

        result.Should().BeTrue();
        token.Should().NotBeNull();
        token.EncodedToken.Should().Be(validToken);
    }

    [Theory]
    [InlineData(null, "null token")]
    [InlineData("", "empty token")]
    [InlineData("   ", "whitespace token")]
    [InlineData("invalid.jwt.format", "invalid JWT format")]
    [InlineData("header.payload", "malformed JWT structure")]
    [InlineData("invalid@base64.invalid@base64.signature", "invalid Base64 encoding")]
    public void TryParseProofToken_InvalidInputs_ReturnsFalseAndNullToken(string? invalidToken, string scenario)
    {
        var result = _service.TryParseProofToken(invalidToken, out JsonWebToken? token);

        result.Should().BeFalse($"because {scenario} should not parse successfully");
        token.Should().BeNull($"because {scenario} should not produce a token instance");
    }

    [Fact]
    public void TryParseProofToken_TokenHandlerThrowsException_ReturnsFalseAndNullToken()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        A.CallTo(() => mockTokenHandler.ReadJsonWebToken(A<string>._))
            .Throws(new ArgumentException("Invalid token"));

        _service.TokenHandler = mockTokenHandler;

        var result = _service.TryParseProofToken("any.token.here", out JsonWebToken? token);

        result.Should().BeFalse();
        token.Should().BeNull();
    }

    [Fact]
    public void TryParseProofToken_JsonExceptionFromTokenHandler_ReturnsFalseAndNullToken()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        A.CallTo(() => mockTokenHandler.ReadJsonWebToken(A<string>._))
            .Throws(new JsonException("Invalid JSON"));

        _service.TokenHandler = mockTokenHandler;

        var result = _service.TryParseProofToken("invalid.json.token", out JsonWebToken? token);

        result.Should().BeFalse();
        token.Should().BeNull();
    }

    [Fact]
    public void TryParseProofToken_SecurityTokenMalformedExceptionFromTokenHandler_ReturnsFalseAndNullToken()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        A.CallTo(() => mockTokenHandler.ReadJsonWebToken(A<string>._))
            .Throws(new SecurityTokenMalformedException("Malformed token"));

        _service.TokenHandler = mockTokenHandler;

        var result = _service.TryParseProofToken("malformed.token.here", out JsonWebToken? token);

        result.Should().BeFalse();
        token.Should().BeNull();
    }

    [Fact]
    public async Task ValidateDPoPPayload_Sets_Error_When_ProofClaims_Is_Null_Or_Invalid()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = null
        };

        await _service.ValidateDPoPPayloadAsync(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofValidationFailure);
    }

    [Fact]
    public async Task ValidateDPoPPayload_Sets_Error_When_AccessTokenHash_Does_Not_Match()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, "invalidHash" },
                { KeyCloakConstants.DPoP.Jti, "jti" },
                { KeyCloakConstants.DPoP.Htm, "GET" },
                { KeyCloakConstants.DPoP.Htu, "https://api.example.com/resource" },
                { KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            }
        };

        await _service.ValidateDPoPPayloadAsync(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofValidationFailure);
    }

    [Fact]
    public async Task ValidateDPoPPayload_sets_error_when_JtiClaim_is_missing()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, _service.ComputeAccessTokenHash("dummy-access-token") },
                { KeyCloakConstants.DPoP.Htm, "GET" },
                { KeyCloakConstants.DPoP.Htu, "https://api.example.com/resource" },
                { KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            }
        };

        await _service.ValidateDPoPPayloadAsync(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofValidationFailure);
    }

    [Fact]
    public async Task ValidateDPoPPayload_Sets_Error_hen_HtmClaim_Does_Not_Match()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, _service.ComputeAccessTokenHash("dummy-access-token") },
                { KeyCloakConstants.DPoP.Jti, "jti" },
                { KeyCloakConstants.DPoP.Htm, "POST" },
                { KeyCloakConstants.DPoP.Htu, "https://api.example.com/resource" },
                { KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            }
        };

        await _service.ValidateDPoPPayloadAsync(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofValidationFailure);
    }

    [Fact]
    public async Task ValidateDPoPPayload_Sets_Error_When_HtuClaim_Does_Not_Match()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, _service.ComputeAccessTokenHash("dummy-access-token") },
                { KeyCloakConstants.DPoP.Jti, "jti" },
                { KeyCloakConstants.DPoP.Htm, "GET" },
                { KeyCloakConstants.DPoP.Htu, "https://api.example.com/resource" },
                { KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            }
        };

        await _service.ValidateDPoPPayloadAsync(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofValidationFailure);
    }

    [Fact]
    public async Task ValidateDPoPPayload_sets_error_when_IatClaim_is_invalid()
    {
        DPoPProofValidationParameters validationParameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, _service.ComputeAccessTokenHash("dummy-access-token") },
                { KeyCloakConstants.DPoP.Jti, "jti" },
                { KeyCloakConstants.DPoP.Htm, "GET" },
                { KeyCloakConstants.DPoP.Htu, "https://example.com/api" },
                { KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds() }
            }
        };

        await _service.ValidateDPoPPayloadAsync(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofValidationFailure);
    }

    [Fact]
    public async Task ValidateDPoPPayload_succeeds_with_valid_claims()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validationParameters = new DPoPProofValidationParameters
        {
            AccessToken = "accessToken",
            Htm = "GET",
            Htu = "https://api.example.com/resource",
            Options = new DPoPOptions()
            {
                IatOffset = 60,
                Leeway = 60
            },
            ProofToken = "ProofToken"
        };
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, _service.ComputeAccessTokenHash("accessToken") },
                { KeyCloakConstants.DPoP.Jti, "jti" },
                { KeyCloakConstants.DPoP.Htm, "GET" },
                { KeyCloakConstants.DPoP.Htu, "https://api.example.com/resource" },
                { KeyCloakConstants.DPoP.Iat, now }
            }
        };

        await _service.ValidateDPoPPayloadAsync(validationParameters, validationResult);

        validationResult.HasError.Should().BeFalse();
    }

    [Fact]
    public void ValidateCnf_sets_error_when_AccessTokenClaims_is_null()
    {
        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithAccessTokenClaim();
        var validationResult = new DPoPProofValidationResult();

        _service.ValidateCnf(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.CnfClaimMissing);
    }

    [Fact]
    public void ValidateCnf_sets_error_when_cnf_claim_value_is_empty_string()
    {
        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithAccessTokenClaim(new List<Claim>()
            {
                new Claim(KeyCloakConstants.DPoP.Cnf, "")
            });

        var validationResult = new DPoPProofValidationResult();

        _service.ValidateCnf(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.CnfClaimMissing);
    }

    [Fact]
    public void ValidateCnf_sets_error_when_cnf_claim_value_is_invalid_json()
    {
        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithAccessTokenClaim(new List<Claim>
            {
                new Claim(KeyCloakConstants.DPoP.Cnf, "not a json")
            });

        var validationResult = new DPoPProofValidationResult();

        _service.ValidateCnf(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.InvalidCnfClaim);
    }

    [Fact]
    public void ValidateCnf_sets_error_when_thumbprint_does_not_match()
    {
        var cnfValue = "{\"jkt\":\"wrong-thumbprint\"}";

        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithAccessTokenClaim(new List<Claim>
            {
                new Claim(KeyCloakConstants.DPoP.Cnf, cnfValue)
            });

        var validationResult = new DPoPProofValidationResult
        {
            JsonWebKeyThumbprint = "expected-thumbprint"
        };

        _service.ValidateCnf(validationParameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.InvalidSignature);
    }

    [Fact]
    public void ValidateCnf_does_not_set_error_when_thumbprint_matches()
    {
        var cnfValue = "{\"jkt\":\"expected-thumbprint\"}";

        DPoPProofValidationParameters validationParameters =
            TestUtilities.CreateValidationParametersWithAccessTokenClaim(new List<Claim>
            {
                new(KeyCloakConstants.DPoP.Cnf, cnfValue)
            });
        var validationResult = new DPoPProofValidationResult
        {
            JsonWebKeyThumbprint = "expected-thumbprint"
        };

        _service.ValidateCnf(validationParameters, validationResult);

        validationResult.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateDPoPHeaderTokenAsync_InvalidProofToken_SetsError()
    {
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParametersWithInvalidProof();
        var result = new DPoPProofValidationResult();

        await _service.ValidateDPoPHeaderTokenAsync(parameters, result);

        result.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
    }

    [Fact]
    public async Task ValidateDPoPHeaderTokenAsync_MissingJwkInHeader_SetsError()
    {
        DPoPProofValidationParameters parameters = TestUtilities.CreateDPoPProofValidationParametersWithMissingJwk();
        var result = new DPoPProofValidationResult();

        await _service.ValidateDPoPHeaderTokenAsync(parameters, result);

        result.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
    }

    [Fact]
    public async Task ValidateDPoPHeaderTokenAsync_InvalidJwkJson_SetsError()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);
        JsonWebToken tokenWithInvalidJwk = TestUtilities.CreateTokenWithInvalidJwkJson();

        A.CallTo(() => mockTokenHandler.ReadJsonWebToken(A<string>._)).Returns(tokenWithInvalidJwk);

        DPoPProofValidationParameters
            parameters = TestUtilities.CreateDPoPProofValidationParametersWithInvalidJwkJson();
        var result = new DPoPProofValidationResult();

        await service.ValidateDPoPHeaderTokenAsync(parameters, result);

        result.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
    }

    [Fact]
    public async Task ValidateDPoPHeaderTokenAsync_SetsJsonWebKeyAndThumbprint_WhenValid()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);
        JsonWebToken tokenWithValidJwk = TestUtilities.CreateTokenWithComplexJwkHeader();

        A.CallTo(() => mockTokenHandler.ReadJsonWebToken(A<string>._)).Returns(tokenWithValidJwk);
        A.CallTo(() => mockTokenHandler.ValidateTokenAsync(A<string>._, A<TokenValidationParameters>._))
            .Returns(new TokenValidationResult
            {
                IsValid = true,
                ClaimsIdentity = new ClaimsIdentity()
            });

        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        var result = new DPoPProofValidationResult();

        await service.ValidateDPoPHeaderTokenAsync(parameters, result);

        result.HasError.Should().BeFalse();
        result.JsonWebKey.Should().NotBeNullOrEmpty();
        result.JsonWebKeyThumbprint.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateDPoPHeaderTokenAsync_CallsValidateTokenSignature()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);
        JsonWebToken tokenWithValidJwk = TestUtilities.CreateTokenWithComplexJwkHeader();

        A.CallTo(() => mockTokenHandler.ReadJsonWebToken(A<string>._)).Returns(tokenWithValidJwk);

        var validateTokenAsyncCalled = false;
        A.CallTo(() => mockTokenHandler.ValidateTokenAsync(A<string>._, A<TokenValidationParameters>._))
            .Invokes(() => validateTokenAsyncCalled = true)
            .Returns(new TokenValidationResult
            {
                IsValid = true,
                ClaimsIdentity = new ClaimsIdentity()
            });

        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        var result = new DPoPProofValidationResult();

        await service.ValidateDPoPHeaderTokenAsync(parameters, result);

        validateTokenAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public void ValidateDPoPHeaderTokenAsync_SetsError_WhenJwkHasPrivateKey()
    {
        var jwkWithPrivateKey = TestUtilities.CreateValidRsaJwkWithPrivateKey();

        var result = _service.TryCreateJsonWebKey(jwkWithPrivateKey, out JsonWebKey? jwk);

        result.Should().BeTrue();
        jwk.Should().NotBeNull();
        jwk!.HasPrivateKey.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ThrowsArgumentNullException_When_ValidationParameters_Is_Null()
    {
        Func<Task> act = async () => await _service.ValidateAsync(null);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateAsync_Returns_Error_When_ProofToken_Is_Null()
    {
        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = null,
            AccessToken = "valid.access.token",
            Options = new DPoPOptions(),
            Htm = "GET",
            Htu = "https://example.com/api"
        };

        DPoPProofValidationResult? result = await _service.ValidateAsync(parameters);

        result.Should().NotBeNull();
        result!.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        result.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofMissing);
    }

    [Fact]
    public async Task ValidateAsync_Returns_Error_When_ProofToken_Is_Empty()
    {
        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = string.Empty,
            AccessToken = "valid.access.token",
            Options = new DPoPOptions(),
            Htm = "GET",
            Htu = "https://example.com/api"
        };

        DPoPProofValidationResult? result = await _service.ValidateAsync(parameters);

        result.Should().NotBeNull();
        result!.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        result.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofMissing);
    }

    [Fact]
    public async Task ValidateAsync_Returns_Error_When_ProofToken_Is_Whitespace()
    {
        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = "   ",
            AccessToken = "valid.access.token",
            Options = new DPoPOptions(),
            Htm = "GET",
            Htu = "https://example.com/api"
        };

        DPoPProofValidationResult? result = await _service.ValidateAsync(parameters);

        result.Should().NotBeNull();
        result!.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        result.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.DPoPProofMissing);
    }

    [Fact]
    public async Task ValidateAsync_Returns_Error_When_AccessToken_Is_Null()
    {
        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = "valid.proof.token",
            AccessToken = null,
            Options = new DPoPOptions(),
            Htm = "GET",
            Htu = "https://example.com/api"
        };

        DPoPProofValidationResult? result = await _service.ValidateAsync(parameters);

        result.Should().NotBeNull();
        result!.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        result.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.AccessTokenMissing);
    }

    [Fact]
    public async Task ValidateAsync_Returns_Error_When_AccessToken_Is_Empty()
    {
        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = "valid.proof.token",
            AccessToken = string.Empty,
            Options = new DPoPOptions(),
            Htm = "GET",
            Htu = "https://example.com/api"
        };

        DPoPProofValidationResult? result = await _service.ValidateAsync(parameters);

        result.Should().NotBeNull();
        result!.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        result.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.AccessTokenMissing);
    }

    [Fact]
    public async Task ValidateAsync_Returns_Error_When_AccessToken_Is_Whitespace()
    {
        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = "valid.proof.token",
            AccessToken = "   ",
            Options = new DPoPOptions(),
            Htm = "GET",
            Htu = "https://example.com/api"
        };

        DPoPProofValidationResult? result = await _service.ValidateAsync(parameters);

        result.Should().NotBeNull();
        result!.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidRequest);
        result.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.AccessTokenMissing);
    }

    [Fact]
    public async Task ValidateAsync_Returns_Early_When_ValidateDPoPHeaderTokenAsync_Sets_Error()
    {
        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = "invalid.proof.token",
            AccessToken = "valid.access.token",
            Options = new DPoPOptions(),
            Htm = "GET",
            Htu = "https://example.com/api"
        };

        DPoPProofValidationResult? result = await _service.ValidateAsync(parameters);

        result.Should().NotBeNull();
        result!.HasError.Should().BeTrue();
        result.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
    }

    [Fact]
    public async Task ValidateAsync_Honors_CancellationToken()
    {
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Func<Task> act = async () => await _service.ValidateAsync(parameters, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ValidateCnf_Sets_Error_When_AccessTokenClaims_List_Is_Empty()
    {
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParametersWithAccessTokenClaim(
            new List<Claim>());
        var validationResult = new DPoPProofValidationResult
        {
            JsonWebKeyThumbprint = "test-thumbprint"
        };

        _service.ValidateCnf(parameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidToken);
        validationResult.ErrorDescription.Should().Be(KeyCloakConstants.DPoP.Error.Description.CnfClaimMissing);
    }

    [Fact]
    public async Task ValidateDPoPPayload_Sets_Error_When_ProofClaims_Is_Empty()
    {
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>()
        };

        await _service.ValidateDPoPPayloadAsync(parameters, validationResult);

        validationResult.HasError.Should().BeTrue();
        validationResult.Error.Should().Be(KeyCloakConstants.DPoP.Error.Code.InvalidDPoPProof);
    }


    public static IEnumerable<object[]> GetInvalidSchemes()
    {
        // Other protocols
        yield return ["ftp"];
        yield return ["file"];
        yield return ["ws"];
        yield return ["wss"];
        yield return ["mailto"];
        yield return ["tel"];
        yield return ["data"];
        yield return ["javascript"];
        yield return ["custom"];

        // Null and whitespace
        yield return [""];
        yield return [" "];
        yield return ["\t"];
        yield return ["\n"];
        yield return [null!];

        // Invalid http/https formats
        yield return ["http://"];
        yield return ["https://"];
        yield return ["http://example.com"];
        yield return ["https://example.com"];
        yield return ["http:"];
        yield return ["https:"];
        yield return ["HTTP:"];
        yield return ["HTTPS:"];
        yield return ["http "];
        yield return [" https"];
        yield return [" http "];
        yield return ["ht tp"];
        yield return ["htt ps"];
        yield return ["http1"];
        yield return ["https2"];
        yield return ["httpx"];
        yield return ["httpss"];
        yield return ["httpp"];
    }

    public static IEnumerable<object[]> GetMatchingUris()
    {
        yield return ["https://example.com/api", "https://example.com/api"];
        yield return ["https://example.com/api?param=value", "https://example.com/api"];
        yield return ["https://example.com/api#fragment", "https://example.com/api"];
        yield return ["https://EXAMPLE.COM/API", "https://example.com/api"];
        yield return ["http://example.com/api", "http://example.com/api"];
        yield return ["https://example.com:443/api", "https://example.com/api"];
        yield return ["http://example.com:80/api", "http://example.com/api"];
        yield return ["https://example.com/api%20with%20spaces", "https://example.com/api with spaces"];
        yield return ["https://example.com/api%2Fencoded", "https://example.com/api/encoded"];
        yield return ["https://example.com/api/../other", "https://example.com/other"];
        yield return ["https://example.com/api/./current", "https://example.com/api/current"];
        yield return ["https://example.com/api/path?query=value&other=data", "https://example.com/api/path"];
        yield return ["https://example.com/api/path#section", "https://example.com/api/path"];
        yield return ["https://user:pass@example.com/api", "https://example.com/api"];
    }

    public static IEnumerable<object[]> GetDifferingUris()
    {
        yield return ["https://example.com/api", "https://different.com/api"];
        yield return ["https://example.com/api", "https://example.com/different"];
        yield return ["https://example.com:8080/api", "https://example.com:9090/api"];
        yield return ["http://example.com/api", "https://example.com/api"];
        yield return ["https://example.com/api/v1", "https://example.com/api/v2"];
        yield return ["not-a-uri", "https://example.com/api"];
        yield return ["https://example.com/api", "not-a-uri"];
        yield return ["not-a-uri", "also-not-a-uri"];
        yield return ["", "https://example.com/api"];
        yield return ["https://example.com/api", ""];
        yield return ["ftp://example.com/file", "https://example.com/api"];
        yield return ["https://example.com/api", "ftp://example.com/file"];
        yield return [null, "https://example.com/api"];
        yield return ["https://example.com/api", null];
        yield return [null, null];
        yield return ["   ", "https://example.com/api"];
        yield return ["https://example.com/api", "   "];
        yield return ["   ", "   "];
        yield return ["relative/path", "https://example.com/api"];
        yield return ["https://example.com/api", "relative/path"];
        yield return ["//example.com/api", "https://example.com/api"];
    }

    [Fact]
    public async Task ValidateTokenSignature_SetsProofClaims_When_ValidationSucceeds()
    {
        var mockTokenHandler = A.Fake<JsonWebTokenHandler>();
        DPoPProofValidationService service = TestUtilities.CreateServiceWithMockTokenHandler(mockTokenHandler);

        var claimsIdentity = new ClaimsIdentity(new[]
        {
            new Claim(KeyCloakConstants.DPoP.Jti, "test-jti-123"),
            new Claim(KeyCloakConstants.DPoP.Htm, "POST"),
            new Claim(KeyCloakConstants.DPoP.Htu, "https://api.example.com"),
            new Claim(KeyCloakConstants.DPoP.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        });

        A.CallTo(() => mockTokenHandler.ValidateTokenAsync(A<string>._, A<TokenValidationParameters>._))
            .Returns(new TokenValidationResult
            {
                IsValid = true,
                ClaimsIdentity = claimsIdentity
            });

        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithJsonWebKey();

        await service.ValidateTokenSignature(parameters, validationResult);

        validationResult.HasError.Should().BeFalse();
        validationResult.ProofClaims.Should().NotBeNull();
        validationResult.ProofClaims.Should().ContainKey(KeyCloakConstants.DPoP.Jti);
    }

    [Fact]
    public void TryExtractJsonWebKey_WhenJwkHeaderIsNull_ReturnsFalse()
    {
        JsonWebToken token = TestUtilities.CreateTokenWithoutJwkHeader();

        var result = _service.TryExtractJsonWebKey(token, out var jwkJson);

        result.Should().BeFalse();
        jwkJson.Should().BeEmpty();
    }

    [Fact]
    public void TryCreateJsonWebKey_WithInvalidJson_ReturnsFalse()
    {
        var invalidJwk = """{invalid json}""";

        var result = _service.TryCreateJsonWebKey(invalidJwk, out JsonWebKey? jwk);

        result.Should().BeFalse();
        jwk.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessTokenHash_WithInvalidHashAlgorithm_ReturnsFalse()
    {
        var accessToken = "test.access.token";
        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, "wrong-hash-value" }
            }
        };

        var result = _service.ValidateAccessTokenHash(accessToken, validationResult);

        result.Should().BeFalse();
    }

    [Fact]
    public void ComputeAccessTokenHash_ProducesCorrectSha256Hash()
    {
        var accessToken = "test.jwt.token";

        var hash1 = _service.ComputeAccessTokenHash(accessToken);
        var hash2 = _service.ComputeAccessTokenHash(accessToken);

        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void ValidateHtuClaimValue_WithQueryStringDifference_ReturnsTrue()
    {
        var requestUri = "https://example.com/api?param=value";
        var htuValue = "https://example.com/api";

        var result = _service.ValidateHtuClaimValue(requestUri, htuValue);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtuClaimValue_WithFragmentDifference_ReturnsTrue()
    {
        var requestUri = "https://example.com/api#section";
        var htuValue = "https://example.com/api";

        var result = _service.ValidateHtuClaimValue(requestUri, htuValue);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtuClaimValue_WithPathDifference_ReturnsFalse()
    {
        var requestUri = "https://example.com/api/v1";
        var htuValue = "https://example.com/api/v2";

        var result = _service.ValidateHtuClaimValue(requestUri, htuValue);

        result.Should().BeFalse();
    }

    [Fact]
    public void CompareUrisStructurally_WithNonAbsoluteUri_ReturnsFalse()
    {
        var requestUri = "relative/path";
        var htuValue = "https://example.com/api";

        var result = _service.CompareUrisStructurally(requestUri, htuValue);

        result.Should().BeFalse();
    }

    [Fact]
    public void CompareUrisStructurally_WithDifferentScheme_ReturnsFalse()
    {
        var requestUri = "http://example.com/api";
        var htuValue = "https://example.com/api";

        var result = _service.CompareUrisStructurally(requestUri, htuValue);

        result.Should().BeFalse();
    }

    [Fact]
    public void CompareUrisStructurally_WithDifferentPort_ReturnsFalse()
    {
        var requestUri = "https://example.com:8080/api";
        var htuValue = "https://example.com:9090/api";

        var result = _service.CompareUrisStructurally(requestUri, htuValue);

        result.Should().BeFalse();
    }

    [Fact]
    public void CompareUrisStructurally_WithDefaultPortComparison_ReturnsTrue()
    {
        var requestUri = "https://example.com:443/api";
        var htuValue = "https://example.com/api";

        var result = _service.CompareUrisStructurally(requestUri, htuValue);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidScheme_WithMixedCaseHttp_ReturnsTrue()
    {
        var result = _service.IsValidScheme("HtTp");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidScheme_WithMixedCaseHttps_ReturnsTrue()
    {
        var result = _service.IsValidScheme("HtTpS");

        result.Should().BeTrue();
    }

    [Fact]
    public void TryParseCnfClaim_WithValidNestedStructure_ReturnsTrue()
    {
        var cnfValue = """{"jkt":"thumbprint","nested":{"key":"value"}}""";

        var result = _service.TryParseCnfClaim(cnfValue, out Dictionary<string, JsonElement>? cnfJson);

        result.Should().BeTrue();
        cnfJson.Should().NotBeNull();
        cnfJson.Should().ContainKey("jkt");
        cnfJson.Should().ContainKey("nested");
    }

    [Fact]
    public void TryValidateThumbprintBinding_WithMismatchedThumbprint_ReturnsFalse()
    {
        var cnfJson = new Dictionary<string, JsonElement>
        {
            { KeyCloakConstants.DPoP.JwkThumbprint, JsonDocument.Parse("\"actual-thumbprint\"").RootElement }
        };

        var result = _service.TryValidateThumbprintBinding(cnfJson, "expected-thumbprint");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateJtiClaim_WithGuidFormat_ReturnsTrue()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Jti, Guid.NewGuid().ToString() }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateJtiClaim_WithBase64UrlFormat_ReturnsTrue()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Jti, "dGVzdC1qdGktdmFsdWU" }
        };

        var result = _service.ValidateJtiClaim(proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtmClaim_WithUppercaseMethod_ReturnsTrue()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "GET" }
        };

        var result = _service.ValidateHtmClaim("GET", proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHtmClaim_WithLowercaseMethod_ReturnsTrue()
    {
        var proofClaims = new Dictionary<string, object>
        {
            { KeyCloakConstants.DPoP.Htm, "get" }
        };

        var result = _service.ValidateHtmClaim("GET", proofClaims);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIatClaim_WithCurrentTimestamp_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParameters();
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(now);

        var result = _service.ValidateIatClaim(parameters, validationResult);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIatClaim_WithFutureTimestampWithinOffset_ReturnsTrue()
    {
        var futureTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30;
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParametersWithOffset(leeway: 60);
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(futureTime);

        var result = _service.ValidateIatClaim(parameters, validationResult);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIatClaim_WithPastTimestampWithinLeeway_ReturnsTrue()
    {
        var pastTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60;
        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParametersWithOffset(iatOffset: 120);
        DPoPProofValidationResult validationResult = TestUtilities.CreateValidationResultWithIat(pastTime);

        var result = _service.ValidateIatClaim(parameters, validationResult);

        result.Should().BeTrue();
    }

    [Fact]
    public void ExtractIssuedAt_WithIntMaxValue_ReturnsCorrectLong()
    {
        var result = _service.ExtractIssuedAt(int.MaxValue);

        result.Should().Be((long)int.MaxValue);
    }

    [Fact]
    public void ExtractIssuedAt_WithNegativeValue_ReturnsCorrectLong()
    {
        var result = _service.ExtractIssuedAt(-123);

        result.Should().Be(-123L);
    }

    [Fact]
    public void ExtractIssuedAt_WithFloatValue_ReturnsNull()
    {
        var result = _service.ExtractIssuedAt(123.45f);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractIssuedAt_WithStringValue_ReturnsNull()
    {
        var result = _service.ExtractIssuedAt("123");

        result.Should().BeNull();
    }

    [Fact]
    public void TryParseProofToken_WithComplexValidToken_ReturnsTrue()
    {
        var validToken = TestUtilities.CreateValidJwtToken();

        var result = _service.TryParseProofToken(validToken, out JsonWebToken? token);

        result.Should().BeTrue();
        token.Should().NotBeNull();
    }

    [Fact]
    public void TryParseProofToken_WithMalformedSignature_ReturnsFalse()
    {
        var malformedToken = "header.payload.invalid@signature";

        var result = _service.TryParseProofToken(malformedToken, out JsonWebToken? token);

        result.Should().BeFalse();
        token.Should().BeNull();
    }

    [Fact]
    public async Task ValidateDPoPPayload_WithAllValidClaims_DoesNotSetError()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var accessToken = "test.access.token";
        var computedHash = _service.ComputeAccessTokenHash(accessToken);

        var parameters = new DPoPProofValidationParameters
        {
            ProofToken = "valid.proof.token",
            AccessToken = accessToken,
            Htm = "POST",
            Htu = "https://api.example.com/resource",
            Options = new DPoPOptions
            {
                IatOffset = 60,
                Leeway = 300
            }
        };

        var validationResult = new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Ath, computedHash },
                { KeyCloakConstants.DPoP.Jti, Guid.NewGuid().ToString() },
                { KeyCloakConstants.DPoP.Htm, "POST" },
                { KeyCloakConstants.DPoP.Htu, "https://api.example.com/resource" },
                { KeyCloakConstants.DPoP.Iat, now }
            }
        };

        await _service.ValidateDPoPPayloadAsync(parameters, validationResult);

        validationResult.HasError.Should().BeFalse();
    }

    [Fact]
    public void ValidateCnf_WithValidThumbprintMatch_DoesNotSetError()
    {
        var thumbprint = "test-thumbprint-value";
        var cnfValue = $$"""{"{{KeyCloakConstants.DPoP.JwkThumbprint}}":"{{thumbprint}}"}""";

        DPoPProofValidationParameters parameters = TestUtilities.CreateValidationParametersWithAccessTokenClaim(
            new List<Claim> { new(KeyCloakConstants.DPoP.Cnf, cnfValue) });

        var validationResult = new DPoPProofValidationResult
        {
            JsonWebKeyThumbprint = thumbprint
        };

        _service.ValidateCnf(parameters, validationResult);

        validationResult.HasError.Should().BeFalse();
    }
}
