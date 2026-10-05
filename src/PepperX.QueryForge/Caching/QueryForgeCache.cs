namespace PepperX.QueryForge.Caching;

/// <summary>
/// Provides access to the ambient/default query cache instance across the QueryForge ecosystem.
/// </summary>
public static class QueryForgeCache
{
    private static IQueryCache _default = new MemoryQueryCache();

    /// <summary>
    /// Gets the ambient default <see cref="IQueryCache"/> used when no specific cache is provided.
    /// Defaults to an in-memory thread-safe <see cref="MemoryQueryCache"/>.
    /// </summary>
    public static IQueryCache Default => _default;

    /// <summary>
    /// Configures the default ambient <see cref="IQueryCache"/> used by QueryForge.
    /// </summary>
    public static void ConfigureDefault(IQueryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _default = cache;
    }
}
