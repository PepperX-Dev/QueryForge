namespace PepperX.QueryForge.Dapper;

/// <summary>Defines how the library interacts with the database schema.</summary>
public enum DapperExecutionApproach
{
    /// <summary>Executes the query as raw, dynamic SQL directly. Does not require SP creation permissions.</summary>
    RawQuery,

    /// <summary>Assumes the Stored Procedure already exists and executes it. Ideal for strict read-only environments.</summary>
    UseReadySp,

    /// <summary>Automatically creates/alters the SP on startup, then uses it. Requires DDL permissions. (Default)</summary>
    DevelopAndUseSp
}

/// <summary>
/// Defines the supported database engines for the Dapper execution provider.
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
    Oracle
}