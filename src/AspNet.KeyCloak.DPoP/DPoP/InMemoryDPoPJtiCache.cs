using Microsoft.Extensions.Caching.Memory;

namespace AspNetCore.Authentication.Api.DPoP;

/// <summary>
///     In-process <see cref="IDPoPJtiCache" /> backed by <see cref="IMemoryCache" />.
///     Suitable for single-instance deployments. Replace with a distributed cache for multi-instance environments.
/// </summary>
public sealed class InMemoryDPoPJtiCache : IDPoPJtiCache
{
    private readonly IMemoryCache _cache;

    /// <summary>Initialises a new instance using a dedicated <see cref="MemoryCache" />.</summary>
    public InMemoryDPoPJtiCache() : this(new MemoryCache(new MemoryCacheOptions()))
    {
    }

    /// <summary>Initialises a new instance using the provided <paramref name="cache" />.</summary>
    public InMemoryDPoPJtiCache(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public Task<bool> TryAddAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // GetOrCreate returns the existing value if the key already exists, or creates and stores a new
        // sentinel value if it does not. We treat the presence of any existing entry as a replay.
        var isNew = false;
        _cache.GetOrCreate(jti, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            isNew = true;
            return true;
        });

        return Task.FromResult(isNew);
    }
}
