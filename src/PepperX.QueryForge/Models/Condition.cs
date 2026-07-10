namespace PepperX.QueryForge;

/// <summary>Represents a single filtering condition applied to a specific column.</summary>
public record Condition(
    string ColumnName,
    ConditionOperator Operator,
    object? Value = null,
    object? ValueTo = null
);