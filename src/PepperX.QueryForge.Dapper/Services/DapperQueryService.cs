using System.Data;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// The default implementation of <see cref="IDapperQueryService"/>.
/// </summary>
internal sealed class DapperQueryService(DapperRegistry registry, IServiceProvider serviceProvider)
    : IDapperQueryService
{
    public Task<QueryResult<TModel>> QueryAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
        => registry.Resolve(connection).QueryAsync<TModel>(connection, query, commandTimeout, transaction);

    public async Task<QueryResult<TModel>> QueryAsync<TModel>(
        DapperQuery query,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        if (registry.Options.ConnectionFactory is null)
        {
            throw new InvalidOperationException(
                "Cannot manage a connection because DapperQueryForgeOptions.ConnectionFactory was not configured " +
                "in AddQueryForgeDapper(). Either configure it, or use the overload that takes an IDbConnection.");
        }

        using var connection = registry.Options.ConnectionFactory(serviceProvider);

        if (connection.State != ConnectionState.Open)
            connection.Open();

        return await QueryAsync<TModel>(connection, query, commandTimeout, transaction);
    }
}
