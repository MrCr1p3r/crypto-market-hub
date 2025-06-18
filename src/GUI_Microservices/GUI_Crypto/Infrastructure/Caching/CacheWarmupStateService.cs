namespace GUI_Crypto.Infrastructure.Caching;

/// <summary>
/// Implementation of cache warmup state service that tracks warmup status in memory.
/// </summary>
public class CacheWarmupStateService : ICacheWarmupStateService
{
    private volatile bool _isWarmedUp;

    /// <inheritdoc />
    public bool IsWarmedUp => _isWarmedUp;

    /// <inheritdoc />
    public void MarkAsWarmedUp()
    {
        _isWarmedUp = true;
    }
}
