using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PepperX.QueryForge.EFCore.Translation;
using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.EFCore;

/// <summary>
/// Applies a QueryForge <see cref="Query"/> to an EF Core <see cref="IQueryable{T}"/>.
/// </summary>
/// <remarks>
/// These compose with whatever you already have, so the query you hand in keeps its includes,
/// projections, and any restriction you have already applied:
/// <code>
/// var result = await db.Users
///     .Where(u => u.TenantId == tenantId)
///     .ToQueryResultAsync&lt;User&gt;(query);
/// </code>
/// Because EF Core generates the SQL, this works on every database EF Core supports and honours the
/// model's global query filters, value converters and owned types.
/// </remarks>
public static class QueryForgeQueryableExtensions
{
    /// <summary>Applies only the filtering criteria.</summary>
    public static IQueryable<TModel> ApplyFilter<TModel>(this IQueryable<TModel> source, Query query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var predicate = ExpressionCompiler.BuildPredicate<TModel>(query.Criteria);

        return predicate is null ? source : source.Where(predicate);
    }

    /// <summary>Applies only the sort columns, in order.</summary>
    public static IQueryable<TModel> ApplySort<TModel>(this IQueryable<TModel> source, Query query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        IOrderedQueryable<TModel>? ordered = null;

        foreach (var sort in query.SortColumns)
        {
            // Orderable rather than merely present: a navigation property resolves like any other, but
            // a collection one cannot be translated at all and a reference one orders by a surrogate
            // key nobody asked for.
            var property = ExpressionCompiler.ResolveOrderableProperty<TModel>(sort.ColumnName);

            if (property is null)
                continue;

            var descending = sort.SortOrder == SortOrder.Descending;

            // Databases disagree on where nulls sort — PostgreSQL and Oracle put them last ascending,
            // SQL Server, MySQL and SQLite put them first. EF Core hands ORDER BY straight to the
            // database, so an explicit null-rank key is what makes the same query return the same
            // order everywhere. Only nullable columns need it.
            var nullRank = ExpressionCompiler.BuildNullRank<TModel>(property, nullsFirst: !descending);

            if (nullRank is not null)
            {
                ordered = (IOrderedQueryable<TModel>)ApplyOrdering(
                    ordered ?? source, nullRank, ordered is null ? "OrderBy" : "ThenBy");
            }

            var selector = ExpressionCompiler.BuildSelector<TModel>(sort.ColumnName)!;

            var method = ordered is null
                ? descending ? "OrderByDescending" : "OrderBy"
                : descending ? "ThenByDescending" : "ThenBy";

            ordered = (IOrderedQueryable<TModel>)ApplyOrdering(ordered ?? source, selector, method);
        }

        return ordered ?? source;
    }

    /// <summary>Applies only the paging window.</summary>
    public static IQueryable<TModel> ApplyPaging<TModel>(this IQueryable<TModel> source, Query query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var size = EffectiveSize(query.Paging);
        var number = query.Paging.Number > 0 ? query.Paging.Number : 1;

        return source.Skip((number - 1) * size).Take(size);
    }

    /// <summary>
    /// Applies only the column projection, narrowing the SELECT list so unselected columns are never
    /// fetched.
    /// </summary>
    /// <param name="source">The query to project.</param>
    /// <param name="query">The query intent, whose <see cref="Query.SelectColumns"/> is used.</param>
    /// <param name="alsoKeep">
    /// Extra columns to keep regardless of the selection — used internally for grouping columns,
    /// which the hierarchy is rebuilt from.
    /// </param>
    /// <remarks>
    /// The results are untracked: EF Core does not track instances constructed inside a projection,
    /// which is the behaviour you want here — a partially-populated entity is not something the
    /// change tracker should ever write back. Models without a parameterless constructor and settable
    /// properties are left unprojected rather than failing the query.
    /// </remarks>
    public static IQueryable<TModel> ApplyProjection<TModel>(
        this IQueryable<TModel> source,
        Query query,
        IReadOnlyList<string>? alsoKeep = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        if (query.SelectColumns.Count == 0)
            return source;

        // EF Core drops every Include from a query whose final shape is a constructed instance rather
        // than the tracked entity. Projecting here would therefore return rows whose navigations are
        // empty — a silent loss of the data the caller explicitly asked to load, with nothing in the
        // response to show for it. The projection is the input that cannot be honoured, so it is the
        // one that gets dropped, which is the rule the rest of QueryForge follows.
        if (HasIncludes(source.Expression))
            return source;

        var selector = ExpressionCompiler.BuildProjection<TModel>(query.SelectColumns, alsoKeep);

        return selector is null ? source : source.Select(selector);
    }

