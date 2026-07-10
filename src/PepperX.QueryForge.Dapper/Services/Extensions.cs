using System.Data;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Extension methods for IDbConnection to execute QueryForge commands.
/// </summary>
public static class DapperQueryForgeConnectionExtensions
{
    /// <summary>
    /// Executes a READ-ONLY QueryForge query and returns the strongly-typed result.
    /// </summary>
    public static async Task<QueryResult<TModel>> QueryForgeAsync<TModel>(
        this IDbConnection connection,
        DapperQuery query,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        var registry = DapperEngine.Registry;
        var provider = registry.Resolve(connection);

        return await provider.QueryAsync<TModel>(
            connection,
            query,
            registry.Options,
            commandTimeout,
            transaction);
    }
}