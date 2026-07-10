namespace PepperX.QueryForge;

/// <summary>Defines the logical operators used to combine conditions or groups.</summary>
public enum Logic { And, Or, AndNot, OrNot }

/// <summary>Defines the sorting direction for a specific column.</summary>
public enum SortOrder { Ascending, Descending }

/// <summary>Defines the comparison operators available for filtering data.</summary>
public enum ConditionOperator
{
    Equals, NotEquals, Contains, NotContains,
    StartsWith, EndsWith, LessThan, GreaterThan,
    LessThanOrEqualTo, GreaterThanOrEqualTo, Between
}

/// <summary>Defines the structural shape of the query result returned by the execution provider.</summary>
public enum QueryResultType { Flat, Grouped }