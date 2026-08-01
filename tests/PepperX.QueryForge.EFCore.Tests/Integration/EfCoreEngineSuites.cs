using Microsoft.EntityFrameworkCore;
using PepperX.QueryForge.Conformance;
using Xunit;

namespace PepperX.QueryForge.EFCore.Tests.Integration;

/// <summary>
/// Both shared suites, run through EF Core against every database engine this machine can reach.
/// </summary>
/// <remarks>
/// EF Core generates different SQL for each engine from the same expression tree, so these catch the
/// cases where a translation works on one database and not another — the class of problem that only
/// shows up when the query actually executes.
/// </remarks>
public static class EfCoreSuites;

#region SQLite

public sealed class SqliteEfConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly EfCoreFixture _fixture = EfCoreFixture.Sqlite(seedOrders: false);

    public void Dispose() => _fixture.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query)
        => _fixture.Context.Widgets.AsNoTracking().ToQueryResultAsync(query);
}

public sealed class SqliteEfSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly EfCoreFixture _fixture = EfCoreFixture.Sqlite(seedOrders: true);

    public void Dispose() => _fixture.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query)
        => _fixture.Context.Orders.AsNoTracking().ToQueryResultAsync(query);
}

#endregion

#region PostgreSQL

[Collection(RelationalServerCollection.Name)]
public sealed class PostgresEfConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public PostgresEfConformanceTests()
    {
        Skip.IfNot(EfCoreEngines.HasPostgres, "No PostgreSQL server reachable.");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UsePostgres(), seedOrders: false);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query)
        => _fixture!.Context.Widgets.AsNoTracking().ToQueryResultAsync(query);
}

[Collection(RelationalServerCollection.Name)]
public sealed class PostgresEfSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public PostgresEfSalesScenarioTests()
    {
        Skip.IfNot(EfCoreEngines.HasPostgres, "No PostgreSQL server reachable.");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UsePostgres(), seedOrders: true);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query)
        => _fixture!.Context.Orders.AsNoTracking().ToQueryResultAsync(query);
}

#endregion

#region MySQL

[Collection(RelationalServerCollection.Name)]
public sealed class MySqlEfConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public MySqlEfConformanceTests()
    {
        Skip.IfNot(EfCoreEngines.HasMySql, "No MySQL server reachable.");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UseMySql(), seedOrders: false);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query)
        => _fixture!.Context.Widgets.AsNoTracking().ToQueryResultAsync(query);
}

[Collection(RelationalServerCollection.Name)]
public sealed class MySqlEfSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public MySqlEfSalesScenarioTests()
    {
        Skip.IfNot(EfCoreEngines.HasMySql, "No MySQL server reachable.");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UseMySql(), seedOrders: true);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query)
        => _fixture!.Context.Orders.AsNoTracking().ToQueryResultAsync(query);
}

#endregion

#region SQL Server

[Collection(RelationalServerCollection.Name)]
public sealed class SqlServerEfConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public SqlServerEfConformanceTests()
    {
        Skip.IfNot(EfCoreEngines.HasSqlServer, "No SQL Server instance reachable.");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UseSqlServer(), seedOrders: false);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query)
        => _fixture!.Context.Widgets.AsNoTracking().ToQueryResultAsync(query);
}

[Collection(RelationalServerCollection.Name)]
public sealed class SqlServerEfSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public SqlServerEfSalesScenarioTests()
    {
        Skip.IfNot(EfCoreEngines.HasSqlServer, "No SQL Server instance reachable.");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UseSqlServer(), seedOrders: true);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query)
        => _fixture!.Context.Orders.AsNoTracking().ToQueryResultAsync(query);
}

#endregion

#region Oracle

[Collection(RelationalServerCollection.Name)]
public sealed class OracleEfConformanceTests : QueryForgeConformanceTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public OracleEfConformanceTests()
    {
        Skip.IfNot(EfCoreEngines.HasOracle, $"No Oracle instance reachable (set {EfCoreEngines.OracleVariable}).");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UseOracle(), seedOrders: false);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<Widget>> RunAsync(Query query)
        => _fixture!.Context.Widgets.AsNoTracking().ToQueryResultAsync(query);
}

[Collection(RelationalServerCollection.Name)]
public sealed class OracleEfSalesScenarioTests : SalesScenarioTests, IDisposable
{
    private readonly EfCoreFixture? _fixture;

    public OracleEfSalesScenarioTests()
    {
        Skip.IfNot(EfCoreEngines.HasOracle, $"No Oracle instance reachable (set {EfCoreEngines.OracleVariable}).");
        _fixture = EfCoreFixture.ForServer(EfCoreEngines.UseOracle(), seedOrders: true);
    }

    public void Dispose() => _fixture?.Dispose();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query)
        => _fixture!.Context.Orders.AsNoTracking().ToQueryResultAsync(query);
}

#endregion
