namespace PepperX.QueryForge.Dapper;

/// <summary>
/// The SQL object a query targets, and any arguments it takes.
/// </summary>
/// <param name="Name">The table, view, table-valued function, or stored procedure name.</param>
/// <param name="Schema">
/// The schema. Left empty, each dialect supplies its own default — <c>dbo</c> on SQL Server,
/// <c>public</c> on PostgreSQL, and none on MySQL or Oracle, which have no equivalent layer.
/// </param>
/// <param name="Type">
/// The kind of object. <see cref="DapperObjectType.Auto"/> means a table or view; functions and
/// procedures must say so, because they are invoked differently.
/// </param>
/// <param name="Parameters">
/// Arguments for a function or procedure. Table-valued function arguments are positional and are
/// passed in the order given here.
/// </param>
public record DapperQueryObject(
    string Name,
    string Schema = "",
    DapperObjectType Type = DapperObjectType.Auto,
    IReadOnlyDictionary<string, object?>? Parameters = null
);
