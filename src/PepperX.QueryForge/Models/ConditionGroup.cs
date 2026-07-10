namespace PepperX.QueryForge;

/// <summary>
/// Represents a logical group of conditions.
/// </summary>
public record ConditionGroup
{
    /// <summary>The collection of conditions within this group. Guaranteed to be non-null.</summary>
    public IReadOnlyList<Condition> Conditions { get; init; }

    /// <summary>The logical operator used to join the conditions inside this specific group.</summary>
    public Logic Logic { get; init; }

    /// <summary>
    /// Standard explicit constructor to guarantee Conditions is never null.
    /// </summary>
    public ConditionGroup(IReadOnlyList<Condition>? conditions = null, Logic logic = Logic.And)
    {
        Conditions = conditions ?? Array.Empty<Condition>();
        Logic = logic;
    }
}