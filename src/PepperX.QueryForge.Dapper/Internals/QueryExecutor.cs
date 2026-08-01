using System.Data;
using Dapper;
using PepperX.QueryForge.Dapper.Compiler;
using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// Runs a compiled query against a connection and assembles the <see cref="QueryResult{TModel}"/>.
/// </summary>
/// <remarks>
/// Ungrouped queries take two statements: one COUNT and one page of rows. Grouped queries take
/// three, because paging applies to the outermost grouping level rather than to rows — the counts
/// and the key page describe groups, and only then are the rows for those groups fetched.
/// </remarks>
internal sealed class QueryExecutor(SqlQueryCompiler compiler, SchemaCache schemaCache)
{
    public async Task<QueryResult<TModel>> QueryAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        int? commandTimeout,
        IDbTransaction? transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(query);

        if (query.Object?.Type == DapperObjectType.SP)
            return await QueryStoredProcedureAsync<TModel>(connection, query, commandTimeout, transaction);

        var columns = await schemaCache.GetAsync(connection, query, commandTimeout, transaction);

        var groups = query.GroupByColumns.Where(g => columns.Contains(g.ColumnName)).ToList();

        return groups.Count > 0
            ? await QueryGroupedAsync<TModel>(connection, query, columns, groups, commandTimeout, transaction)
            : await QueryFlatAsync<TModel>(connection, query, columns, commandTimeout, transaction);
    }

    private async Task<QueryResult<TModel>> QueryFlatAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        ColumnWhitelist columns,
        int? commandTimeout,
        IDbTransaction? transaction)
    {
        var countSql = compiler.CompileRowCount(query, columns);
        var total = await connection.ExecuteScalarAsync<int>(
            countSql.Text, SchemaCache.ToDapperParameters(countSql), transaction, commandTimeout);

        var rowsSql = compiler.CompileRows(query, columns);
        var rows = await connection.QueryAsync<TModel>(
            rowsSql.Text, SchemaCache.ToDapperParameters(rowsSql), transaction, commandTimeout);

        var size = EffectiveSize(query.Paging);

        return new QueryResult<TModel>
        {
            Meta = new QueryResultMeta(new QueryResultMetaTotal(total, PageCount(total, size)), QueryResultType.Flat),
            Models = rows.ToList()
        };
    }

    private async Task<QueryResult<TModel>> QueryGroupedAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        ColumnWhitelist columns,
        IReadOnlyList<GroupByDescriptor> groups,
        int? commandTimeout,
        IDbTransaction? transaction)
    {
        var countSql = compiler.CompileGroupCount(query, columns);
        var totalGroups = await connection.ExecuteScalarAsync<int>(
            countSql.Text, SchemaCache.ToDapperParameters(countSql), transaction, commandTimeout);

        var keysSql = compiler.CompileGroupKeys(query, columns);
        var keyRows = await connection.QueryAsync(
            keysSql.Text, SchemaCache.ToDapperParameters(keysSql), transaction, commandTimeout);

        // Each row of the key query has exactly one column: the outermost group key.
        var keys = keyRows
            .Cast<IDictionary<string, object?>>()
            .Select(r => r.Values.FirstOrDefault())
            .ToList();

        var size = EffectiveSize(query.Paging);
        var meta = new QueryResultMeta(
            new QueryResultMetaTotal(totalGroups, PageCount(totalGroups, size)),
            QueryResultType.Grouped);

        if (keys.Count == 0)
            return new QueryResult<TModel> { Meta = meta, Groups = Array.Empty<HierarchyNode<TModel>>() };

        var rowsSql = compiler.CompileGroupRows(query, columns, keys);
        var rows = await connection.QueryAsync<TModel>(
            rowsSql.Text, SchemaCache.ToDapperParameters(rowsSql), transaction, commandTimeout);

        return new QueryResult<TModel>
        {
            Meta = meta,
            Groups = HierarchyBuilder.Build(rows.ToList(), groups)
        };
    }

    /// <summary>
    /// Executes a stored procedure and applies the query to its result set in memory.
    /// </summary>
    /// <remarks>
    /// A procedure's result set cannot be composed into a larger SELECT in a way that works across
    /// engines, so filtering, sorting, paging and grouping happen after materialization. The rules
    /// applied are the same ones the SQL path uses, because both go through
    /// <see cref="ConditionSemantics"/>.
    /// </remarks>
    private async Task<QueryResult<TModel>> QueryStoredProcedureAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        int? commandTimeout,
        IDbTransaction? transaction)
    {
        var dialect = compiler.Dialect;

        if (!dialect.SupportsStoredProcedures)
        {
            throw new NotSupportedException(
                $"{dialect.ProviderType} cannot return a result set from a stored procedure, " +
                "so DapperObjectType.SP is not available for it.");
        }

        var target = query.Object!;
        var schema = string.IsNullOrWhiteSpace(target.Schema) ? dialect.DefaultSchema : target.Schema;
        var supplied = target.Parameters ?? new Dictionary<string, object?>();

        var values = new Dictionary<string, object?>();
        var references = new List<KeyValuePair<string, string>>();
        var index = 0;

        foreach (var (name, value) in supplied)
        {
            var parameterName = "p" + index++;
            values[parameterName] = ConditionSemantics.Unwrap(value);
            references.Add(new KeyValuePair<string, string>(name, dialect.ParameterReference(parameterName)));
        }

        var sql = dialect.BuildStoredProcedureCall(schema, target.Name, references);

        var rows = await connection.QueryAsync<TModel>(
            sql,
            new QueryForgeParameters(new CompiledSql(sql, values)),
            transaction,
            commandTimeout,
            CommandType.Text);

        return InMemoryQueryEngine.Apply(rows.ToList(), query);
    }

    private static int EffectiveSize(QueryPaging paging) => paging.Size > 0 ? paging.Size : 12;

    private static int PageCount(int total, int size) => size <= 0 ? 0 : (int)Math.Ceiling(total / (double)size);
}
