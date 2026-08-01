namespace PepperX.QueryForge.Querying;

/// <summary>
/// Applies a <see cref="Query"/> to an in-memory sequence, producing the same
/// <see cref="QueryResult{TModel}"/> shape a database-backed provider would.
/// </summary>
/// <remarks>
/// Two callers rely on this: the standalone in-memory provider, and the stored-procedure path of
/// the SQL providers — a stored procedure's result set cannot be composed into a larger SQL
/// statement portably, so the rows are materialized and filtered here instead.
/// </remarks>
public static class InMemoryQueryEngine
{
    /// <summary>
    /// Filters, sorts, pages and optionally groups <paramref name="source"/> according to
    /// <paramref name="query"/>.
    /// </summary>
    /// <typeparam name="TModel">The domain model type.</typeparam>
    /// <param name="source">The rows to query.</param>
    /// <param name="query">The query intent.</param>
    /// <param name="valueAccessor">
    /// Reads a column value from a row. Defaults to a cached, case-insensitive property lookup.
    /// </param>
    public static QueryResult<TModel> Apply<TModel>(
        IEnumerable<TModel> source,
        Query query,
        Func<TModel, string, object?>? valueAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        var accessor = valueAccessor ?? PropertyAccessor.For<TModel>();

        // With the default accessor the model's properties are the whitelist, matching how the SQL
        // providers use the result set and how the EF Core provider uses the entity. A caller who
        // supplies their own accessor owns that decision, so every column is taken at face value.
        Func<string, bool> columnExists = valueAccessor is null
            ? PropertyAccessor.Exists<TModel>
            : static _ => true;

        var filtered = source.Where(row => Matches(row, query.Criteria, accessor, columnExists));
        var sorted = Sort(filtered, query.SortColumns, accessor).ToList();

        var size = query.Paging.Size > 0 ? query.Paging.Size : 12;
        var number = query.Paging.Number > 0 ? query.Paging.Number : 1;
        var skip = (number - 1) * size;

        var groups = query.GroupByColumns.Where(g => columnExists(g.ColumnName)).ToList();

        return groups.Count > 0
            ? BuildGrouped(sorted, query, groups, accessor, size, skip)
            : BuildFlat(sorted, query, size, skip);
    }

    private static QueryResult<TModel> BuildFlat<TModel>(List<TModel> rows, Query query, int size, int skip)
    {
        var page = rows.Skip(skip).Take(size).ToList();

        return new QueryResult<TModel>
        {
            Meta = new QueryResultMeta(
                new QueryResultMetaTotal(rows.Count, PageCount(rows.Count, size)),
                QueryResultType.Flat),
            Models = ProjectionShaper.ShapeAll(page, query.SelectColumns)
        };
    }

    private static QueryResult<TModel> BuildGrouped<TModel>(
        List<TModel> rows,
        Query query,
        IReadOnlyList<GroupByDescriptor> groups,
        Func<TModel, string, object?> accessor,
        int size,
        int skip)
    {
        // Paging applies to the outermost grouping level, so the totals describe groups, not rows.
        var outer = groups[0];

        var distinctKeys = rows
            .Select(row => accessor(row, outer.ColumnName))
            .Distinct(LooseEqualityComparer.Instance)
            .ToList();

        var orderedKeys = outer.SortOrder == SortOrder.Descending
            ? distinctKeys.OrderByDescending(k => k, QueryValueComparer.Instance)
            : distinctKeys.OrderBy(k => k, QueryValueComparer.Instance);

        var pagedKeys = orderedKeys.Skip(skip).Take(size).ToList();
        var keySet = new HashSet<object?>(pagedKeys, LooseEqualityComparer.Instance);

        var pagedRows = rows
            .Where(row => keySet.Contains(accessor(row, outer.ColumnName)))
            .ToList();

        return new QueryResult<TModel>
        {
            Meta = new QueryResultMeta(
                new QueryResultMetaTotal(distinctKeys.Count, PageCount(distinctKeys.Count, size)),
                QueryResultType.Grouped),
            // Grouping columns survive the projection, because the hierarchy is rebuilt from them
            // and because the SQL providers force them into the SELECT list for the same reason.
            Groups = ProjectionShaper.ShapeHierarchy(
                HierarchyBuilder.Build(pagedRows, groups, accessor),
                query.SelectColumns,
                groups.Select(g => g.ColumnName).ToList())
        };
    }

    private static int PageCount(int total, int size) => size <= 0 ? 0 : (int)Math.Ceiling(total / (double)size);

    #region Filtering

    /// <summary>
    /// Evaluates a full <see cref="QueryCriteria"/> against one row, following the same group
    /// connector and negation rules the SQL providers use.
    /// </summary>
    public static bool Matches<TModel>(
        TModel row,
        QueryCriteria criteria,
        Func<TModel, string, object?> accessor,
        Func<string, bool>? columnExists = null)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(accessor);

        columnExists ??= _ => true;

        var criteriaIsOr = ConditionSemantics.IsDisjunction(criteria.Logic);
        bool? result = null;
        var sawGroup = false;

