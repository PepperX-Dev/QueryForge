using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.InMemory;

/// <summary>
/// Runs a QueryForge <see cref="Query"/> against any in-memory sequence.
/// </summary>
/// <remarks>
/// Same <see cref="Query"/> in, same <see cref="QueryResult{TModel}"/> out as the database providers —
/// against a list you already hold rather than a table. Useful for cached reference data, for results
/// composed from several services, and for tests that need a provider without a database.
/// </remarks>
public static class InMemoryQueryExtensions
{
    /// <summary>
    /// Filters, sorts, pages, projects and optionally groups the sequence.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="source">The rows to query.</param>
    /// <param name="query">The query intent.</param>
    /// <param name="valueAccessor">
    /// Optional. Reads a column value from a row. Defaults to a cached, case-insensitive property
    /// lookup, which covers ordinary POCOs. Supply your own to query dictionaries, dynamic rows, or
    /// models whose property names differ from the column names the caller uses.
    /// </param>
    public static QueryResult<TModel> ToQueryResult<TModel>(
        this IEnumerable<TModel> source,
        Query query,
        Func<TModel, string, object?>? valueAccessor = null)
        => InMemoryQueryEngine.Apply(source, query, valueAccessor);

    /// <summary>
    /// Async wrapper over <see cref="ToQueryResult{TModel}"/>, for call sites that are already async.
    /// </summary>
    /// <remarks>
    /// The work is synchronous — this exists so an in-memory source can stand in for a database
    /// provider without reshaping the calling code, which is what makes it useful in tests.
    /// </remarks>
    public static Task<QueryResult<TModel>> ToQueryResultAsync<TModel>(
        this IEnumerable<TModel> source,
        Query query,
        Func<TModel, string, object?>? valueAccessor = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(InMemoryQueryEngine.Apply(source, query, valueAccessor));
    }

    /// <summary>Applies only the filtering criteria, leaving the sequence lazy.</summary>
    public static IEnumerable<TModel> ApplyFilter<TModel>(
        this IEnumerable<TModel> source,
        Query query,
        Func<TModel, string, object?>? valueAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var accessor = valueAccessor ?? PropertyAccessor.For<TModel>();

        Func<string, bool> columnExists = valueAccessor is null
            ? PropertyAccessor.Exists<TModel>
            : static _ => true;

        return source.Where(row => InMemoryQueryEngine.Matches(row, query.Criteria, accessor, columnExists));
    }

    /// <summary>Applies only the sort columns, in order.</summary>
    public static IEnumerable<TModel> ApplySort<TModel>(
        this IEnumerable<TModel> source,
        Query query,
        Func<TModel, string, object?>? valueAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var accessor = valueAccessor ?? PropertyAccessor.For<TModel>();

        IOrderedEnumerable<TModel>? ordered = null;

        foreach (var sort in query.SortColumns)
        {
            var column = sort.ColumnName;

            if (ordered is null)
            {
                ordered = sort.SortOrder == SortOrder.Descending
                    ? source.OrderByDescending(r => accessor(r, column), QueryValueComparer.Instance)
                    : source.OrderBy(r => accessor(r, column), QueryValueComparer.Instance);
            }
            else
            {
                ordered = sort.SortOrder == SortOrder.Descending
                    ? ordered.ThenByDescending(r => accessor(r, column), QueryValueComparer.Instance)
                    : ordered.ThenBy(r => accessor(r, column), QueryValueComparer.Instance);
            }
        }

        return ordered ?? source;
    }

    /// <summary>Applies only the paging window.</summary>
    public static IEnumerable<TModel> ApplyPaging<TModel>(this IEnumerable<TModel> source, Query query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var size = query.Paging.Size > 0 ? query.Paging.Size : 12;
        var number = query.Paging.Number > 0 ? query.Paging.Number : 1;

        return source.Skip((number - 1) * size).Take(size);
    }

    /// <summary>Applies only the column projection.</summary>
    public static IEnumerable<TModel> ApplyProjection<TModel>(this IEnumerable<TModel> source, Query query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        return query.SelectColumns.Count == 0
            ? source
            : source.Select(row => ProjectionShaper.Shape(row, query.SelectColumns));
    }

    /// <summary>
    /// Applies filtering, sorting, paging and projection, leaving the sequence lazy.
    /// </summary>
    /// <remarks>
    /// Grouping is not applied here, because a hierarchy is a shape rather than a sequence. Use
    /// <see cref="ToQueryResult{TModel}"/> for grouped queries.
    /// </remarks>
    public static IEnumerable<TModel> ApplyQuery<TModel>(
        this IEnumerable<TModel> source,
        Query query,
        Func<TModel, string, object?>? valueAccessor = null)
        => source
            .ApplyFilter(query, valueAccessor)
            .ApplySort(query, valueAccessor)
            .ApplyPaging(query)
            .ApplyProjection(query);
}
