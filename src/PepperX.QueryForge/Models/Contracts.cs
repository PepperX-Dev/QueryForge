namespace PepperX.QueryForge;

/// <summary>
/// A shared contract for models that target a specific database column or property.
/// Used by the validation engine to generically check column names across different descriptors.
/// </summary>
public interface IColumnDescriptor
{
    /// <summary>The name of the column or property being targeted.</summary>
    string ColumnName { get; }
}