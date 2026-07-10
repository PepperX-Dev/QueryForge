namespace PepperX.QueryForge;

/// <summary>
/// Provides extension methods for validating and sanitizing <see cref="Query"/> instances.
/// </summary>
public static class QueryValidationExtensions
{
    /// <summary>
    /// Validates the query against the provided rules. 
    /// If mode is <see cref="QueryValidationMode.SilentStrip"/>, it mutates the current Query object in-place.
    /// If mode is <see cref="QueryValidationMode.ThrowException"/>, it throws a <see cref="QueryValidationException"/>.
    /// </summary>
    public static Query Validate(this Query query, Action<QueryValidationRules> configure, QueryValidationMode mode = QueryValidationMode.SilentStrip)
    {
        var rules = new QueryValidationRules();
        configure(rules);
        return Validate(query, rules, mode);
    }

    public static Query Validate(this Query query, QueryValidationRules rules, QueryValidationMode mode = QueryValidationMode.SilentStrip)
    {
        var errors = new List<string>();
        ApplyBaseRules(query, rules, errors, mode);
        ThrowIfErrors(errors, mode);
        return query;
    }

    public static void ApplyBaseRules(Query query, QueryValidationRules rules, List<string> errors, QueryValidationMode mode)
    {
        ApplyPagingRules(query, rules.Paging, errors, mode);
        ApplyStringColumnRules(query.SelectColumns, rules.SelectColumns, errors, mode, cols => query.SelectColumns = cols);
        ApplyColumnRules(query.SortColumns, rules.SortColumns, errors, mode, cols => query.SortColumns = cols);
        ApplyColumnRules(query.GroupByColumns, rules.GroupByColumns, errors, mode, cols => query.GroupByColumns = cols);
        ApplyCriteriaRules(query.Criteria, rules.ConditionColumns, errors, mode, crit => query.Criteria = crit);
    }

    public static void ThrowIfErrors(List<string> errors, QueryValidationMode mode)
    {
        if (errors.Count > 0 && mode == QueryValidationMode.ThrowException)
        {
            throw new QueryValidationException("Query validation failed. See InvalidProperties for details.", errors);
        }
    }

    #region Private Mutation Helpers

    private static void ApplyPagingRules(Query query, QueryPagingRule rules, List<string> errors, QueryValidationMode mode)
    {
        int newSize = query.Paging.Size;
        bool changed = false;

        if (rules.MaxSize.HasValue && newSize > rules.MaxSize.Value)
        {
            if (mode == QueryValidationMode.ThrowException) errors.Add($"Paging.Size > {rules.MaxSize.Value}");
            else { newSize = rules.MaxSize.Value; changed = true; }
        }
        if (rules.MinSize.HasValue && newSize < rules.MinSize.Value)
        {
            if (mode == QueryValidationMode.ThrowException) errors.Add($"Paging.Size < {rules.MinSize.Value}");
            else { newSize = rules.MinSize.Value; changed = true; }
        }

        if (changed) query.Paging = query.Paging with { Size = newSize };
    }

    private static void ApplyStringColumnRules(IReadOnlyList<string> current, QueryColumnRule rules, List<string> errors, QueryValidationMode mode, Action<IReadOnlyList<string>> setter)
    {
        var valid = new List<string>();
        foreach (var col in current)
        {
            bool isValid = (rules.Allowed.Count == 0 || rules.Allowed.Contains(col)) && !rules.Denied.Contains(col);
            if (isValid) valid.Add(col);
            else if (mode == QueryValidationMode.ThrowException) errors.Add(col);
        }

        if (valid.Count != current.Count) setter(valid);
    }

    private static void ApplyColumnRules<T>(IReadOnlyList<T> current, QueryColumnRule rules, List<string> errors, QueryValidationMode mode, Action<IReadOnlyList<T>> setter) where T : IColumnDescriptor
    {
        var valid = new List<T>();
        foreach (var item in current)
        {
            bool isValid = (rules.Allowed.Count == 0 || rules.Allowed.Contains(item.ColumnName)) && !rules.Denied.Contains(item.ColumnName);
            if (isValid) valid.Add(item);
            else if (mode == QueryValidationMode.ThrowException) errors.Add(item.ColumnName);
        }

        if (valid.Count != current.Count) setter(valid);
    }

    private static void ApplyCriteriaRules(QueryCriteria criteria, QueryColumnRule rules, List<string> errors, QueryValidationMode mode, Action<QueryCriteria> setter)
    {
        var validGroups = new List<ConditionGroup>();
        bool changed = false;

        foreach (var group in criteria.Groups)
        {
            var validConditions = new List<Condition>();
            foreach (var cond in group.Conditions)
            {
                bool isValid = (rules.Allowed.Count == 0 || rules.Allowed.Contains(cond.ColumnName)) && !rules.Denied.Contains(cond.ColumnName);
                if (isValid) validConditions.Add(cond);
                else if (mode == QueryValidationMode.ThrowException) errors.Add(cond.ColumnName);
                else changed = true;
            }

            if (validConditions.Count != group.Conditions.Count) changed = true;
            if (validConditions.Count > 0) validGroups.Add(group with { Conditions = validConditions });
        }

        if (changed) setter(criteria with { Groups = validGroups });
    }

    #endregion
}