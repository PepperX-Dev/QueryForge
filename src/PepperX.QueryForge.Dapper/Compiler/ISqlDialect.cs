namespace PepperX.QueryForge.Dapper.Compiler;

/// <summary>
/// The database-specific vocabulary <see cref="SqlQueryCompiler"/> needs in order to emit SQL.
/// </summary>
/// <remarks>
/// Everything structural — which conditions apply, how groups combine, how paging maps onto
/// grouping levels — lives in the compiler and is shared by every database. A dialect supplies only
/// the handful of things that genuinely differ between engines, which is why adding a database is a
/// small class rather than a reimplementation.
/// </remarks>
public interface ISqlDialect
{
    /// <summary>The database engine this dialect targets.</summary>
    DapperDatabaseProvider ProviderType { get; }

    /// <summary>The schema assumed when a query object does not name one.</summary>
    string DefaultSchema { get; }

    /// <summary>Wraps an identifier so that reserved words and unusual names are safe to emit.</summary>
    /// <remarks>
    /// Implementations must reject or neutralize the dialect's own closing delimiter. The compiler
    /// only ever passes identifiers that survived the column whitelist, but this is the last line of
    /// defence and should behave as one.
    /// </remarks>
    string QuoteIdentifier(string identifier);

    /// <summary>Renders a parameter reference, for example <c>@p0</c> or <c>:p0</c>.</summary>
    string ParameterReference(string name);

    /// <summary>The clause that limits a result to one page, appended after ORDER BY.</summary>
    string PagingClause(int offset, int size);

    /// <summary>
    /// Whether <see cref="PagingClause"/> is only valid when an ORDER BY is present. SQL Server and
    /// Oracle require one; the compiler emits <see cref="OrderByFallback"/> when nothing else sorts.
    /// </summary>
    bool RequiresOrderByForPaging { get; }

    /// <summary>An expression that satisfies ORDER BY without imposing a meaningful order.</summary>
    string OrderByFallback { get; }

    /// <summary>
    /// The clause that pins where nulls sort, including a leading space, or empty when the engine's
    /// default already matches.
    /// </summary>
    /// <remarks>
    /// Engines disagree: PostgreSQL and Oracle sort nulls last ascending, while SQL Server, MySQL and
    /// SQLite sort them first. QueryForge standardises on nulls-first ascending and nulls-last
    /// descending, so the same query returns the same order everywhere; a dialect whose default
    /// already does that returns an empty string.
    /// </remarks>
    string NullOrdering(SortOrder order);

    /// <summary>Escapes LIKE wildcards in a value so it matches literally.</summary>
    string EscapeLikeValue(string value);

    /// <summary>The ESCAPE clause matching <see cref="EscapeLikeValue"/>, including a leading space.</summary>
    string LikeEscapeClause { get; }

    /// <summary>Whether this engine can select from a table-valued function.</summary>
    bool SupportsTableValuedFunctions { get; }

    /// <summary>Whether this engine can execute a stored procedure that returns a result set.</summary>
    bool SupportsStoredProcedures { get; }

    /// <summary>
    /// Renders the FROM source for a table, view, or table-valued function.
    /// </summary>
    /// <param name="schema">The resolved schema name.</param>
    /// <param name="name">The object name.</param>
    /// <param name="type">The declared object type.</param>
    /// <param name="argumentReferences">
    /// Parameter references for a table-valued function's arguments, in the order supplied by the
    /// caller. Empty for tables and views.
    /// </param>
    string BuildSource(string schema, string name, DapperObjectType type, IReadOnlyList<string> argumentReferences);

    /// <summary>
    /// Renders the statement that invokes a stored procedure.
    /// </summary>
    /// <param name="schema">The resolved schema name.</param>
    /// <param name="name">The procedure name.</param>
    /// <param name="argumentNames">The caller's parameter names, paired with their references.</param>
    string BuildStoredProcedureCall(
        string schema,
        string name,
        IReadOnlyList<KeyValuePair<string, string>> argumentNames);
}
