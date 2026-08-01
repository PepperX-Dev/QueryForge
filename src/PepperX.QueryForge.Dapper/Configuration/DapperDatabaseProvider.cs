namespace PepperX.QueryForge.Dapper;

/// <summary>
/// The database engines the Dapper execution provider can target.
/// </summary>
public enum DapperDatabaseProvider
{
    /// <summary>Microsoft SQL Server.</summary>
    MSSQL,

    /// <summary>MySQL or MariaDB.</summary>
    MySQL,

    /// <summary>PostgreSQL.</summary>
    PostgreSQL,

    /// <summary>Oracle Database.</summary>
    Oracle,

    /// <summary>SQLite.</summary>
    SQLite
}
