using System.Data;
using FluentAssertions;
using PepperX.QueryForge.Dapper;
using PepperX.QueryForge.Dapper.Internals;
using PepperX.QueryForge.Dapper.Providers.MSSQL;
using Xunit;

namespace PepperX.QueryForge.Dapper.Tests.Internals;

public class DapperRegistryTests
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

    [Fact]
    public void Resolve_ByExplicitType_ShouldReturnCorrectProvider()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());
        registry.Register(new MSSQLQueryForgeProvider());

        var provider = registry.Resolve(DapperDatabaseProvider.MSSQL);

        provider.Should().BeOfType<MSSQLQueryForgeProvider>();
    }

    [Fact]
    public void Resolve_UnknownProvider_ShouldThrow()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());

        var act = () => registry.Resolve(DapperDatabaseProvider.MySQL);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Resolve_BySqlConnection_ShouldReturnMSSQLProvider()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());
        registry.Register(new MSSQLQueryForgeProvider());
        
        // Fake connection with the exact type name Dapper looks for
        var connection = new SqlConnection();

        var provider = registry.Resolve(connection);

        provider.Should().BeOfType<MSSQLQueryForgeProvider>();
    }

    [Fact]
    public void Resolve_ByUnknownConnection_ShouldThrow()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());
        var connection = new SqlConnection();

        var act = () => registry.Resolve(connection);

        act.Should().Throw<NotSupportedException>();
    }
}