    /// <summary>
    /// Whether an <c>Include</c> or <c>ThenInclude</c> already appears in the query.
    /// </summary>
    /// <remarks>
    /// Walks the source chain rather than the whole tree: the operators are composed left to right,
    /// so an include is always reachable through the first argument of each call — including through
    /// the <c>AsNoTracking</c> and <c>AsSplitQuery</c> that usually follow it.
    /// </remarks>
    private static bool HasIncludes(Expression expression)
    {
        while (expression is MethodCallExpression call)
        {
            // A projection the caller wrote themselves has already cost them the includes, so anything
            // further down is moot and QueryForge's own projection is free to apply. Without this, a
            // query that includes and then selects into a DTO would have SelectColumns quietly ignored
            // for no benefit.
            if (call.Method.DeclaringType == typeof(Queryable)
                && call.Method.Name == nameof(Queryable.Select))
            {
                return false;
            }

            // Matched by name because ThenInclude is declared against IIncludableQueryable, so the
            // two do not share a single closed generic definition to compare against.
            if (call.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions)
                && call.Method.Name is "Include" or "ThenInclude")
            {
                return true;
            }

            if (call.Arguments.Count == 0)
                break;

            expression = call.Arguments[0];
        }

        return false;
    }

    /// <summary>Applies filtering, sorting and paging, leaving the query unexecuted.</summary>
    /// <remarks>
    /// Grouping is not applied here, because a hierarchy is a shape rather than a queryable. Use
    /// <see cref="ToQueryResultAsync{TModel}"/> for grouped queries.
    /// </remarks>
    public static IQueryable<TModel> ApplyQuery<TModel>(this IQueryable<TModel> source, Query query)
        => source.ApplyFilter(query).ApplySort(query).ApplyPaging(query).ApplyProjection(query);

    /// <summary>
    /// Executes the query and returns the standard QueryForge result, flat or grouped.
    /// </summary>
    public static async Task<QueryResult<TModel>> ToQueryResultAsync<TModel>(
        this IQueryable<TModel> source,
        Query query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var filtered = source.ApplyFilter(query);

        // A grouping level has to be orderable, not merely present: its keys are ordered and paged
        // in the database, which a navigation property cannot be.
        var grouping = query.GroupByColumns
            .FirstOrDefault(g => ExpressionCompiler.ResolveOrderableProperty<TModel>(g.ColumnName) is not null);

        return grouping is null
            ? await FlatAsync(filtered, query, cancellationToken)
            : await GroupedAsync(filtered, query, grouping, cancellationToken);
    }

    private static async Task<QueryResult<TModel>> FlatAsync<TModel>(
        IQueryable<TModel> filtered,
        Query query,
        CancellationToken cancellationToken)
    {
        var total = await filtered.CountAsync(cancellationToken);

        var models = await filtered
            .ApplySort(query)
            .ApplyPaging(query)
            .ApplyProjection(query)
            .ToListAsync(cancellationToken);

        var size = EffectiveSize(query.Paging);

        return new QueryResult<TModel>
        {
            Meta = new QueryResultMeta(new QueryResultMetaTotal(total, PageCount(total, size)), QueryResultType.Flat),
            Models = models
        };
    }

    /// <summary>
    /// Runs a grouped query. Paging applies to the outermost grouping level, so the keys are paged
    /// first and only then are the rows belonging to those groups fetched.
    /// </summary>
    private static Task<QueryResult<TModel>> GroupedAsync<TModel>(
        IQueryable<TModel> filtered,
        Query query,
        GroupByDescriptor grouping,
        CancellationToken cancellationToken)
    {
        var property = ExpressionCompiler.ResolveOrderableProperty<TModel>(grouping.ColumnName)!;

        // The key type is only known at run time, so the typed implementation is reached reflectively.
        var method = typeof(QueryForgeQueryableExtensions)
            .GetMethod(nameof(GroupedCoreAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TModel), property.PropertyType);

        return (Task<QueryResult<TModel>>)method.Invoke(
            null, [filtered, query, grouping, cancellationToken])!;
    }

    private static async Task<QueryResult<TModel>> GroupedCoreAsync<TModel, TKey>(
        IQueryable<TModel> filtered,
        Query query,
        GroupByDescriptor grouping,
        CancellationToken cancellationToken)
    {
        var selector = (Expression<Func<TModel, TKey>>)ExpressionCompiler.BuildSelector<TModel>(grouping.ColumnName)!;

        var keys = filtered.Select(selector).Distinct();

        var totalGroups = await keys.CountAsync(cancellationToken);

        var descending = grouping.SortOrder == SortOrder.Descending;

        // Grouping keys need the same null-placement guarantee the sort columns get, or a paged
        // grouped result would start on a different group depending on the database.
        var keyNullRank = ExpressionCompiler.BuildKeyNullRank<TKey>(nullsFirst: !descending);

        IOrderedQueryable<TKey> orderedKeys;

        if (keyNullRank is not null)
        {
            orderedKeys = descending
                ? keys.OrderBy(keyNullRank).ThenByDescending(k => k)
                : keys.OrderBy(keyNullRank).ThenBy(k => k);
        }
        else
        {
            orderedKeys = descending ? keys.OrderByDescending(k => k) : keys.OrderBy(k => k);
        }

        var size = EffectiveSize(query.Paging);
        var number = query.Paging.Number > 0 ? query.Paging.Number : 1;

        var pagedKeys = await orderedKeys
            .Skip((number - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var meta = new QueryResultMeta(
            new QueryResultMetaTotal(totalGroups, PageCount(totalGroups, size)),
            QueryResultType.Grouped);

        if (pagedKeys.Count == 0)
            return new QueryResult<TModel> { Meta = meta, Groups = Array.Empty<HierarchyNode<TModel>>() };

        // Every row of the paged groups is needed, because a node's count is the number of leaf rows
        // beneath it. The page size bounds the number of groups, not the rows inside them.
        var parameter = selector.Parameters[0];
        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(TKey)],
            Expression.Constant(pagedKeys),
            selector.Body);

        var groups = query.GroupByColumns
            .Where(g => ExpressionCompiler.ResolveOrderableProperty<TModel>(g.ColumnName) is not null)
            .ToList();

        // Grouping columns survive the projection: the hierarchy is rebuilt from them after the rows
        // come back, so dropping them would leave nothing to group by.
        var rows = await filtered
            .Where(Expression.Lambda<Func<TModel, bool>>(contains, parameter))
            .ApplySort(query)
            .ApplyProjection(query, groups.Select(g => g.ColumnName).ToList())
            .ToListAsync(cancellationToken);

        return new QueryResult<TModel>
        {
            Meta = meta,
            Groups = HierarchyBuilder.Build(rows, groups)
        };
    }

    private static IQueryable<TModel> ApplyOrdering<TModel>(
        IQueryable<TModel> source,
        LambdaExpression selector,
        string method)
        => (IQueryable<TModel>)typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == method && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TModel), selector.ReturnType)
            .Invoke(null, [source, selector])!;

    private static int EffectiveSize(QueryPaging paging) => paging.Size > 0 ? paging.Size : 12;

    private static int PageCount(int total, int size) => size <= 0 ? 0 : (int)Math.Ceiling(total / (double)size);
}
