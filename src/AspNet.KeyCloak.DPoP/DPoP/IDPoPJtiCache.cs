namespace AspNetCore.Authentication.Api.DPoP;

/// <summary>
///     Tracks DPoP proof <c>jti</c> (JWT ID) values to prevent replay attacks.
///     Implementations must treat <see cref="TryAddAsync" /> as an atomic set-if-absent operation.
/// </summary>
/// <remarks>
///     The default registration is <see cref="InMemoryDPoPJtiCache" />, suitable for single-instance deployments.
///     Replace it with a distributed implementation (e.g. Redis) in multi-instance environments.
/// </remarks>
public interface IDPoPJtiCache
{
    /// <summary>
    ///     Attempts to record a <paramref name="jti" /> value as seen.
    /// </summary>
    /// <param name="jti">The JWT ID claim value from the DPoP proof token.</param>
    /// <param name="ttl">How long to retain the entry. Should cover the full proof acceptance window.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    ///     <c>true</c> if the <paramref name="jti" /> was not previously recorded (proof is fresh);
    ///     <c>false</c> if it was already present (replay detected).
    /// </returns>
    Task<bool> TryAddAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default);
}
