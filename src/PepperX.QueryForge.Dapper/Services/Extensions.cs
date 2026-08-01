using System.Data;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Extension methods for running QueryForge queries straight off an <see cref="IDbConnection"/>.
/// </summary>
public static class DapperQueryForgeConnectionExtensions
{
    /// <summary>
    /// Executes a read-only QueryForge query and returns the strongly-typed result.
    /// </summary>
    /// <remarks>
    /// Useful when you would rather not take a service dependency. It uses the dialects registered
    /// by <c>AddQueryForgeDapper</c>, so that call still has to have happened at startup.
    /// </remarks>
    public static Task<QueryResult<TModel>> QueryForgeAsync<TModel>(
        this IDbConnection connection,
        DapperQuery query,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
        => DapperEngine.Registry.Resolve(connection).QueryAsync<TModel>(connection, query, commandTimeout, transaction);
}
