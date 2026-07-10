namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Represents the SQL object metadata and parameters for a Dapper query.
/// </summary>
public record DapperQueryObject(
    /// <summary>The name of the table, view, TVF, or stored procedure.</summary>
    string Name,
    /// <summary>The database schema. Defaults to "dbo".</summary>
    string Schema = "dbo",
    /// <summary>The explicit type of the SQL object. Defaults to Auto.</summary>
    DapperObjectType Type = DapperObjectType.Auto,
    /// <summary>Parameters to pass to the SQL object (e.g., SP parameters or TVF arguments).</summary>
    IReadOnlyDictionary<string, object?>? Parameters = null
);