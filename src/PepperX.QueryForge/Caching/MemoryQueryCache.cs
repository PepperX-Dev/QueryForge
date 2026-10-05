using System.Collections.Concurrent;

namespace PepperX.QueryForge.Caching;

/// <summary>
/// A high-performance, thread-safe in-memory cache implementation of <see cref="IQueryCache"/>
/// with absolute/sliding expiration, tag indexing, and automated expiration eviction.
/// Zero external package dependencies.
/// </summary>
public sealed class MemoryQueryCache : IQueryCache, IDisposable
{
    private sealed class CacheEntry
    {
        public required object Result { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? AbsoluteExpiresAt { get; set; }
        public TimeSpan? SlidingExpiration { get; init; }
        public DateTimeOffset LastAccessedAt { get; set; }
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

        public bool IsExpired(DateTimeOffset now)
        {
            if (AbsoluteExpiresAt.HasValue && now >= AbsoluteExpiresAt.Value)
                return true;

            if (SlidingExpiration.HasValue && now - LastAccessedAt >= SlidingExpiration.Value)
                return true;

            return false;
        }

        public void Refresh(DateTimeOffset now)
        {
            LastAccessedAt = now;
            if (SlidingExpiration.HasValue)
            {
                var newSlidingExpiry = now + SlidingExpiration.Value;
                if (!AbsoluteExpiresAt.HasValue || newSlidingExpiry < AbsoluteExpiresAt.Value)
                    AbsoluteExpiresAt = newSlidingExpiry;
            }
        }
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Default expiration duration applied when an entry specifies none.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the current count of cached query results.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Initializes a new instance of <see cref="MemoryQueryCache"/>.
    /// </summary>
    /// <param name="cleanupInterval">How often to scan and remove expired items. Defaults to 1 minute.</param>
    public MemoryQueryCache(TimeSpan? cleanupInterval = null)
    {
        var interval = cleanupInterval ?? TimeSpan.FromMinutes(1);
        _cleanupTimer = new Timer(_ => CleanExpired(), null, interval, interval);
    }

    /// <inheritdoc />
    public ValueTask<QueryResult<TModel>?> GetAsync<TModel>(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_entries.TryGetValue(key, out var entry))
        {
            var now = DateTimeOffset.UtcNow;
            if (entry.IsExpired(now))
            {
                RemoveInternal(key);
                return ValueTask.FromResult<QueryResult<TModel>?>(null);
            }

            entry.Refresh(now);
            if (entry.Result is QueryResult<TModel> typedResult)
                return ValueTask.FromResult<QueryResult<TModel>?>(typedResult);
        }

        return ValueTask.FromResult<QueryResult<TModel>?>(null);
    }

    /// <inheritdoc />
    public ValueTask SetAsync<TModel>(
        string key,
        QueryResult<TModel> result,
        QueryCacheOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);

        var now = DateTimeOffset.UtcNow;
        var expiration = options.Expiration ?? DefaultExpiration;
        DateTimeOffset? absExpiry = expiration > TimeSpan.Zero ? now + expiration : null;

        var entry = new CacheEntry
        {
            Result = result,
            CreatedAt = now,
            LastAccessedAt = now,
            AbsoluteExpiresAt = absExpiry,
            SlidingExpiration = options.SlidingExpiration,
            Tags = options.Tags
        };

        _entries[key] = entry;

        // Index tags
        if (options.Tags.Count > 0)
        {
            foreach (var tag in options.Tags)
            {
                var keysForTag = _tagIndex.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
                keysForTag[key] = 0;
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        RemoveInternal(key);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask InvalidateTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        if (_tagIndex.TryRemove(tag, out var keys))
        {
            foreach (var key in keys.Keys)
            {
                RemoveInternal(key);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _entries.Clear();
        _tagIndex.Clear();
        return ValueTask.CompletedTask;
    }

    private void RemoveInternal(string key)
    {
        if (_entries.TryRemove(key, out var entry))
        {
            foreach (var tag in entry.Tags)
            {
                if (_tagIndex.TryGetValue(tag, out var keys))
                    keys.TryRemove(key, out _);
            }
        }
    }

    private void CleanExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _entries)
        {
            if (kvp.Value.IsExpired(now))
                RemoveInternal(kvp.Key);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer.Dispose();
            _entries.Clear();
            _tagIndex.Clear();
            _disposed = true;
        }
    }
}
