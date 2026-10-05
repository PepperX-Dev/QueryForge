namespace PepperX.QueryForge.Caching;

/// <summary>
/// Fluent extension methods for configuring query caching policies.
/// </summary>
public static class QueryCacheExtensions
{
    /// <summary>
    /// Enables caching for the query with the specified expiration, optional custom key, and tags.
    /// </summary>
    public static TQuery WithCache<TQuery>(
        this TQuery query,
        TimeSpan? expiration = null,
        string? key = null,
        string[]? tags = null,
        TimeSpan? slidingExpiration = null) where TQuery : Query
    {
        ArgumentNullException.ThrowIfNull(query);

        query.Cache = new QueryCacheOptions
        {
            Enabled = true,
            Expiration = expiration,
            Key = key,
            Tags = tags ?? Array.Empty<string>(),
            SlidingExpiration = slidingExpiration
        };

        return query;
    }

    /// <summary>
    /// Enables caching for the query using the specified options.
    /// </summary>
    public static TQuery WithCacheOptions<TQuery>(
        this TQuery query,
        QueryCacheOptions options) where TQuery : Query
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        query.Cache = options;
        return query;
    }

    /// <summary>
    /// Disables caching for the query.
    /// </summary>
    public static TQuery WithoutCache<TQuery>(this TQuery query) where TQuery : Query
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Cache is not null)
            query.Cache.Enabled = false;

        return query;
    }
}
