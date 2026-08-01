using System.Data;
using Microsoft.Data.Sqlite;
using PepperX.QueryForge.Conformance;
using PepperX.QueryForge.Dapper.Compiler;
using PepperX.QueryForge.Dapper.Dialects;
using PepperX.QueryForge.Dapper.Internals;
using Xunit;

namespace PepperX.QueryForge.Dapper.Tests.Integration;

/// <summary>
/// Shared plumbing for running a suite against one real database engine.
/// </summary>
/// <remarks>
/// Each engine gets its own tables so the suites can run in parallel without colliding, and every
/// fixture drops and reseeds on construction, so one test's data can never leak into another's
/// assertions.
/// </remarks>
public abstract class DapperEngineFixture : IDisposable
{
    private readonly QueryExecutor _executor;

    protected IDbConnection Connection { get; }

    protected abstract string WidgetTable { get; }

    protected abstract string OrderTable { get; }

    protected DapperEngineFixture(IDbConnection connection, ISqlDialect dialect, bool seedOrders)
    {
        Connection = connection;

        var registry = new DapperRegistry(new DapperQueryForgeOptions());
        registry.Register(dialect);
        _executor = registry.Resolve(connection);

        if (seedOrders)
            RelationalTestDatabase.CreateSalesOrders(connection, dialect, OrderTable);
        else
            RelationalTestDatabase.CreateWidgets(connection, dialect, WidgetTable);
    }

    public void Dispose() => Connection.Dispose();

    protected Task<QueryResult<TModel>> ExecuteAsync<TModel>(Query query, string table)
    {
        var dapperQuery = DapperQueryBuilder
            .New(new DapperQuery
            {
                Criteria = query.Criteria,
                Paging = query.Paging,
                SelectColumns = query.SelectColumns,
                SortColumns = query.SortColumns,
                GroupByColumns = query.GroupByColumns
            })
            .ForObject(table)
            .Build();

        return _executor.QueryAsync<TModel>(Connection, dapperQuery, commandTimeout: null, transaction: null);
    }
}

#region SQLite

public sealed class SqliteWidgetFixture() : DapperEngineFixture(OpenSqlite(), new SqliteDialect(), seedOrders: false)
{
    protected override string WidgetTable => "Widget";
    protected override string OrderTable => "SalesOrder";

    public Task<QueryResult<Widget>> QueryAsync(Query query) => ExecuteAsync<Widget>(query, WidgetTable);

    private static SqliteConnection OpenSqlite()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        return connection;
    }
}

public sealed class SqliteSalesFixture() : DapperEngineFixture(OpenSqlite(), new SqliteDialect(), seedOrders: true)
{
    protected override string WidgetTable => "Widget";
    protected override string OrderTable => "SalesOrder";

    public Task<QueryResult<SalesOrder>> QueryAsync(Query query) => ExecuteAsync<SalesOrder>(query, OrderTable);

    private static SqliteConnection OpenSqlite()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        return connection;
    }
}

/// <summary>The full conformance suite, executed against SQLite.</summary>
public sealed class SqliteConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly SqliteWidgetFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query) => _fixture.QueryAsync(query);
}

/// <summary>The business scenarios, executed against SQLite.</summary>
public sealed class SqliteSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly SqliteSalesFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query) => _fixture.QueryAsync(query);
}

#endregion

#region PostgreSQL

public sealed class PostgresWidgetFixture : DapperEngineFixture
{
    public PostgresWidgetFixture()
        : base(LiveDatabases.OpenPostgres(), new PostgreSqlDialect(), seedOrders: false) { }

    protected override string WidgetTable => "qf_widget";
    protected override string OrderTable => "qf_order";

    public Task<QueryResult<Widget>> QueryAsync(Query query) => ExecuteAsync<Widget>(query, WidgetTable);
}

public sealed class PostgresSalesFixture : DapperEngineFixture
{
    public PostgresSalesFixture()
        : base(LiveDatabases.OpenPostgres(), new PostgreSqlDialect(), seedOrders: true) { }

    protected override string WidgetTable => "qf_widget_s";
    protected override string OrderTable => "qf_order";

    public Task<QueryResult<SalesOrder>> QueryAsync(Query query) => ExecuteAsync<SalesOrder>(query, OrderTable);
}

