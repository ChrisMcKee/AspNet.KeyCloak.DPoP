using System.Security.Claims;
using System.Text;
using System.Text.Json;

using AspNetCore.Authentication.Api;
using AspNetCore.Authentication.Api.DPoP;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;

using Moq;

namespace UnitTests;

internal static class TestUtilities
{
    internal static MessageReceivedContext CreateContextWithDPoPAuthAndProof()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "DPoP token";
        httpContext.Request.Headers.Append(KeyCloakConstants.DPoP.ProofHeader, new StringValues("valid-proof-token"));

        return new MessageReceivedContext(
            httpContext,
            new AuthenticationScheme("KeyCloak", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions());
    }


    internal static Mock<MessageReceivedContext> CreateMessageReceivedContextMock()
    {
        var contextMock = new Mock<MessageReceivedContext>(
            new DefaultHttpContext(),
            new AuthenticationScheme("KeyCloak", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions());
        return contextMock;
    }

    internal static MessageReceivedContext CreateMessageReceivedContext()
    {
        var context = new MessageReceivedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("KeyCloak", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions()
        );
        return context;
    }

    internal static MessageReceivedContext WithDPoPOptions(this MessageReceivedContext context, DPoPOptions dPoPOptions)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dPoPOptions);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        context.HttpContext.RequestServices = serviceProvider;
        return context;
    }


    internal static string CreateValidJwtToken()
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var payload =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes("{\"sub\":\"1234567890\",\"name\":\"John Doe\",\"iat\":1516239022}"));
        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("signature"));

        return $"{header}.{payload}.{signature}";
    }

    internal static JsonWebToken CreateTokenWithoutJwkHeader()
    {
        var header = new Dictionary<string, object>
        {
            { "alg", "RS256" },
            { "typ", "JWT" }
        };
        var payload = new Dictionary<string, object>
        {
            { "sub", "test-subject" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        return CreateJsonWebToken(header, payload);
    }

    internal static JsonWebToken CreateTokenWithJwkHeader()
    {
        var jwkData = new { kty = "RSA", use = "sig" };
        var header = new Dictionary<string, object>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { KeyCloakConstants.DPoP.JsonWebKey, jwkData }
        };
        var payload = new Dictionary<string, object>
        {
            { "sub", "test-subject" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        return CreateJsonWebToken(header, payload);
    }

    internal static JsonWebToken CreateTokenWithComplexJwkHeader()
    {
        var complexJwkData = new Dictionary<string, object>
        {
            { "kty", "RSA" },
            { "use", "sig" },
            { "kid", "test-key-id" },
            { "n", "sample-modulus" },
            { "e", "AQAB" }
        };
        var header = new Dictionary<string, object>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { KeyCloakConstants.DPoP.JsonWebKey, complexJwkData }
        };
        var payload = new Dictionary<string, object>
        {
            { "sub", "test-subject" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        return CreateJsonWebToken(header, payload);
    }

    internal static JsonWebToken CreateTokenWithEmptyJwkHeader()
    {
        var emptyJwkData = new { };
        var header = new Dictionary<string, object>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { KeyCloakConstants.DPoP.JsonWebKey, emptyJwkData }
        };
        var payload = new Dictionary<string, object>
        {
            { "sub", "test-subject" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        return CreateJsonWebToken(header, payload);
    }

    internal static JsonWebToken CreateJsonWebToken(Dictionary<string, object> header,
        Dictionary<string, object> payload)
    {
        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);
        var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
        const string signature = "fake-signature";

        var tokenString = $"{headerBase64}.{payloadBase64}.{signature}";
        return new JsonWebToken(tokenString);
    }

    internal static DPoPProofValidationService CreateServiceWithMockTokenHandler(JsonWebTokenHandler tokenHandler)
    {
        var service = new DPoPProofValidationService
        {
            TokenHandler = tokenHandler
        };
        return service;
    }

    internal static DPoPProofValidationResult CreateValidationResultWithJsonWebKey()
    {
        return new DPoPProofValidationResult
        {
            JsonWebKey = """
                         {
                             "kty": "RSA",
                             "kid": "test-key-id",
                             "n": "test-modulus",
                             "e": "AQAB"
                         }
                         """
        };
    }

    internal static DPoPProofValidationResult CreateValidationResult()
    {
        return new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>()
        };
    }

    internal static DPoPProofValidationParameters CreateValidationParameters()
    {
        return new DPoPProofValidationParameters
        {
            Options = new DPoPOptions(),
            ProofToken =
                "eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2IiwiandrIjp7Imt0eSI6IkVDIiwieCI6Imw4dEZyaHgtMzR0VjNoUklDUkRZOXpDa0RscEJoRjQyVVFVZldWQVdCRnMiLCJ5IjoiOVZFNGpmX09rX282NHpiVFRsY3VOSmFqSG10NnY5VERWclUwQ2R2R1JEQSIsImNydiI6IlAtMjU2In19.eyJqdGkiOiItQndDM0VTYzZhY2MybFRjIiwiaHRtIjoiUE9TVCIsImh0dSI6Imh0dHBzOi8vc2VydmVyLmV4YW1wbGUuY29tL3Rva2VuIiwiaWF0IjoxNTYyMjYyNjE2fQ.5Gg1UpcvdgEPtlWgGHDJuR0x1Dxj8F5AAkqMfPteSzSOXpBqWgQuuiviekzDX6pPdP6gQYuLbhWUCzWZCCgYeg",
            AccessToken = "dummy-access-token",
            Htm = "GET",
            Htu = "https://example.com/api"
        };
    }

    internal static DPoPProofValidationParameters CreateValidationParametersWithAccessTokenClaim(
        List<Claim>? accessTokenClaim = null)
    {
        return new DPoPProofValidationParameters
        {
            AccessTokenClaims = accessTokenClaim,
            ProofToken = "dummy-token",
            AccessToken = "dummy-access-token",
            Htm = "GET",
            Htu = "https://example.com/api",
            Options = new DPoPOptions()
        };
    }

    internal static DPoPProofValidationParameters CreateValidationParametersWithOffset(int iatOffset = 0,
        int leeway = 0)
    {
        return new DPoPProofValidationParameters
        {
            Options = new DPoPOptions
            {
                IatOffset = iatOffset,
                Leeway = leeway
            },
            ProofToken = "dummy-token",
            AccessToken = "dummy-access-token",
            Htm = "GET",
            Htu = "https://example.com/api"
        };
    }

    internal static DPoPProofValidationResult CreateValidationResultWithIat(object iatValue)
    {
        return new DPoPProofValidationResult
        {
            ProofClaims = new Dictionary<string, object>
            {
                { KeyCloakConstants.DPoP.Iat, iatValue }
            }
        };
    }


    internal static DPoPProofValidationParameters CreateDPoPProofValidationParametersWithMissingJwk()
    {
        // Returns parameters with a proof token missing the JWK in the header
        return new DPoPProofValidationParameters
        {
            ProofToken = "token.without.jwk.header",
            AccessToken = "valid.access.token",
            Options = new DPoPOptions()
            {
                IatOffset = 60,
                Leeway = 60
            },
            Htm = "GET",
            Htu = "https://api.example.com/resource"
        };
    }

    internal static DPoPProofValidationParameters CreateDPoPProofValidationParametersWithInvalidJwkJson()
    {
        return new DPoPProofValidationParameters
        {
            ProofToken = "invalid.token.with.malformed.jwk",
            AccessToken = "valid.access.token",
            Options = new DPoPOptions()
            {
                IatOffset = 60,
                Leeway = 60
            },
            Htm = "GET",
            Htu = "https://api.example.com/resource"
        };
    }

    internal static JsonWebToken CreateTokenWithInvalidJwkJson()
    {
        var invalidJwkData = "not-a-valid-jwk-object";
        var header = new Dictionary<string, object>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { KeyCloakConstants.DPoP.JsonWebKey, invalidJwkData }
        };
        var payload = new Dictionary<string, object>
        {
            { "sub", "test-subject" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        return CreateJsonWebToken(header, payload);
    }

    internal static JsonWebToken CreateTokenWithPrivateJwk()
    {
        var privateJwkData = new Dictionary<string, object>
        {
            { "kty", "RSA" },
            { "use", "sig" },
            { "kid", "test-key-id" },
            { "n", "sample-modulus" },
            { "e", "AQAB" },
            { "d", "private-exponent" },
            { "p", "prime1" },
            { "q", "prime2" }
        };
        var header = new Dictionary<string, object>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { KeyCloakConstants.DPoP.JsonWebKey, privateJwkData }
        };
        var payload = new Dictionary<string, object>
        {
            { "sub", "test-subject" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        return CreateJsonWebToken(header, payload);
    }

    internal static string CreateValidRsaJwkWithPrivateKey()
    {
        return """
               {
                   "kty": "RSA",
                   "n": "0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAtVT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn64tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FDW2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n91CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINHaQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw",
                   "e": "AQAB",
                   "d": "X4cTteJY_gn4FYPsXB8rdXix5vwsg1FLN5E3EaG6RJoVH-HLLKD9M7dx5oo7GURknchnrRweUkC7hT5fJLM0WbFAKNLWY2vv7B6NqXSzUvxT0_YSfqijwp3RTzlBaCxWp4doFk5N2o8Gy_nHNKroADIkJ46pRUohsXywbReAdYaMwFs9tv8d_cPVY3i07a3t8MN6TNwm0dSawm9v47UiCl3Sk5ZiG7xojPLu4sbg1U2jx4IBTNBznbJSzFHK66jT8bgkuqsk0GjskDJk19Z4qwjwbsnn4j2WBii3RL-Us2lGVkY8fkFzme1z0HbIkfz0Y6mqnOYtqc0X4jfcKoAC8Q",
                   "p": "83i-7IvMGXoMXCskv73TKr8637FiO7Z27zv8oj6pbWUQyLPQBQxtPVnwD20R-60eTDmD2ujnMt5PoqMrm8RfmNhVWDtjjMmCMjOpSXicFHj7XOuVIYQyqVWlWEh6dN36GVZYk93N8Bc9vY41xy8B9RzzOGVQzXvNEvn7O0nVbfs",
                   "q": "3dfOR9cuYq-0S-mkFLzgItgMEfFzB2q3hWehMuG0oCuqnb3vobLyumqjVZQO1dIrdwgTnCdpYzBcOfW5r370AFXjiWft0NGW9GPmQQTn_B-bPzp7qhXrGc1fQ-4fEK-mFMQdOQyxb2cNYWi8Wv1kVlqK1n1F0U2CfCNq5n9cBw0",
                   "dp": "G4sPXkc6Ya9y8oJW9_ILj4xuppu0lzi_H7VTkS8xj5SdX3coE0oimYwxIi2emTAue0UOa5dpgFGyBJ4c8tQ2VF402XRugKDTP8akYhFo5tAA77Qe_NmtuYZc3C3m3I24G2GvR5sSDxUyAN2zq8Lfn9EUms6rY3Ob8YeiKkTiBj0",
                   "dq": "s9lAH9fggBsoFR8Oac2R_E2gw282rT2kGOAhvIllETE1efrA6huUUvMfBcMpn8lqeW6vzznYY5SSQF7pMdC_agI3nG8Ibp1BUb0JUiraRNqUfLhcQb_d9GF4Dh7e74WbRsobRonujTYN1xCaP6TO61jvWrX-L18txXw494Q_cgk",
                   "qi": "GyM_p6JrXySiz1toFgKbWV-JdI3jQ4ypu9rbMWx3rQJBfmt0FoYzgUIZEVFEcOqwemRN81zoDAaa-Bk0KWNGDjJHZDdDmFhW3AN7lI-puxk_mHZGJ11rxyR8O55XLSe3SPmRfKwZI6yU24ZxvQKFYItdldUKGzO6Ia6zTKhAVRU"
               }
               """;
    }

    internal static DPoPProofValidationParameters CreateDPoPProofValidationParametersWithPrivateJwk()
    {
        return new DPoPProofValidationParameters
        {
            ProofToken = "token.with.private.jwk",
            AccessToken = "valid.access.token",
            Options = new DPoPOptions()
            {
                IatOffset = 60,
                Leeway = 60
            },
            Htm = "GET",
            Htu = "https://api.example.com/resource"
        };
    }

    internal static DPoPProofValidationParameters CreateValidationParametersWithInvalidProof()
    {
        return new DPoPProofValidationParameters
        {
            ProofToken = null,
            AccessToken = "valid.access.token",
            Options = new DPoPOptions()
            {
                IatOffset = 60,
                Leeway = 60
            },
            Htm = "GET",
            Htu = "https://api.example.com/resource"
        };
    }

    internal static DPoPEventHandlers CreateDPoPEventHandlers()
    {
        return new DPoPEventHandlers();
    }

    internal static IServiceProvider CreateServiceProviderWithDPoPOptions(DPoPOptions? options = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(options ?? new DPoPOptions
        {
            Mode = DPoPModes.Allowed,
        });
        return serviceCollection.BuildServiceProvider();
    }
}
