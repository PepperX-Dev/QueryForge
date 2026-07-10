namespace PepperX.QueryForge;

/// <summary>The root configuration object for query validation rules.</summary>
public class QueryValidationRules
{
    internal QueryPagingRule Paging { get; } = new();
    internal QueryColumnRule SelectColumns { get; } = new();
    internal QueryColumnRule SortColumns { get; } = new();
    internal QueryColumnRule GroupByColumns { get; } = new();
    internal QueryColumnRule ConditionColumns { get; } = new();

    /// <summary>Configures pagination limits.</summary>
    public QueryValidationRules PageSize(Action<QueryPagingRuleBuilder> configure) { configure(new QueryPagingRuleBuilder(Paging)); return this; }

    /// <summary>Configures allowed/denied columns for the SELECT clause.</summary>
    public QueryValidationRules Select(Action<QueryColumnRuleBuilder> configure) { configure(new QueryColumnRuleBuilder(SelectColumns)); return this; }

    /// <summary>Configures allowed/denied columns for the ORDER BY clause.</summary>
    public QueryValidationRules Sort(Action<QueryColumnRuleBuilder> configure) { configure(new QueryColumnRuleBuilder(SortColumns)); return this; }

    /// <summary>Configures allowed/denied columns for the GROUP BY clause.</summary>
    public QueryValidationRules GroupBy(Action<QueryColumnRuleBuilder> configure) { configure(new QueryColumnRuleBuilder(GroupByColumns)); return this; }

    /// <summary>Configures allowed/denied columns for the WHERE (Criteria) clause.</summary>
    public QueryValidationRules Where(Action<QueryColumnRuleBuilder> configure) { configure(new QueryColumnRuleBuilder(ConditionColumns)); return this; }
}