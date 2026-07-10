namespace PepperX.QueryForge;

/// <summary>Internal rule model for pagination limits.</summary>
public class QueryPagingRule
{
    internal int? MaxSize { get; set; }
    internal int? MinSize { get; set; }
}

/// <summary>Fluent builder for configuring <see cref="QueryPagingRule"/>.</summary>
public class QueryPagingRuleBuilder
{
    private readonly QueryPagingRule _rule;
    internal QueryPagingRuleBuilder(QueryPagingRule rule) => _rule = rule;

    /// <summary>Sets the maximum allowed page size.</summary>
    public QueryPagingRuleBuilder Max(int max) { _rule.MaxSize = max; return this; }

    /// <summary>Sets the minimum allowed page size.</summary>
    public QueryPagingRuleBuilder Min(int min) { _rule.MinSize = min; return this; }
}