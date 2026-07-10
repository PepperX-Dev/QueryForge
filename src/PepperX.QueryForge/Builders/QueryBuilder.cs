namespace PepperX.QueryForge;

/// <summary>
/// A static entry point for constructing <see cref="Query"/> instances using a fluent API.
/// Eliminates the need for the 'new' keyword, allowing syntax like: QueryBuilder.Select("Id").Build()
/// </summary>
public static class QueryBuilder
{
    /// <summary>Starts a new, empty query building chain.</summary>
    public static QueryFluent New() => new QueryFluent();

    /// <summary>Starts a new query building chain by specifying columns to select.</summary>
    public static QueryFluent Select(params string[] columns) => new QueryFluent().Select(columns);

    /// <summary>Starts a new query building chain by specifying the filtering criteria.</summary>
    public static QueryFluent Where(QueryCriteria criteria) => new QueryFluent().Where(criteria);

    /// <summary>Starts a new query building chain by specifying sort descriptors.</summary>
    public static QueryFluent Sort(params SortDescriptor[] sorts) => new QueryFluent().Sort(sorts);

    /// <summary>Starts a new query building chain by specifying group-by descriptors.</summary>
    public static QueryFluent GroupBy(params GroupByDescriptor[] groups) => new QueryFluent().GroupBy(groups);

    /// <summary>Starts a new query building chain by specifying pagination parameters.</summary>
    public static QueryFluent Page(int size, int number = 1) => new QueryFluent().Page(size, number);
}

/// <summary>
/// The instance-based fluent builder that handles the actual chaining and state accumulation.
/// Returned by the static <see cref="QueryBuilder"/> methods.
/// </summary>
public class QueryFluent
{
    protected readonly Query _query;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFluent"/> class with a fresh <see cref="Query"/>.
    /// Used by the static <see cref="QueryBuilder"/> entry points.
    /// </summary>
    public QueryFluent()
    {
        _query = new Query();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFluent"/> class with an existing <see cref="Query"/>.
    /// Used for inheritance (e.g., DapperQueryFluent passing a DapperQuery to the base).
    /// </summary>
    public QueryFluent(Query query)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
    }

    /// <summary>Specifies the columns to include in the result.</summary>
    public QueryFluent Select(params string[] columns) { _query.SelectColumns = columns; return this; }

    /// <summary>Sets the filtering criteria for the query.</summary>
    public QueryFluent Where(QueryCriteria criteria) { _query.Criteria = criteria; return this; }

    /// <summary>Specifies the columns and directions to sort the results by.</summary>
    public QueryFluent Sort(params SortDescriptor[] sorts) { _query.SortColumns = sorts; return this; }

    /// <summary>Specifies the columns to group the results by, creating a hierarchy.</summary>
    public QueryFluent GroupBy(params GroupByDescriptor[] groups) { _query.GroupByColumns = groups; return this; }

    /// <summary>Sets the pagination parameters (size and page number).</summary>
    public QueryFluent Page(int size, int number = 1) { _query.Paging = new QueryPaging(size, number); return this; }

    /// <summary>Builds and returns the final <see cref="Query"/> instance.</summary>
    public Query Build() => _query;
}