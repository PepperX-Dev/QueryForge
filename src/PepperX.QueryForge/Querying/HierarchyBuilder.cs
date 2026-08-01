namespace PepperX.QueryForge.Querying;

/// <summary>
/// Builds the nested <see cref="HierarchyNode{TModel}"/> tree returned by grouped queries.
/// </summary>
/// <remarks>
/// This is the single, shared grouping implementation used by every QueryForge execution
/// provider, so that a grouped <see cref="Query"/> produces an identical
/// <see cref="QueryResult{TModel}"/> shape regardless of how the rows were fetched.
/// <para>
/// The contract it implements:
/// <list type="bullet">
/// <item>A node holds either <see cref="HierarchyNode{TModel}.SubGroups"/> or
/// <see cref="HierarchyNode{TModel}.Items"/>, never both.</item>
/// <item><see cref="HierarchyNode{TModel}.Count"/> is the total number of leaf rows beneath the
/// node, not the number of direct children.</item>
/// <item><see langword="null"/> is a valid grouping key and forms its own group.</item>
/// <item>Group keys are ordered by each level's <see cref="GroupByDescriptor.SortOrder"/>, with
/// nulls first ascending and last descending, matching SQL ordering semantics.</item>
/// <item>Leaf rows keep the order they arrive in, which is the order the caller's
/// <see cref="Query.SortColumns"/> already established.</item>
/// </list>
/// </para>
/// </remarks>
public static class HierarchyBuilder
{
    /// <summary>
    /// Groups a flat, already-sorted set of rows into a hierarchy.
    /// </summary>
    /// <typeparam name="TModel">The domain model type for the leaf items.</typeparam>
    /// <param name="rows">
    /// The flat rows to nest. These must already contain every row belonging to the groups being
    /// built, otherwise the resulting counts will be short.
    /// </param>
    /// <param name="groups">The grouping levels, outermost first.</param>
    /// <param name="valueAccessor">
    /// Reads a column value from a row. Defaults to a cached, case-insensitive property lookup,
    /// which suits the POCOs that Dapper and EF Core materialize.
    /// </param>
    /// <returns>
    /// The nested hierarchy, or an empty list when <paramref name="groups"/> is empty.
    /// </returns>
    public static IReadOnlyList<HierarchyNode<TModel>> Build<TModel>(
        IReadOnlyList<TModel> rows,
        IReadOnlyList<GroupByDescriptor> groups,
        Func<TModel, string, object?>? valueAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(groups);

        if (groups.Count == 0 || rows.Count == 0)
            return Array.Empty<HierarchyNode<TModel>>();

        return BuildLevel(rows, groups, 0, valueAccessor ?? PropertyAccessor.For<TModel>());
    }

    private static IReadOnlyList<HierarchyNode<TModel>> BuildLevel<TModel>(
        IReadOnlyList<TModel> rows,
        IReadOnlyList<GroupByDescriptor> groups,
        int level,
        Func<TModel, string, object?> accessor)
    {
        var descriptor = groups[level];
        var isLeafLevel = level == groups.Count - 1;

        // Bucket in first-seen order, then sort explicitly, so the result is deterministic
        // regardless of how the underlying dictionary happens to enumerate.
        var buckets = new List<Bucket<TModel>>();
        var index = new Dictionary<GroupKey, Bucket<TModel>>();

        foreach (var row in rows)
        {
            var key = Normalize(accessor(row, descriptor.ColumnName));

            if (!index.TryGetValue(new GroupKey(key), out var bucket))
            {
                bucket = new Bucket<TModel>(key);
                index.Add(new GroupKey(key), bucket);
                buckets.Add(bucket);
            }

            bucket.Rows.Add(row);
        }

        var ordered = descriptor.SortOrder == SortOrder.Descending
            ? buckets.OrderByDescending(b => b.Key, QueryValueComparer.Instance)
            : buckets.OrderBy(b => b.Key, QueryValueComparer.Instance);

        var nodes = new List<HierarchyNode<TModel>>(buckets.Count);

        foreach (var bucket in ordered)
        {
            // Every row in the bucket is a leaf row beneath this node, at any depth.
            var count = bucket.Rows.Count;

            nodes.Add(isLeafLevel
                ? new HierarchyNode<TModel>(bucket.Key, count, null, bucket.Rows)
                : new HierarchyNode<TModel>(bucket.Key, count, BuildLevel(bucket.Rows, groups, level + 1, accessor), null));
        }

        return nodes;
    }

    private sealed class Bucket<TModel>(object? key)
    {
        public object? Key { get; } = key;

        public List<TModel> Rows { get; } = new();
    }

    private static object? Normalize(object? value) => value is DBNull ? null : value;

    #region Key equality

    /// <summary>Null-safe equality wrapper so that null can be used as a dictionary key.</summary>
    private readonly struct GroupKey(object? value) : IEquatable<GroupKey>
    {
        private readonly object? _value = value;

        public bool Equals(GroupKey other)
            => _value is null ? other._value is null : _value.Equals(other._value);

        public override bool Equals(object? obj) => obj is GroupKey other && Equals(other);

        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    }

    #endregion
}
