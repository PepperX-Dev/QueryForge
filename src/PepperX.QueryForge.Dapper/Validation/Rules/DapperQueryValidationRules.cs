namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Extends the base validation rules with Dapper-specific SQL object rules.
/// </summary>
public class DapperQueryValidationRules : PepperX.QueryForge.QueryValidationRules
{
    internal DapperObjectRule ObjectRule { get; } = new();

    /// <summary>Configures validation rules for the target SQL object (Schema, Table name).</summary>
    public DapperQueryValidationRules Object(Action<DapperObjectRuleBuilder> configure)
    {
        configure(new DapperObjectRuleBuilder(ObjectRule));
        return this;
    }
}