        foreach (var group in criteria.Groups)
        {
            var conditions = group.Conditions
                .Where(c => ConditionSemantics.IsExecutable(c) && columnExists(c.ColumnName))
                .ToList();

            if (conditions.Count == 0)
                continue;

            var groupIsOr = ConditionSemantics.IsDisjunction(group.Logic);

            // Seeded with the identity of the operator being folded: true for AND, false for OR.
            bool? groupResult = !groupIsOr;

            foreach (var condition in conditions)
            {
                var value = Evaluate(row, condition, accessor);
                groupResult = groupIsOr ? Or(groupResult, value) : And(groupResult, value);
            }

            if (ConditionSemantics.IsNegated(group.Logic))
                groupResult = Not(groupResult);

            result = sawGroup
                ? criteriaIsOr ? Or(result, groupResult) : And(result, groupResult)
                : groupResult;

            sawGroup = true;
        }

        // No usable conditions means no filter at all, which must match everything. Anything that
        // came out unknown did not match, which is how SQL treats a WHERE clause evaluating to NULL.
        return !sawGroup || result == true;
    }

    #region Three-valued logic

    // SQL evaluates a comparison against NULL as unknown rather than false, and unknown survives
    // negation. Modelling that here — with null standing for unknown — is what lets a negated group
    // behave the same in memory as it does in the database.

    private static bool? And(bool? left, bool? right)
    {
        if (left == false || right == false) return false;
        if (left is null || right is null) return null;

        return true;
    }

    private static bool? Or(bool? left, bool? right)
    {
        if (left == true || right == true) return true;
        if (left is null || right is null) return null;

        return false;
    }

    private static bool? Not(bool? value) => value is null ? null : !value;

    #endregion

    /// <summary>
    /// Evaluates one condition, returning <see langword="null"/> for SQL's unknown.
    /// </summary>
    /// <remarks>
    /// Only <see cref="ConditionOperator.Equals"/> and <see cref="ConditionOperator.NotEquals"/>
    /// against a null value give a definite answer — those are IS NULL and IS NOT NULL tests. Every
    /// other operator applied to a null column is unknown, which keeps a null row out of the result
    /// whether or not its group is negated.
    /// </remarks>
    private static bool? Evaluate<TModel>(TModel row, Condition condition, Func<TModel, string, object?> accessor)
    {
        var actual = ConditionSemantics.Unwrap(accessor(row, condition.ColumnName));
        var expected = ConditionSemantics.Unwrap(condition.Value);

        switch (condition.Operator)
        {
            case ConditionOperator.Equals when expected is null:
                return actual is null;

            case ConditionOperator.NotEquals when expected is null:
                return actual is not null;
        }

        if (actual is null)
            return null;

        switch (condition.Operator)
        {
            case ConditionOperator.Equals:
                return QueryValueComparer.Instance.AreEqual(actual, expected);

            case ConditionOperator.NotEquals:
                return !QueryValueComparer.Instance.AreEqual(actual, expected);

            case ConditionOperator.Contains:
                return Text(actual).Contains(Text(expected), StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.NotContains:
                return !Text(actual).Contains(Text(expected), StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.StartsWith:
                return Text(actual).StartsWith(Text(expected), StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.EndsWith:
                return Text(actual).EndsWith(Text(expected), StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.LessThan:
                return QueryValueComparer.Instance.Compare(actual, expected) < 0;

            case ConditionOperator.GreaterThan:
                return QueryValueComparer.Instance.Compare(actual, expected) > 0;

            case ConditionOperator.LessThanOrEqualTo:
                return QueryValueComparer.Instance.Compare(actual, expected) <= 0;

            case ConditionOperator.GreaterThanOrEqualTo:
                return QueryValueComparer.Instance.Compare(actual, expected) >= 0;

            case ConditionOperator.Between:
                var upper = ConditionSemantics.Unwrap(condition.ValueTo);

                if (upper is null)
                    return null;

                return QueryValueComparer.Instance.Compare(actual, expected) >= 0
                    && QueryValueComparer.Instance.Compare(actual, upper) <= 0;

            default:
                return null;
        }
    }

    private static string Text(object? value) => value?.ToString() ?? string.Empty;

    #endregion

    #region Sorting

    private static IEnumerable<TModel> Sort<TModel>(
        IEnumerable<TModel> source,
        IReadOnlyList<SortDescriptor> sorts,
        Func<TModel, string, object?> accessor)
    {
        if (sorts.Count == 0)
            return source;

        IOrderedEnumerable<TModel>? ordered = null;

        foreach (var sort in sorts)
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

    #endregion

    private sealed class LooseEqualityComparer : IEqualityComparer<object?>
    {
        public static readonly LooseEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => QueryValueComparer.Instance.AreEqual(x, y);

        public int GetHashCode(object? obj)
        {
            // Deliberately weak: values that compare equal across types (1 vs 1L vs "1") must land
            // in the same bucket, so equality does the real work.
            var unwrapped = ConditionSemantics.Unwrap(obj);

            return unwrapped switch
            {
                null => 0,
                string s => s.ToUpperInvariant().GetHashCode(),
                _ => 1
            };
        }
    }
}
