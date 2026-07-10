namespace PepperX.QueryForge;

/// <summary>Internal rule model for column allow/deny lists.</summary>
public class QueryColumnRule
{
    // Case-insensitive for better developer experience
    internal HashSet<string> Allowed { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal HashSet<string> Denied { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Fluent builder for configuring <see cref="QueryColumnRule"/>.</summary>
public class QueryColumnRuleBuilder
{
    private readonly QueryColumnRule _rule;
    internal QueryColumnRuleBuilder(QueryColumnRule rule) => _rule = rule;

    /// <summary>Adds columns to the strict allowlist.</summary>
    public QueryColumnRuleBuilder Allow(params string[] columns) { foreach (var c in columns) _rule.Allowed.Add(c); return this; }

    /// <summary>Adds columns to the strict denylist.</summary>
    public QueryColumnRuleBuilder Deny(params string[] columns) { foreach (var c in columns) _rule.Denied.Add(c); return this; }
}