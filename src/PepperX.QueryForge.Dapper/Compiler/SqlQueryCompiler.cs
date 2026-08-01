using System.Text;
using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.Dapper.Compiler;

/// <summary>
/// Turns a <see cref="DapperQuery"/> into parameterized SQL for a given <see cref="ISqlDialect"/>.
/// </summary>
/// <remarks>
/// <para>
/// Two rules govern everything here.
/// </para>
/// <para>
/// <strong>Values are never inlined.</strong> Every caller-supplied value becomes a parameter, so
/// the database sees a stable statement it can cache a plan for, and comparisons use the column's
/// real type instead of degrading into string comparison.
/// </para>
/// <para>
/// <strong>Identifiers are never trusted.</strong> Column names arrive from callers and are checked
/// against the whitelist discovered from the live result set before they are emitted. A name that is
/// not on the list is dropped rather than escaped, which is what stops a query being used to probe
/// the schema.
/// </para>
/// </remarks>
public sealed class SqlQueryCompiler(ISqlDialect dialect)
{
    private readonly ISqlDialect _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));

    /// <summary>The dialect this compiler emits for.</summary>
    public ISqlDialect Dialect => _dialect;

    /// <summary>
    /// A statement that returns no rows, used once per object to discover its real column names.
    /// </summary>
    public CompiledSql CompileSchemaProbe(DapperQuery query)
    {
        var context = new CompilationContext(_dialect);
        var source = BuildSource(query, context);

        return context.Complete($"SELECT * FROM {source} WHERE 1 = 0");
    }

    /// <summary>Compiles the page of rows for an ungrouped query.</summary>
    public CompiledSql CompileRows(DapperQuery query, ColumnWhitelist columns)
    {
        var context = new CompilationContext(_dialect);
        var source = BuildSource(query, context);
        var where = BuildWhere(query.Criteria, columns, context);
        var orderBy = BuildOrderBy(query.SortColumns, columns);

        var sql = new StringBuilder()
            .Append("SELECT ").Append(BuildProjection(query.SelectColumns, columns))
            .Append(" FROM ").Append(source);

        AppendWhere(sql, where);
        AppendOrderByAndPaging(sql, orderBy, query.Paging);

        return context.Complete(sql.ToString());
    }

    /// <summary>Compiles the total row count for an ungrouped query.</summary>
    public CompiledSql CompileRowCount(DapperQuery query, ColumnWhitelist columns)
    {
        var context = new CompilationContext(_dialect);
        var source = BuildSource(query, context);
        var where = BuildWhere(query.Criteria, columns, context);

        var sql = new StringBuilder().Append("SELECT COUNT(*) FROM ").Append(source);
        AppendWhere(sql, where);

        return context.Complete(sql.ToString());
    }

    /// <summary>
    /// Compiles the page of outermost group keys. Paging applies to groups, not rows, so this is
    /// what the page size actually slices.
    /// </summary>
    public CompiledSql CompileGroupKeys(DapperQuery query, ColumnWhitelist columns)
    {
        var outer = RequireOuterGroup(query, columns);

        var context = new CompilationContext(_dialect);
        var source = BuildSource(query, context);
        var where = BuildWhere(query.Criteria, columns, context);
        var keyColumn = _dialect.QuoteIdentifier(outer.ColumnName);

        var sql = new StringBuilder()
            .Append("SELECT DISTINCT ").Append(keyColumn)
            .Append(" FROM ").Append(source);

        AppendWhere(sql, where);

        AppendOrderByAndPaging(sql, OrderTerm(outer.ColumnName, outer.SortOrder), query.Paging);

        return context.Complete(sql.ToString());
    }

    /// <summary>Compiles the count of distinct outermost group keys.</summary>
    public CompiledSql CompileGroupCount(DapperQuery query, ColumnWhitelist columns)
    {
        var outer = RequireOuterGroup(query, columns);

        var context = new CompilationContext(_dialect);
        var source = BuildSource(query, context);
        var where = BuildWhere(query.Criteria, columns, context);

        var inner = new StringBuilder()
            .Append("SELECT DISTINCT ").Append(_dialect.QuoteIdentifier(outer.ColumnName))
            .Append(" FROM ").Append(source);

        AppendWhere(inner, where);

        // The alias is deliberately written without AS: Oracle rejects AS for a table alias, while
        // every other engine accepts the bare form, so this one spelling works everywhere.
        return context.Complete($"SELECT COUNT(*) FROM ({inner}) qf_groups");
    }

    /// <summary>
    /// Compiles every row belonging to the supplied page of group keys.
    /// </summary>
    /// <remarks>
    /// All rows for the paged groups are fetched, because a node's count is the number of leaf rows
    /// beneath it and that cannot be known from a partial set. The page size bounds how many groups
    /// come back, not how many rows each contains.
    /// </remarks>
    public CompiledSql CompileGroupRows(DapperQuery query, ColumnWhitelist columns, IReadOnlyList<object?> groupKeys)
    {
        ArgumentNullException.ThrowIfNull(groupKeys);

        var outer = RequireOuterGroup(query, columns);

        var context = new CompilationContext(_dialect);
        var source = BuildSource(query, context);
        var where = BuildWhere(query.Criteria, columns, context);
        var keyColumn = _dialect.QuoteIdentifier(outer.ColumnName);

        var keyPredicate = BuildKeyPredicate(keyColumn, groupKeys, context);

        var combined = where is null ? keyPredicate : $"({where}) AND {keyPredicate}";

        var sql = new StringBuilder()
            .Append("SELECT ").Append(BuildProjection(query.SelectColumns, columns, query.GroupByColumns))
            .Append(" FROM ").Append(source)
            .Append(" WHERE ").Append(combined);

        // Leaf ordering comes from SortColumns; the hierarchy builder orders the group keys itself.
        var orderBy = BuildOrderBy(query.SortColumns, columns);

        if (orderBy is not null)
            sql.Append(" ORDER BY ").Append(orderBy);

        return context.Complete(sql.ToString());
    }

    #region Structure

    private GroupByDescriptor RequireOuterGroup(DapperQuery query, ColumnWhitelist columns)
    {
        var outer = query.GroupByColumns.FirstOrDefault(g => columns.Contains(g.ColumnName));

        return outer ?? throw new InvalidOperationException(
            "A grouped query needs at least one GroupByColumn naming a real column of the target object.");
    }

    private string BuildSource(DapperQuery query, CompilationContext context)
    {
        var target = query.Object ?? throw new InvalidOperationException(
            "Query.Object must be specified before executing a query. Use ForObject(...) on the builder to set it.");

        if (string.IsNullOrWhiteSpace(target.Name))
            throw new InvalidOperationException("Query.Object.Name must be a non-empty object name.");

        var schema = string.IsNullOrWhiteSpace(target.Schema) ? _dialect.DefaultSchema : target.Schema;

        if (target.Type == DapperObjectType.TVF)
        {
            if (!_dialect.SupportsTableValuedFunctions)
            {
                throw new NotSupportedException(
                    $"{_dialect.ProviderType} has no table-valued functions, so DapperObjectType.TVF cannot be used with it.");
            }

            // Arguments are positional, in the order the caller supplied them.
            var references = (target.Parameters ?? new Dictionary<string, object?>())
                .Select(p => context.AddValue(p.Value))
                .ToList();

            return _dialect.BuildSource(schema, target.Name, target.Type, references);
        }

        if (target.Type == DapperObjectType.SP)
        {
            throw new InvalidOperationException(
                "Stored procedures are executed directly rather than composed into a SELECT. " +
                "This is handled by the execution path, not the compiler.");
        }

        return _dialect.BuildSource(schema, target.Name, target.Type, Array.Empty<string>());
    }

    private string BuildProjection(
        IReadOnlyList<string> selectColumns,
        ColumnWhitelist columns,
        IReadOnlyList<GroupByDescriptor>? alsoInclude = null)
    {
        var chosen = selectColumns.Where(columns.Contains).ToList();

        if (chosen.Count == 0)
            return "*";

        // A grouped query still needs its grouping columns in the rows, otherwise the hierarchy
        // cannot be rebuilt from what comes back.
        if (alsoInclude is not null)
        {
            foreach (var group in alsoInclude)
            {
                if (columns.Contains(group.ColumnName)
                    && !chosen.Contains(group.ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    chosen.Add(group.ColumnName);
                }
            }
        }

        return string.Join(", ", chosen.Select(_dialect.QuoteIdentifier));
    }

    private string? BuildOrderBy(IReadOnlyList<SortDescriptor> sorts, ColumnWhitelist columns)
    {
        var clauses = sorts
            .Where(s => columns.Contains(s.ColumnName))
            .Select(s => OrderTerm(s.ColumnName, s.SortOrder))
            .ToList();

        return clauses.Count == 0 ? null : string.Join(", ", clauses);
    }

    /// <summary>
    /// Renders one ORDER BY term, including where nulls sort.
    /// </summary>
    /// <remarks>
    /// Engines disagree on this by default — PostgreSQL and Oracle put nulls last ascending, while
    /// SQL Server, MySQL and SQLite put them first — so the dialect states it explicitly and every
    /// engine returns the same order for the same query.
    /// </remarks>
    private string OrderTerm(string columnName, SortOrder order)
        => _dialect.QuoteIdentifier(columnName)
           + (order == SortOrder.Descending ? " DESC" : " ASC")
           + _dialect.NullOrdering(order);

    private static void AppendWhere(StringBuilder sql, string? where)
    {
        if (where is not null)
            sql.Append(" WHERE ").Append(where);
    }

    private void AppendOrderByAndPaging(StringBuilder sql, string? orderBy, QueryPaging paging)
    {
        var size = paging.Size > 0 ? paging.Size : 12;
        var number = paging.Number > 0 ? paging.Number : 1;
        var offset = (number - 1) * size;

        if (orderBy is not null)
            sql.Append(" ORDER BY ").Append(orderBy);
        else if (_dialect.RequiresOrderByForPaging)
            sql.Append(" ORDER BY ").Append(_dialect.OrderByFallback);

        sql.Append(' ').Append(_dialect.PagingClause(offset, size));
    }

    private string BuildKeyPredicate(string keyColumn, IReadOnlyList<object?> groupKeys, CompilationContext context)
    {
        var hasNull = groupKeys.Any(k => k is null);
        var present = groupKeys.Where(k => k is not null).ToList();

        if (present.Count == 0)
            return hasNull ? $"{keyColumn} IS NULL" : "1 = 0";

        var references = present.Select(context.AddValue);
        var inClause = $"{keyColumn} IN ({string.Join(", ", references)})";

        return hasNull ? $"({inClause} OR {keyColumn} IS NULL)" : inClause;
    }

    #endregion

    #region Predicates

    private string? BuildWhere(QueryCriteria criteria, ColumnWhitelist columns, CompilationContext context)
    {
        var sql = new StringBuilder();

        foreach (var group in criteria.Groups)
        {
            var fragments = new List<string>();

            foreach (var condition in group.Conditions)
            {
                if (!ConditionSemantics.IsExecutable(condition) || !columns.Contains(condition.ColumnName))
                    continue;

                var fragment = BuildCondition(condition, columns, context);

                if (fragment is not null)
                    fragments.Add(fragment);
            }

            if (fragments.Count == 0)
                continue;

            var joiner = ConditionSemantics.IsDisjunction(group.Logic) ? " OR " : " AND ";
            var piece = $"({string.Join(joiner, fragments)})";

            if (ConditionSemantics.IsNegated(group.Logic))
                piece = "NOT " + piece;

            if (sql.Length > 0)
                sql.Append(ConditionSemantics.IsDisjunction(criteria.Logic) ? " OR " : " AND ");

            sql.Append(piece);
        }

        return sql.Length == 0 ? null : sql.ToString();
    }

    private string? BuildCondition(Condition condition, ColumnWhitelist columns, CompilationContext context)
    {
        var column = _dialect.QuoteIdentifier(condition.ColumnName);
        var columnType = columns.TypeOf(condition.ColumnName);
        var value = Coerce(condition.Value, columnType);

        switch (condition.Operator)
        {
            case ConditionOperator.Equals:
                return value is null ? $"{column} IS NULL" : $"{column} = {context.AddValue(value)}";

            case ConditionOperator.NotEquals:
                return value is null ? $"{column} IS NOT NULL" : $"{column} <> {context.AddValue(value)}";

            case ConditionOperator.LessThan:
                return $"{column} < {context.AddValue(value)}";

            case ConditionOperator.GreaterThan:
                return $"{column} > {context.AddValue(value)}";

            case ConditionOperator.LessThanOrEqualTo:
                return $"{column} <= {context.AddValue(value)}";

            case ConditionOperator.GreaterThanOrEqualTo:
                return $"{column} >= {context.AddValue(value)}";

            case ConditionOperator.Between:
                var upper = Coerce(condition.ValueTo, columnType);
                return $"{column} BETWEEN {context.AddValue(value)} AND {context.AddValue(upper)}";

            case ConditionOperator.Contains:
                return Like(column, value, prefixWildcard: true, suffixWildcard: true, negate: false, context);

            case ConditionOperator.NotContains:
                return Like(column, value, prefixWildcard: true, suffixWildcard: true, negate: true, context);

            case ConditionOperator.StartsWith:
                return Like(column, value, prefixWildcard: false, suffixWildcard: true, negate: false, context);

            case ConditionOperator.EndsWith:
                return Like(column, value, prefixWildcard: true, suffixWildcard: false, negate: false, context);

            default:
                return null;
        }
    }

    /// <summary>
    /// Converts a caller's value to the column's type, so a strict engine does not reject the
    /// comparison. A value that cannot represent that type is passed through unchanged and simply
    /// matches nothing, which is what an unusable filter should do.
    /// </summary>
    private static object? Coerce(object? value, Type? columnType)
    {
        var unwrapped = ConditionSemantics.Unwrap(value);

        if (columnType is null || unwrapped is null)
            return unwrapped;

        return ValueCoercion.TryCoerce(unwrapped, columnType, out var coerced) ? coerced : unwrapped;
    }

    /// <summary>
    /// Builds a LIKE predicate. The caller's value is escaped so that wildcards inside it match
    /// literally — only the wildcards this method adds are meant to be significant.
    /// </summary>
    private string Like(
        string column,
        object? value,
        bool prefixWildcard,
        bool suffixWildcard,
        bool negate,
        CompilationContext context)
    {
        var escaped = _dialect.EscapeLikeValue(value?.ToString() ?? string.Empty);
        var pattern = (prefixWildcard ? "%" : string.Empty) + escaped + (suffixWildcard ? "%" : string.Empty);
        var reference = context.AddValue(pattern);
        var op = negate ? "NOT LIKE" : "LIKE";

        return $"{column} {op} {reference}{_dialect.LikeEscapeClause}";
    }

    #endregion

    /// <summary>Accumulates parameters while a single statement is being built.</summary>
    private sealed class CompilationContext(ISqlDialect dialect)
    {
        private readonly Dictionary<string, object?> _parameters = new();

        /// <summary>Registers a value and returns the reference to use in the SQL text.</summary>
        public string AddValue(object? value)
        {
            var name = "p" + _parameters.Count;
            _parameters[name] = ConditionSemantics.Unwrap(value);

            return dialect.ParameterReference(name);
        }

        public CompiledSql Complete(string text) => new(text, _parameters);
    }
}
