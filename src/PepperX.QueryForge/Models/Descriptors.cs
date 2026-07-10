namespace PepperX.QueryForge;

/// <summary>Defines a column to sort the results by, and the direction of the sort.</summary>
public record SortDescriptor(string ColumnName, SortOrder SortOrder = SortOrder.Ascending) : IColumnDescriptor;

/// <summary>Defines a column to group the results by, creating a hierarchical tree structure.</summary>
public record GroupByDescriptor(string ColumnName, SortOrder SortOrder = SortOrder.Ascending) : IColumnDescriptor;