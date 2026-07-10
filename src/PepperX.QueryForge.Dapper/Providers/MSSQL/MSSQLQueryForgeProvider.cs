using System.Data;
using Dapper;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper.Providers.MSSQL;

/// <summary>
/// The SQL Server implementation of the Dapper QueryForge provider.
/// </summary>
internal class MSSQLQueryForgeProvider : IDapperQueryForgeProvider
{
    public DapperDatabaseProvider ProviderType => DapperDatabaseProvider.MSSQL;

    public async Task InitializeAsync(IDbConnection connection, DapperQueryForgeOptions options, CancellationToken ct = default)
    {
        if (connection.State != ConnectionState.Open)
            connection.Open();

        await connection.ExecuteAsync(Constants.Scripts.Initialization.usp_QueryForge_BuildWhere);
        await connection.ExecuteAsync(Constants.Scripts.Initialization.usp_QueryForge_ExecGrouped);
        await connection.ExecuteAsync(Constants.Scripts.Initialization.usp_QueryForge);
    }

    public async Task<QueryResult<TModel>> QueryAsync<TModel>(
        IDbConnection connection,
        DapperQuery query,
        DapperQueryForgeOptions options,
        int? commandTimeout = null,
        IDbTransaction? transaction = null)
    {
        var parameters = MSSQLParameterMapper.Map(query);

        MSSQLResultParser.RawQueryForgeResult? rawResult;

        if (options.Approach == DapperExecutionApproach.RawQuery)
        {
            rawResult = await connection.QueryFirstAsync<MSSQLResultParser.RawQueryForgeResult>(Constants.Scripts.RawQuery, parameters, commandTimeout: commandTimeout, transaction: transaction, commandType: CommandType.Text);

            return MSSQLResultParser.Parse<TModel>(rawResult);
        }

        rawResult = await connection.QueryFirstOrDefaultAsync<MSSQLResultParser.RawQueryForgeResult>(Constants.Scripts.StoredProcedureName, parameters, commandTimeout: commandTimeout, commandType: CommandType.StoredProcedure);

        return MSSQLResultParser.Parse<TModel>(rawResult);
    }
}