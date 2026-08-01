using System.Text.Json;

namespace PepperX.QueryForge.Querying;

/// <summary>
/// The shared rules that decide how a <see cref="QueryCriteria"/> turns into a predicate.
/// </summary>
/// <remarks>
/// Every execution provider — SQL generation, in-memory evaluation, EF Core expression trees —
/// routes through these helpers so that the same <see cref="Query"/> filters identically no matter
/// where it runs. Changing a rule here changes it everywhere at once, which is the point.
/// </remarks>
public static class ConditionSemantics
{
    /// <summary>
    /// The operator used to join the conditions <em>inside</em> a group, derived from the group's
    /// <see cref="Logic"/>. <see cref="Logic.Or"/> and <see cref="Logic.OrNot"/> join with OR;
    /// everything else joins with AND.
    /// </summary>
    public static bool IsDisjunction(Logic logic) => logic is Logic.Or or Logic.OrNot;

    /// <summary>
    /// Whether a group's result is negated, derived from the group's <see cref="Logic"/>.
    /// <see cref="Logic.AndNot"/> and <see cref="Logic.OrNot"/> negate; the others do not.
    /// </summary>
    /// <remarks>
    /// Negation is meaningful on a group only. At the <see cref="QueryCriteria.Logic"/> level the
    /// NOT suffix is ignored — criteria logic just joins groups together.
    /// </remarks>
    public static bool IsNegated(Logic logic) => logic is Logic.AndNot or Logic.OrNot;

    /// <summary>
    /// Whether a condition contributes a predicate at all.
    /// </summary>
    /// <remarks>
    /// A condition is skipped when its value is missing, because a filter the caller did not fill in
    /// must not silently become a match-nothing clause. The exceptions are
    /// <see cref="ConditionOperator.Equals"/> and <see cref="ConditionOperator.NotEquals"/>, where a
    /// null value is a deliberate IS NULL / IS NOT NULL test, and
    /// <see cref="ConditionOperator.Between"/>, which additionally requires
    /// <see cref="Condition.ValueTo"/>.
    /// </remarks>
    public static bool IsExecutable(Condition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (string.IsNullOrWhiteSpace(condition.ColumnName))
            return false;

        if (!Enum.IsDefined(condition.Operator))
            return false;

        var value = Unwrap(condition.Value);

        if (condition.Operator is ConditionOperator.Equals or ConditionOperator.NotEquals)
            return true;

        if (value is null)
            return false;

        if (condition.Operator is ConditionOperator.Between)
            return Unwrap(condition.ValueTo) is not null;

        return true;
    }

    /// <summary>Whether the operator performs a text match and therefore needs LIKE escaping.</summary>
    public static bool IsPatternOperator(ConditionOperator op) => op
        is ConditionOperator.Contains
        or ConditionOperator.NotContains
        or ConditionOperator.StartsWith
        or ConditionOperator.EndsWith;

    /// <summary>
    /// Converts a value that arrived as JSON into a plain CLR value.
    /// </summary>
    /// <remarks>
    /// <see cref="Condition.Value"/> is typed as <see cref="object"/>, so when a
    /// <see cref="Query"/> is model-bound from an HTTP request body it arrives as
    /// <see cref="JsonElement"/> rather than a primitive. Unwrapping here means providers never have
    /// to think about where the query came from.
    /// </remarks>
    public static object? Unwrap(object? value)
    {
        if (value is not JsonElement element)
            return value is DBNull ? null : value;

        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            _ => element.GetRawText()
        };
    }
}
