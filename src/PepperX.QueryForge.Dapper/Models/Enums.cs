namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Defines the type of SQL object being queried in the Dapper provider.
/// </summary>
public enum DapperObjectType
{
    /// <summary>Automatically detect the object type.</summary>
    Auto = 0,
    /// <summary>A standard database table.</summary>
    Table = 1,
    /// <summary>A database view.</summary>
    View = 2,
    /// <summary>A Table-Valued Function.</summary>
    TVF = 3,
    /// <summary>A Stored Procedure.</summary>
    SP = 4
}