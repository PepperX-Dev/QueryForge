namespace PepperX.QueryForge;

/// <summary>
/// Represents the complete filtering criteria for a query, composed of multiple condition groups.
/// </summary>
public record QueryCriteria
{
    /// <summary>The collection of condition groups. Guaranteed to be non-null.</summary>
    public IReadOnlyList<ConditionGroup> Groups { get; init; }

    /// <summary>The logical operator used to join the different groups together.</summary>
    public Logic Logic { get; init; }

    /// <summary>
    /// Standard explicit constructor to guarantee Groups is never null.
    /// </summary>
    public QueryCriteria(IReadOnlyList<ConditionGroup>? groups = null, Logic logic = Logic.And)
    {
        Groups = groups ?? Array.Empty<ConditionGroup>();
        Logic = logic;
    }
}