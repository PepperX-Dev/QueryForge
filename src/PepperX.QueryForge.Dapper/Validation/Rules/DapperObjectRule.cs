namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Internal rule model for validating the SQL Object metadata (Schema, Table Name).
/// </summary>
public class DapperObjectRule
{
    internal HashSet<string> AllowedSchemas { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal HashSet<string> DeniedSchemas { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal HashSet<string> AllowedTables { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal HashSet<string> DeniedTables { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal bool RequireName { get; set; } = true;
}

/// <summary>
/// Fluent builder for configuring <see cref="DapperObjectRule"/>.
/// </summary>
public class DapperObjectRuleBuilder
{
    private readonly DapperObjectRule _rule;
    internal DapperObjectRuleBuilder(DapperObjectRule rule) => _rule = rule;

    /// <summary>Adds schemas to the strict allowlist.</summary>
    public DapperObjectRuleBuilder AllowSchema(params string[] schemas) { foreach (var s in schemas) _rule.AllowedSchemas.Add(s); return this; }

    /// <summary>Adds schemas to the strict denylist.</summary>
    public DapperObjectRuleBuilder DenySchema(params string[] schemas) { foreach (var s in schemas) _rule.DeniedSchemas.Add(s); return this; }

    /// <summary>Adds table/object names to the strict allowlist.</summary>
    public DapperObjectRuleBuilder AllowTable(params string[] tables) { foreach (var t in tables) _rule.AllowedTables.Add(t); return this; }

    /// <summary>Adds table/object names to the strict denylist.</summary>
    public DapperObjectRuleBuilder DenyTable(params string[] tables) { foreach (var t in tables) _rule.DeniedTables.Add(t); return this; }

    /// <summary>Specifies whether the Object Name is strictly required. Defaults to true.</summary>
    public DapperObjectRuleBuilder RequireName(bool require = true) { _rule.RequireName = require; return this; }
}