/// <summary>The full conformance suite, executed against a real PostgreSQL server.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class PostgresConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly PostgresWidgetFixture? _fixture;

    public PostgresConformanceTests()
    {
        Skip.IfNot(LiveDatabases.HasPostgres, "No PostgreSQL server reachable.");
        _fixture = new PostgresWidgetFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

/// <summary>The business scenarios, executed against a real PostgreSQL server.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class PostgresSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly PostgresSalesFixture? _fixture;

    public PostgresSalesScenarioTests()
    {
        Skip.IfNot(LiveDatabases.HasPostgres, "No PostgreSQL server reachable.");
        _fixture = new PostgresSalesFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

#endregion

#region MySQL

public sealed class MySqlWidgetFixture : DapperEngineFixture
{
    public MySqlWidgetFixture()
        : base(LiveDatabases.OpenMySql(), new MySqlDialect(), seedOrders: false) { }

    protected override string WidgetTable => "qf_widget";
    protected override string OrderTable => "qf_order";

    public Task<QueryResult<Widget>> QueryAsync(Query query) => ExecuteAsync<Widget>(query, WidgetTable);
}

public sealed class MySqlSalesFixture : DapperEngineFixture
{
    public MySqlSalesFixture()
        : base(LiveDatabases.OpenMySql(), new MySqlDialect(), seedOrders: true) { }

    protected override string WidgetTable => "qf_widget_s";
    protected override string OrderTable => "qf_order";

    public Task<QueryResult<SalesOrder>> QueryAsync(Query query) => ExecuteAsync<SalesOrder>(query, OrderTable);
}

/// <summary>The full conformance suite, executed against a real MySQL server.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class MySqlConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly MySqlWidgetFixture? _fixture;

    public MySqlConformanceTests()
    {
        Skip.IfNot(LiveDatabases.HasMySql, "No MySQL server reachable.");
        _fixture = new MySqlWidgetFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

/// <summary>The business scenarios, executed against a real MySQL server.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class MySqlSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly MySqlSalesFixture? _fixture;

    public MySqlSalesScenarioTests()
    {
        Skip.IfNot(LiveDatabases.HasMySql, "No MySQL server reachable.");
        _fixture = new MySqlSalesFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

#endregion

#region SQL Server

public sealed class SqlServerWidgetFixture : DapperEngineFixture
{
    public SqlServerWidgetFixture()
        : base(LiveDatabases.OpenSqlServer(), new SqlServerDialect(), seedOrders: false) { }

    protected override string WidgetTable => "qf_widget";
    protected override string OrderTable => "qf_order";

    public Task<QueryResult<Widget>> QueryAsync(Query query) => ExecuteAsync<Widget>(query, WidgetTable);
}

public sealed class SqlServerSalesFixture : DapperEngineFixture
{
    public SqlServerSalesFixture()
        : base(LiveDatabases.OpenSqlServer(), new SqlServerDialect(), seedOrders: true) { }

    protected override string WidgetTable => "qf_widget_s";
    protected override string OrderTable => "qf_order";

    public Task<QueryResult<SalesOrder>> QueryAsync(Query query) => ExecuteAsync<SalesOrder>(query, OrderTable);
}

/// <summary>The full conformance suite, executed against a real SQL Server instance.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class SqlServerConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly SqlServerWidgetFixture? _fixture;

    public SqlServerConformanceTests()
    {
        Skip.IfNot(LiveDatabases.HasSqlServer, "No SQL Server instance reachable.");
        _fixture = new SqlServerWidgetFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

/// <summary>The business scenarios, executed against a real SQL Server instance.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class SqlServerSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly SqlServerSalesFixture? _fixture;

    public SqlServerSalesScenarioTests()
    {
        Skip.IfNot(LiveDatabases.HasSqlServer, "No SQL Server instance reachable.");
        _fixture = new SqlServerSalesFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

#endregion

#region Oracle

public sealed class OracleWidgetFixture : DapperEngineFixture
{
    public OracleWidgetFixture()
        : base(LiveDatabases.OpenOracle(), new OracleDialect(), seedOrders: false) { }

    // Oracle folds unquoted names to upper case, so the tables are created and referenced that way.
    protected override string WidgetTable => "QF_WIDGET";
    protected override string OrderTable => "QF_ORDER";

    public Task<QueryResult<Widget>> QueryAsync(Query query) => ExecuteAsync<Widget>(query, WidgetTable);
}

public sealed class OracleSalesFixture : DapperEngineFixture
{
    public OracleSalesFixture()
        : base(LiveDatabases.OpenOracle(), new OracleDialect(), seedOrders: true) { }

    protected override string WidgetTable => "QF_WIDGET_S";
    protected override string OrderTable => "QF_ORDER";

    public Task<QueryResult<SalesOrder>> QueryAsync(Query query) => ExecuteAsync<SalesOrder>(query, OrderTable);
}

/// <summary>The full conformance suite, executed against a real Oracle instance.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class OracleConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly OracleWidgetFixture? _fixture;

    public OracleConformanceTests()
    {
        Skip.IfNot(LiveDatabases.HasOracle, $"No Oracle instance reachable (set {LiveDatabases.OracleVariable}).");
        _fixture = new OracleWidgetFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

/// <summary>The business scenarios, executed against a real Oracle instance.</summary>
[Collection(RelationalServerCollection.Name)]
public sealed class OracleSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly OracleSalesFixture? _fixture;

    public OracleSalesScenarioTests()
    {
        Skip.IfNot(LiveDatabases.HasOracle, $"No Oracle instance reachable (set {LiveDatabases.OracleVariable}).");
        _fixture = new OracleSalesFixture();
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query) => _fixture!.QueryAsync(query);
}

#endregion
