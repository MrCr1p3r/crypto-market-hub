namespace GUI_Crypto.Infrastructure.Caching;

/// <summary>
/// Service for managing cache warmup state across the application.
/// </summary>
public interface ICacheWarmupStateService
{
    /// <summary>
    /// Gets a value indicating whether the cache warmup has completed.
    /// </summary>
    bool IsWarmedUp { get; }

    /// <summary>
    /// Marks the cache as warmed up.
    /// </summary>
    void MarkAsWarmedUp();
}
