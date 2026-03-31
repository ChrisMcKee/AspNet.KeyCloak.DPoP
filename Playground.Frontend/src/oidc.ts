import { oidcSpa } from "oidc-spa/react-tanstack-start";
import { z } from "zod";

export const {
    bootstrapOidc,
    useOidc,
    getOidc,
    // NOTE: Each time you enforceLogin on a route the oidc-spa vite plugin
    // will automatically switch this route to `ssr: false`.
    // This ensures that everything that can be SSR'd is and the rest is delayed to the client.
    enforceLogin,
    oidcFnMiddleware,
    oidcRequestMiddleware
} = oidcSpa
    .withExpectedDecodedIdTokenShape({
        decodedIdTokenSchema: z.object({
            name: z.string(),
            picture: z.string().optional(),
            email: z.email().optional(),
            preferred_username: z.string().optional(),
            realm_access: z.object({ roles: z.array(z.string()) }).optional()
        }),
        decodedIdToken_mock: {
            name: "John Doe",
            preferred_username: "john.doe",
            realm_access: {
                roles: ["realm-admin"]
            }
        }
    })
    .withAccessTokenValidation({
        type: "RFC 9068: JSON Web Token (JWT) Profile for OAuth 2.0 Access Tokens",
        expectedAudience: (/*{ paramsOfBootstrap, process }*/) => "test-api",
        accessTokenClaimsSchema: z.object({
            sub: z.string(),
            realm_access: z.object({ roles: z.array(z.string()) }).optional()
        }),
        accessTokenClaims_mock: {
            sub: "u123",
            realm_access: {
                roles: ["realm-admin"]
            }
        }
    })
    // See: https://docs.oidc-spa.dev/features/auto-login#tanstack-start
    //.withAutoLogin()
    .createUtils();

// Can be call anywhere, even in the body of a React component.
// All later calls will be safely ignored.
bootstrapOidc(({ process }) =>
    process.env.OIDC_USE_MOCK === "true"
        ? {
              implementation: "mock",
              isUserInitiallyLoggedIn: true
          }
        : {
              implementation: "real",
              issuerUri: process.env.OIDC_ISSUER_URI,
              clientId: process.env.OIDC_CLIENT_ID,
              debugLogs: true,
              warnUserSecondsBeforeAutoLogout: 45,
              idleSessionLifetimeInSeconds: 300, // 5 minutes
          }
);

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "";

/**
 * Shared API fetch wrapper for Playground.Server calls.
 * Relative URLs are resolved against VITE_API_BASE_URL.
 * If auth is enabled, oidc-spa intercepts the Authorization header and adds DPoP proof.
 */
export const fetchApi = async (
    input: Parameters<typeof fetch>[0],
    init?: RequestInit,
    options?: {
        auth?: "none" | "optional" | "required";
    }
): Promise<Response> => {
    const authMode = options?.auth ?? "optional";
    const oidc = await getOidc();

    const url =
        typeof input === "string" && input.startsWith("/")
            ? `${apiBaseUrl}${input}`
            : input;

    if (authMode === "required" && !oidc.isUserLoggedIn) {
        throw new Error("User must be logged in to call this endpoint.");
    }

    if (authMode !== "none" && oidc.isUserLoggedIn) {
        const accessToken = await oidc.getAccessToken();
        const headers = new Headers(init?.headers);
        headers.set("Authorization", `Bearer ${accessToken}`);
        (init ??= {}).headers = headers;
    }

    return fetch(url, init);
};

export const fetchWithAuth: typeof fetch = (input, init) =>
    fetchApi(input, init, { auth: "required" });

