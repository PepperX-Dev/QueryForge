namespace PepperX.QueryForge;

/// <summary>
/// The master, provider-agnostic query intent object. 
/// It is implemented as a mutable class so that the Validation engine can strip 
/// invalid properties in-place when using SilentStrip mode.
/// All collection properties are initialized to empty arrays to prevent null reference exceptions.
/// </summary>
public class Query
{
    /// <summary>The filtering criteria to apply to the data source.</summary>
    public QueryCriteria Criteria { get; set; } = new();

    /// <summary>The pagination parameters (size and page number).</summary>
    public QueryPaging Paging { get; set; } = new();

    /// <summary>The specific columns/properties to include in the final result.</summary>
    public IReadOnlyList<string> SelectColumns { get; set; } = Array.Empty<string>();

    /// <summary>The columns and directions to sort the final result by.</summary>
    public IReadOnlyList<SortDescriptor> SortColumns { get; set; } = Array.Empty<SortDescriptor>();

    /// <summary>The columns to group the results by, creating a nested hierarchy.</summary>
    public IReadOnlyList<GroupByDescriptor> GroupByColumns { get; set; } = Array.Empty<GroupByDescriptor>();
}