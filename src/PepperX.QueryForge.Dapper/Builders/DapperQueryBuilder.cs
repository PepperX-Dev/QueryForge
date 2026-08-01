using PepperX.QueryForge.Dapper;

/// <summary>
/// A static entry point for constructing Dapper <see cref="DapperQuery"/> instances.
/// </summary>
public static class DapperQueryBuilder
{
    /// <summary>Starts a new, empty Dapper query building chain.</summary>
    public static DapperQueryFluent New() => new(new DapperQuery());

    /// <summary>Starts a new, empty Dapper query building chain.</summary>
    public static DapperQueryFluent New(DapperQuery dapperQuery) => new(dapperQuery);

    /// <summary>
    /// Upgrades a base, provider-agnostic Query into a Dapper-specific query builder.
    /// This allows the frontend to send the base Query, and the backend to securely append the DapperQueryObject.
    /// </summary>
    public static DapperQueryFluent FromBase(PepperX.QueryForge.Query baseQuery) => New(new DapperQuery { Criteria = baseQuery.Criteria, Paging = baseQuery.Paging, SelectColumns = baseQuery.SelectColumns, SortColumns = baseQuery.SortColumns, GroupByColumns = baseQuery.GroupByColumns });

    /// <summary>Starts a new chain by specifying the target SQL object.</summary>
    public static DapperQueryFluent ForObject(string name, string schema = "", DapperObjectType type = DapperObjectType.Auto, IReadOnlyDictionary<string, object?>? parameters = null)
        => New().ForObject(name, schema, type, parameters);

    /// <summary>Starts a new chain by specifying columns to select.</summary>
    public static DapperQueryFluent Select(params string[] columns) => New().Select(columns);

    /// <summary>Starts a new chain by specifying the filtering criteria.</summary>
    public static DapperQueryFluent Where(PepperX.QueryForge.QueryCriteria criteria) => New().Where(criteria);

    /// <summary>Starts a new chain by specifying sort descriptors.</summary>
    public static DapperQueryFluent Sort(params PepperX.QueryForge.SortDescriptor[] sorts) => New().Sort(sorts);

    /// <summary>Starts a new chain by specifying group-by descriptors.</summary>
    public static DapperQueryFluent GroupBy(params PepperX.QueryForge.GroupByDescriptor[] groups) => New().GroupBy(groups);

    /// <summary>Starts a new chain by specifying pagination parameters.</summary>
    public static DapperQueryFluent Page(int size, int number = 1) => New().Page(size, number);
}

/// <summary>
/// The instance-based fluent builder for Dapper DapperQuery. 
/// Inherits base methods and overrides them to return <see cref="DapperQueryFluent"/> for seamless chaining.
/// </summary>
public class DapperQueryFluent : PepperX.QueryForge.QueryFluent
{
    private readonly DapperQuery _dapperQuery;

    /// <summary>
    /// Initializes the builder with a Dapper-specific DapperQuery instance.
    /// </summary>
    public DapperQueryFluent(DapperQuery query) : base(query)
    {
        _dapperQuery = query;
    }

    /// <summary>Specifies the target SQL object and its parameters.</summary>
    public DapperQueryFluent ForObject(string name, string schema = "", DapperObjectType type = DapperObjectType.Auto, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        _dapperQuery.Object = new DapperQueryObject(name, schema, type, parameters);
        return this;
    }

    // Override base methods to return Dapper's DapperQueryFluent instead of the base DapperQueryFluent
    /// <inheritdoc/>
    public new DapperQueryFluent Select(params string[] columns) { base.Select(columns); return this; }

    /// <inheritdoc/>
    public new DapperQueryFluent Where(PepperX.QueryForge.QueryCriteria criteria) { base.Where(criteria); return this; }

    /// <inheritdoc/>
    public new DapperQueryFluent Sort(params PepperX.QueryForge.SortDescriptor[] sorts) { base.Sort(sorts); return this; }

    /// <inheritdoc/>
    public new DapperQueryFluent GroupBy(params PepperX.QueryForge.GroupByDescriptor[] groups) { base.GroupBy(groups); return this; }

    /// <inheritdoc/>
    public new DapperQueryFluent Page(int size, int number = 1) { base.Page(size, number); return this; }

    /// <summary>
    /// Upgrades a base, provider-agnostic Query into a Dapper-specific query builder.
    /// This allows the frontend to send the base Query, and the backend to securely append the DapperQueryObject.
    /// </summary>
    public static DapperQueryFluent FromBase(PepperX.QueryForge.Query baseQuery)
    {
        var dapperQuery = new DapperQuery
        {
            Criteria = baseQuery.Criteria,
            Paging = baseQuery.Paging,
            SelectColumns = baseQuery.SelectColumns,
            SortColumns = baseQuery.SortColumns,
            GroupByColumns = baseQuery.GroupByColumns
        };

        return new DapperQueryFluent(dapperQuery);
    }

    /// <summary>Builds and returns the final Dapper <see cref="DapperQuery"/> instance.</summary>
    public new DapperQuery Build() => _dapperQuery;
}