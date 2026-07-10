using System.Data;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// The injectable service for executing QueryForge READ operations via Dapper.
/// </summary>
public interface IDapperQueryService
{
    /// <summary>
    /// Queries the database using an existing, externally managed database connection.
    /// </summary>
    Task<QueryResult<TModel>> QueryAsync<TModel>(IDbConnection connection, DapperQuery query, int? commandTimeout = null, IDbTransaction? transaction = null);

    /// <summary>
    /// Queries the database by automatically creating and disposing a connection 
    /// using the ConnectionFactory configured in AddQueryForgeDapper().
    /// </summary>
    Task<QueryResult<TModel>> QueryAsync<TModel>(DapperQuery query, int? commandTimeout = null, IDbTransaction? transaction = null);
}