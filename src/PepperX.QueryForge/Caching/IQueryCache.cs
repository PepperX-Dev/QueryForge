namespace PepperX.QueryForge.Caching;

/// <summary>
/// Abstraction for second-level and in-memory query result caching.
/// Supports both simple key lookups and tag-based invalidation.
/// </summary>
public interface IQueryCache
{
    /// <summary>
    /// Attempts to retrieve a cached query result by key.
    /// </summary>
    ValueTask<QueryResult<TModel>?> GetAsync<TModel>(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a query result into the cache with the given policy.
    /// </summary>
    ValueTask SetAsync<TModel>(
        string key,
        QueryResult<TModel> result,
        QueryCacheOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entry from the cache by its key.
    /// </summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached query results associated with the specified tag.
    /// </summary>
    ValueTask InvalidateTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
