using PepperX.QueryForge.Dapper.Internals;
using System.Data;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// The default implementation of <see cref="IDapperQueryService"/>.
/// </summary>
internal class DapperQueryService : IDapperQueryService
{
    private readonly DapperRegistry _registry;
    private readonly IServiceProvider _serviceProvider;

    public DapperQueryService(DapperRegistry registry, IServiceProvider serviceProvider)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
    }

    public async Task<QueryResult<TModel>> QueryAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        var provider = _registry.Resolve(connection);

        return await provider.QueryAsync<TModel>(
            connection,
            query,
            _registry.Options,
            commandTimeout,
            transaction: transaction);
    }

    public async Task<QueryResult<TModel>> QueryAsync<TModel>(
        DapperQuery query,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        var options = _registry.Options;

        if (options.ConnectionFactory == null)
        {
            throw new InvalidOperationException(
                "Cannot auto-manage connections because DapperQueryForgeOptions.ConnectionFactory was not configured in AddQueryForgeDapper().");
        }

        using var connection = options.ConnectionFactory(_serviceProvider);

        if (connection.State != ConnectionState.Open)
            connection.Open();

        return await QueryAsync<TModel>(connection, query, commandTimeout, transaction: transaction);
    }
}
