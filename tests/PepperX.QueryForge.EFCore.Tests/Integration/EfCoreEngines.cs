using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using MySql.EntityFrameworkCore.Extensions;
using PepperX.QueryForge.Conformance;
using Xunit;

namespace PepperX.QueryForge.EFCore.Tests.Integration;

/// <summary>
/// The EF Core context the shared suites are run through.
/// </summary>
/// <remarks>
/// One model, several backing databases. EF Core generates the SQL for each, so running the suites
/// on more than one engine is what proves the provider is genuinely database-agnostic rather than
/// just SQLite-shaped.
/// </remarks>
public sealed class QueryForgeTestContext(DbContextOptions<QueryForgeTestContext> options) : DbContext(options)
{
    public DbSet<Widget> Widgets => Set<Widget>();

    public DbSet<SalesOrder> Orders => Set<SalesOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Widget>(e =>
        {
            e.ToTable("qf_ef_widget");
            e.HasKey(w => w.Id);
            e.Property(w => w.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<SalesOrder>(e =>
        {
            e.ToTable("qf_ef_order");
            e.HasKey(o => o.OrderId);
            e.Property(o => o.OrderId).ValueGeneratedNever();
        });

        // Npgsql maps DateTime to "timestamp with time zone" by default, which only accepts UTC
        // values. These are wall-clock order dates with no zone, so the untyped column is correct —
        // and mapping it explicitly keeps the same seed data usable on every engine.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Widget>().Property(w => w.ReleasedOn).HasColumnType("timestamp without time zone");
            modelBuilder.Entity<SalesOrder>().Property(o => o.PlacedOn).HasColumnType("timestamp without time zone");
            modelBuilder.Entity<SalesOrder>().Property(o => o.ShippedOn).HasColumnType("timestamp without time zone");
        }
    }
}

/// <summary>Discovers which database servers the EF Core suites can reach.</summary>
/// <remarks>
/// The same environment variables the Dapper suites use, so one set of connection strings covers
/// both providers.
/// </remarks>
public static class EfCoreEngines
{
    public const string PostgresVariable = "QUERYFORGE_POSTGRES";
    public const string MySqlVariable = "QUERYFORGE_MYSQL";
    public const string SqlServerVariable = "QUERYFORGE_MSSQL";
    public const string OracleVariable = "QUERYFORGE_ORACLE";

    /// <summary>Set to <c>1</c> to try the built-in local defaults for engines with no variable set.</summary>
    public const string EnableVariable = "QUERYFORGE_DB_TESTS";

    private static bool LocalDefaultsEnabled =>
        Environment.GetEnvironmentVariable(EnableVariable) is "1" or "true" or "True" or "TRUE";

    /// <summary>
    /// Resolves a connection string, or <see langword="null"/> when this engine is not opted in — so
    /// an ordinary build skips it outright rather than waiting for a connection to time out.
    /// </summary>
    private static string? Resolve(string variable, string? localDefault)
    {
        var configured = Environment.GetEnvironmentVariable(variable);

        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return LocalDefaultsEnabled ? localDefault : null;
    }

    public static string? Postgres =>
        Resolve(PostgresVariable, "Host=127.0.0.1;Port=55432;Username=postgres;Database=postgres");

    /// <summary>
    /// The MySQL connection string, normalised for Oracle's <c>MySql.Data</c> driver.
    /// </summary>
    /// <remarks>
    /// The Dapper suites use MySqlConnector and this one uses MySql.Data, and the two spell the
    /// no-TLS setting differently — <c>SslMode=None</c> versus <c>SslMode=Disabled</c>. Normalising
    /// here means one environment variable still configures both.
    /// </remarks>
    public static string? MySql
    {
        get
        {
            var raw = Resolve(
                MySqlVariable,
                "Server=127.0.0.1;Port=55306;Uid=root;Database=queryforge;AllowPublicKeyRetrieval=true;SslMode=Disabled");

            return raw?.Replace("SslMode=None", "SslMode=Disabled", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static string? SqlServer => Resolve(SqlServerVariable, localDefault: null);

    public static string? Oracle => Resolve(OracleVariable, localDefault: null);

    private static readonly Lazy<bool> PostgresUp = new(() => Postgres is not null && CanCreate(UsePostgres));
    private static readonly Lazy<bool> MySqlUp = new(() => MySql is not null && CanCreate(UseMySql));
    private static readonly Lazy<bool> SqlServerUp = new(() => SqlServer is not null && CanCreate(UseSqlServer));
    private static readonly Lazy<bool> OracleUp = new(() => Oracle is not null && CanCreate(UseOracle));

    public static bool HasPostgres => PostgresUp.Value;

    public static bool HasMySql => MySqlUp.Value;

    public static bool HasSqlServer => SqlServerUp.Value;

    public static bool HasOracle => OracleUp.Value;

    public static DbContextOptions<QueryForgeTestContext> UsePostgres()
        => new DbContextOptionsBuilder<QueryForgeTestContext>().UseNpgsql(Postgres).Options;

    public static DbContextOptions<QueryForgeTestContext> UseMySql()
        => new DbContextOptionsBuilder<QueryForgeTestContext>().UseMySQL(MySql!).Options;

    public static DbContextOptions<QueryForgeTestContext> UseSqlServer()
        => new DbContextOptionsBuilder<QueryForgeTestContext>().UseSqlServer(SqlServer).Options;

    public static DbContextOptions<QueryForgeTestContext> UseOracle()
        => new DbContextOptionsBuilder<QueryForgeTestContext>().UseOracle(Oracle).Options;

    private static bool CanCreate(Func<DbContextOptions<QueryForgeTestContext>> build)
    {
        try
        {
            using var context = new QueryForgeTestContext(build());

            return context.Database.CanConnect();
        }
        catch (Exception)
        {
            // No server is a skip, not a failure.
            return false;
        }
    }
}

/// <summary>
/// Creates the tables and seeds them, then hands back a context the suites can query.
/// </summary>
/// <remarks>
/// The tables are dropped and recreated per fixture, so a run always starts from the same data no
/// matter what a previous run left behind.
/// </remarks>
public sealed class EfCoreFixture : IDisposable
{
    private readonly SqliteConnection? _sqlite;

    public QueryForgeTestContext Context { get; }

    private EfCoreFixture(QueryForgeTestContext context, SqliteConnection? sqlite, bool seedOrders)
    {
        Context = context;
        _sqlite = sqlite;

        // Only the fixture's own tables are dropped and recreated. EnsureDeleted would drop the whole
        // database, which fails outright on a shared server and would be rude even where it works.
        Context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS qf_ef_widget");
        Context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS qf_ef_order");

        var creator = (RelationalDatabaseCreator)Context.Database.GetService<IDatabaseCreator>();
        creator.CreateTables();

        if (seedOrders)
            Context.Orders.AddRange(SalesData.Fresh());
        else
            Context.Widgets.AddRange(WidgetData.Fresh());

        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public static EfCoreFixture Sqlite(bool seedOrders)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<QueryForgeTestContext>().UseSqlite(connection).Options;

        return new EfCoreFixture(new QueryForgeTestContext(options), connection, seedOrders);
    }

    public static EfCoreFixture ForServer(DbContextOptions<QueryForgeTestContext> options, bool seedOrders)
        => new(new QueryForgeTestContext(options), sqlite: null, seedOrders);

    public void Dispose()
    {
        Context.Dispose();
        _sqlite?.Dispose();
    }
}
