using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using PepperX.QueryForge.Dapper.Dialects;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper.Tests.Internals;

public class DapperRegistryTests
{
    private sealed class SqlConnection : StubConnection;

    private sealed class NpgsqlConnection : StubConnection;

    private sealed class MySqlConnection : StubConnection;

    private sealed class OracleConnection : StubConnection;

    private sealed class SqliteConnection : StubConnection;

    private sealed class FirebirdConnection : StubConnection;

    private static DapperRegistry FullyRegistered()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());

        registry.Register(new SqlServerDialect());
        registry.Register(new PostgreSqlDialect());
        registry.Register(new MySqlDialect());
        registry.Register(new OracleDialect());
        registry.Register(new SqliteDialect());

        return registry;
    }

    [Theory]
    [InlineData(DapperDatabaseProvider.MSSQL)]
    [InlineData(DapperDatabaseProvider.PostgreSQL)]
    [InlineData(DapperDatabaseProvider.MySQL)]
    [InlineData(DapperDatabaseProvider.Oracle)]
    [InlineData(DapperDatabaseProvider.SQLite)]
    public void Resolve_ByProvider_ShouldReturnAnExecutorForEveryBuiltInEngine(DapperDatabaseProvider provider)
    {
        var registry = FullyRegistered();

        registry.Resolve(provider).Should().NotBeNull();
    }

    [Fact]
    public void Resolve_ByUnregisteredProvider_ShouldThrow()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());

        var act = () => registry.Resolve(DapperDatabaseProvider.MSSQL);

        act.Should().Throw<NotSupportedException>().WithMessage("*No QueryForge dialect is registered*");
    }

    [Fact]
    public void Resolve_ByConnection_ShouldMapEachKnownDriverType()
    {
        var registry = FullyRegistered();

        IDbConnection[] connections =
        [
            new SqlConnection(),
            new NpgsqlConnection(),
            new MySqlConnection(),
            new OracleConnection(),
            new SqliteConnection()
        ];

        foreach (var connection in connections)
            registry.Resolve(connection).Should().NotBeNull();
    }

    [Fact]
    public void Resolve_ByUnknownConnection_ShouldExplainWhatIsSupported()
    {
        var registry = FullyRegistered();

        var act = () => registry.Resolve(new FirebirdConnection());

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*cannot infer a database engine*")
            .WithMessage("*SqlConnection*");
    }

    [Fact]
    public void Register_ShouldReplaceAPreviousDialectForTheSameEngine()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());

        registry.Register(new SqlServerDialect());
        registry.Register(new SqlServerDialect());

        registry.Resolve(DapperDatabaseProvider.MSSQL).Should().NotBeNull();
    }

    /// <summary>
    /// The registry identifies engines by connection type name, so a stub named after a real driver
    /// is all these tests need.
    /// </summary>
    private abstract class StubConnection : IDbConnection
    {
        // IDbConnection.ConnectionString allows null on the way in; the stub keeps a non-null value.
        [AllowNull]
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
