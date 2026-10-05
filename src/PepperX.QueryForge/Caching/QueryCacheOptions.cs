namespace PepperX.QueryForge.Caching;

/// <summary>
/// Defines caching behavior and lifetime policies for a query execution.
/// </summary>
public sealed class QueryCacheOptions
{
    /// <summary>
    /// Gets or sets whether caching is enabled for this query.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets an explicit cache key. If null or empty, a deterministic key is automatically
    /// calculated from the query criteria, paging, sorting, grouping, projections, and model type.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets an optional prefix prepended to the generated cache key.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration duration relative to when the item is cached.
    /// If null, a default of 5 minutes is applied.
    /// </summary>
    public TimeSpan? Expiration { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration duration. If accessed within this window, the entry's
    /// expiration is extended.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Gets or sets the cache tags associated with this query entry for group or second-level invalidation
    /// (e.g. invalidate all queries tagged with "users" or "reports").
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Creates a default caching policy with the specified absolute expiration.
    /// </summary>
    public static QueryCacheOptions WithExpiration(TimeSpan expiration, params string[] tags) => new()
    {
        Enabled = true,
        Expiration = expiration,
        Tags = tags
    };
}
