using System.Data;
using FluentAssertions;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper.Tests.Services;

public class DapperQueryServiceTests
{
    private sealed class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Fact]
    public async Task QueryAsync_WithoutConnection_AndNoFactory_ShouldExplainHowToFixIt()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions { ConnectionFactory = null });
        var service = new DapperQueryService(registry, new FakeServiceProvider());

        var query = DapperQueryBuilder.ForObject("Users").Build();

        var act = async () => await service.QueryAsync<object>(query);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ConnectionFactory was not configured*")
            .WithMessage("*takes an IDbConnection*");
    }

    [Fact]
    public async Task QueryAsync_WithAnUnsupportedConnection_ShouldThrow()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());
        var service = new DapperQueryService(registry, new FakeServiceProvider());

        var query = DapperQueryBuilder.ForObject("Users").Build();

        var act = async () => await service.QueryAsync<object>(new FirebirdConnection(), query);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    private sealed class FirebirdConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 0;
        public string Database => "";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Dispose() { }
        public void Open() { }
    }
}
