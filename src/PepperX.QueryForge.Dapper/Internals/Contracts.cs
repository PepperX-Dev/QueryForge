using System.Data;

namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// The core Strategy interface. Every database-specific Dapper provider must implement this.
/// </summary>
internal interface IDapperQueryForgeProvider
{
    /// <summary>The database engine this provider targets.</summary>
    DapperDatabaseProvider ProviderType { get; }

    /// <summary>
    /// Runs initialization logic at application startup (e.g., creating Stored Procedures).
    /// </summary>
    Task InitializeAsync(IDbConnection connection, DapperQueryForgeOptions options, CancellationToken ct = default);

    /// <summary>
    /// Executes a READ-ONLY query against the database and returns the strongly-typed result.
    /// </summary>
    Task<QueryResult<TModel>> QueryAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        DapperQueryForgeOptions options,
        int? commandTimeout = null,
        IDbTransaction? transaction = null);
}