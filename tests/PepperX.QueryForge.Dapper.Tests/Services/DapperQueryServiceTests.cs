using System.Data;
using FluentAssertions;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper.Tests.Services;

public class DapperQueryServiceTests
{
    private class SqlConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 0;
        public string Database => "";
        public ConnectionState State { get; set; } = ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Dispose() { }
        public void Open() { State = ConnectionState.Open; }
    }

    private class FakeProvider : IDapperQueryForgeProvider
    {
        public DapperDatabaseProvider ProviderType => DapperDatabaseProvider.MSSQL;
        public Task InitializeAsync(IDbConnection connection, DapperQueryForgeOptions options, CancellationToken ct = default) => Task.CompletedTask;

        public Task<QueryResult<TModel>> QueryAsync<TModel>(IDbConnection connection, DapperQuery query, DapperQueryForgeOptions options, int? commandTimeout = null, CancellationToken ct = default)
        {
            return Task.FromResult(new QueryResult<TModel> { Meta = new QueryResultMeta(new QueryResultMetaTotal(99, 1), QueryResultType.Flat) });
        }

        public Task<QueryResult<TModel>> QueryAsync<TModel>(IDbConnection connection, DapperQuery query, DapperQueryForgeOptions options, int? commandTimeout = null, IDbTransaction? transaction = null)
        {
            return Task.FromResult(new QueryResult<TModel> { Meta = new QueryResultMeta(new QueryResultMetaTotal(99, 1), QueryResultType.Flat) });
        }
    }

    [Fact]
    public async Task QueryAsync_WithConnection_ShouldDelegateToProvider()
    {
        var options = new DapperQueryForgeOptions();
        var registry = new DapperRegistry(options);
        registry.Register(new FakeProvider());

        var serviceProvider = new FakeServiceProvider();
        var service = new DapperQueryService(registry, serviceProvider);

        var query = DapperQueryBuilder.New().ForObject("Users").Build();
        var connection = new SqlConnection();

        var result = await service.QueryAsync<object>(connection, query);

        result.Meta.Total.Rows.Should().Be(99);
    }

    [Fact]
    public async Task QueryAsync_WithoutConnection_ShouldUseConnectionFactory()
    {
        var options = new DapperQueryForgeOptions
        {
            ConnectionFactory = sp => new SqlConnection()
        };
        var registry = new DapperRegistry(options);
        registry.Register(new FakeProvider());

        var serviceProvider = new FakeServiceProvider();
        var service = new DapperQueryService(registry, serviceProvider);

        var query = DapperQueryBuilder.New().ForObject("Users").Build();

        var result = await service.QueryAsync<object>(query);

        result.Meta.Total.Rows.Should().Be(99);
    }

    [Fact]
    public async Task QueryAsync_WithoutConnection_AndNoFactory_ShouldThrow()
    {
        var options = new DapperQueryForgeOptions { ConnectionFactory = null };
        var registry = new DapperRegistry(options);
        var serviceProvider = new FakeServiceProvider();
        var service = new DapperQueryService(registry, serviceProvider);

        var query = DapperQueryBuilder.New().ForObject("Users").Build();

        var act = async () => await service.QueryAsync<object>(query);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ConnectionFactory was not configured*");
    }

    // Minimal IServiceProvider implementation for testing
    private class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}