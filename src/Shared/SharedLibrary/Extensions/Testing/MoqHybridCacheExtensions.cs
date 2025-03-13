using Microsoft.Extensions.Caching.Hybrid;
using Moq;
using Moq.Language.Flow;

namespace SharedLibrary.Extensions.Testing;

public static class MoqHybridCacheExtensions
{
    public static IReturnsResult<HybridCache> SetupGetOrCreateAsync<TState, TResult>(
        this Mock<HybridCache> mockCache,
        string key,
        TResult expectedValue
    )
    {
        return mockCache
            .Setup(m =>
                m.GetOrCreateAsync(
                    It.Is<string>(s => s == key),
                    It.IsAny<TState>(),
                    It.IsAny<Func<TState, CancellationToken, ValueTask<TResult>>>(),
                    It.IsAny<HybridCacheEntryOptions>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expectedValue);
    }

    public static IReturnsResult<HybridCache> SetupGetOrCreateAsyncToExecuteFactory<
        TState,
        TResult
    >(this Mock<HybridCache> mockCache, string key)
    {
        return mockCache
            .Setup(m =>
                m.GetOrCreateAsync(
                    It.Is<string>(s => s == key),
                    It.IsAny<TState>(),
                    It.IsAny<Func<TState, CancellationToken, ValueTask<TResult>>>(),
                    It.IsAny<HybridCacheEntryOptions>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns<
                string,
                TState,
                Func<TState, CancellationToken, ValueTask<TResult>>,
                HybridCacheEntryOptions,
                IEnumerable<string>,
                CancellationToken
            >(async (_, state, factory, _, _, ct) => await factory(state, ct));
    }

    public static IReturnsResult<HybridCache> SetupGetOrCreateAsyncToThrow<TState, TResult>(
        this Mock<HybridCache> mockCache,
        string key,
        Exception exception
    )
    {
        return mockCache
            .Setup(m =>
                m.GetOrCreateAsync(
                    It.Is<string>(s => s == key),
                    It.IsAny<TState>(),
                    It.IsAny<Func<TState, CancellationToken, ValueTask<TResult>>>(),
                    It.IsAny<HybridCacheEntryOptions>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(exception);
    }

    public static void VerifyGetOrCreateAsyncCalled<TState, T>(
        this Mock<HybridCache> mockCache,
        string key,
        Times times
    )
    {
        mockCache.Verify(
            m =>
                m.GetOrCreateAsync(
                    It.Is<string>(s => s == key),
                    It.IsAny<TState>(),
                    It.IsAny<Func<TState, CancellationToken, ValueTask<T>>>(),
                    It.IsAny<HybridCacheEntryOptions>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                ),
            times
        );
    }
}
