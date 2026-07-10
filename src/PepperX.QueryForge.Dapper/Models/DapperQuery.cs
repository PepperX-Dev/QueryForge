namespace PepperX.QueryForge.Dapper;

/// <summary>
/// The Dapper-specific query object. Inherits all base query intents and adds SQL object metadata.
/// </summary>
public class DapperQuery : PepperX.QueryForge.Query
{
    /// <summary>
    /// The target SQL object (Table, View, SP, etc.) and its parameters.
    /// Nullable because it must be explicitly set via the Builder or manually before execution.
    /// </summary>
    public DapperQueryObject? Object { get; set; }
}