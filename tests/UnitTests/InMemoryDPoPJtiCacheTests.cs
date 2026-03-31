using AspNet.KeyCloak.DPoP.DPoP;

namespace UnitTests;

public class InMemoryDPoPJtiCacheTests
{
    private readonly InMemoryDPoPJtiCache _cache = new();

    [Fact]
    public async Task TryAddAsync_NewJti_ReturnsTrue()
    {
        var result = await _cache.TryAddAsync("unique-jti-1", TimeSpan.FromSeconds(60));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAddAsync_DuplicateJti_ReturnsFalse()
    {
        await _cache.TryAddAsync("duplicate-jti", TimeSpan.FromSeconds(60));

        var result = await _cache.TryAddAsync("duplicate-jti", TimeSpan.FromSeconds(60));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAddAsync_DifferentJtis_BothReturnTrue()
    {
        var first  = await _cache.TryAddAsync("jti-a", TimeSpan.FromSeconds(60));
        var second = await _cache.TryAddAsync("jti-b", TimeSpan.FromSeconds(60));

        first.Should().BeTrue();
        second.Should().BeTrue();
    }
}
