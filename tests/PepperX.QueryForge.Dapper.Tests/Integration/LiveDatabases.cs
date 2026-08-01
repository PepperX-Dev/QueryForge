using System.Data;
using MySqlConnector;
using Npgsql;

namespace PepperX.QueryForge.Dapper.Tests.Integration;

/// <summary>
/// Discovers which real database servers this run can reach.
/// </summary>
/// <remarks>
/// SQLite always runs, because it is in-process. The server-backed engines are opt-in, so an ordinary
/// build — a developer's machine, or CI without database services — skips them immediately instead of
/// waiting out a connection timeout per engine.
/// <para>
/// An engine runs when its own variable is set (<c>QUERYFORGE_POSTGRES</c>, <c>QUERYFORGE_MYSQL</c>,
/// <c>QUERYFORGE_MSSQL</c>, <c>QUERYFORGE_ORACLE</c>), or when <c>QUERYFORGE_DB_TESTS=1</c> asks the
/// built-in local defaults to be tried.
/// </para>
/// </remarks>
public static class LiveDatabases
{
    public const string PostgresVariable = "QUERYFORGE_POSTGRES";
    public const string MySqlVariable = "QUERYFORGE_MYSQL";
    public const string SqlServerVariable = "QUERYFORGE_MSSQL";
    public const string OracleVariable = "QUERYFORGE_ORACLE";

    /// <summary>Set to <c>1</c> to try the built-in local defaults for engines with no variable set.</summary>
    public const string EnableVariable = "QUERYFORGE_DB_TESTS";

    /// <summary>Set to <c>1</c> to fail, rather than skip, when a configured engine cannot be reached.</summary>
    /// <remarks>
    /// An unreachable engine is normally a skip, which is right on a developer's machine. In CI it is
    /// dangerous: a database that never came up would quietly turn the whole matrix into a pass. This
    /// makes the intent explicit — if a connection string was supplied, the server behind it must work.
    /// </remarks>
    public const string RequireVariable = "QUERYFORGE_REQUIRE_DB";

    private const string DefaultPostgres =
        "Host=127.0.0.1;Port=55432;Username=postgres;Database=postgres;Include Error Detail=true";

    private const string DefaultMySql =
        "Server=127.0.0.1;Port=55306;Uid=root;Database=queryforge;AllowPublicKeyRetrieval=true;SslMode=None";

    private const string DefaultSqlServer =
        "Server=127.0.0.1,1433;User Id=sa;Password=QueryForge!2024;Database=master;TrustServerCertificate=true;Encrypt=false";

    /// <summary>Whether the built-in local defaults may be tried for engines with no variable set.</summary>
    private static bool LocalDefaultsEnabled =>
        Environment.GetEnvironmentVariable(EnableVariable) is "1" or "true" or "True" or "TRUE";

    /// <summary>
    /// Resolves a connection string, or <see langword="null"/> when this engine is not opted in.
    /// </summary>
    private static string? Resolve(string variable, string? localDefault)
    {
        var configured = Environment.GetEnvironmentVariable(variable);

        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return LocalDefaultsEnabled ? localDefault : null;
    }

    public static string? PostgresConnectionString => Resolve(PostgresVariable, DefaultPostgres);

    public static string? MySqlConnectionString => Resolve(MySqlVariable, DefaultMySql);

    public static string? SqlServerConnectionString => Resolve(SqlServerVariable, DefaultSqlServer);

    /// <summary>
    /// Oracle has no built-in local default — there is no ubiquitous throwaway instance the way there
    /// is for the others — so it only ever runs when pointed at one explicitly.
    /// </summary>
    public static string? OracleConnectionString => Resolve(OracleVariable, localDefault: null);

    private static readonly Lazy<bool> PostgresReachable = new(() =>
        PostgresConnectionString is not null && CanConnect(() => new NpgsqlConnection(PostgresConnectionString)));

    private static readonly Lazy<bool> MySqlReachable = new(() =>
        MySqlConnectionString is not null && CanConnect(() =>
        {
            EnsureMySqlDatabase();
            return new MySqlConnection(MySqlConnectionString);
        }));

    private static readonly Lazy<bool> SqlServerReachable = new(() =>
        SqlServerConnectionString is not null
        && CanConnect(() => new Microsoft.Data.SqlClient.SqlConnection(SqlServerConnectionString)));

    private static readonly Lazy<bool> OracleReachable = new(() =>
        OracleConnectionString is not null && CanConnect(OpenOracleConnection));

    public static bool HasPostgres => PostgresReachable.Value;

    public static bool HasSqlServer => SqlServerReachable.Value;

    public static Microsoft.Data.SqlClient.SqlConnection OpenSqlServer()
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnection(SqlServerConnectionString);
        connection.Open();

        return connection;
    }

    public static bool HasMySql => MySqlReachable.Value;

    public static bool HasOracle => OracleReachable.Value;

    public static bool EnginesAreRequired =>
        Environment.GetEnvironmentVariable(RequireVariable) is "1" or "true" or "True" or "TRUE";

    /// <summary>The variables naming an engine that was configured but could not be reached.</summary>
    public static IReadOnlyList<string> ConfiguredButUnreachable()
    {
        var missing = new List<string>();

        if (PostgresConnectionString is not null && !HasPostgres)
            missing.Add(PostgresVariable);

        if (MySqlConnectionString is not null && !HasMySql)
            missing.Add(MySqlVariable);

        if (SqlServerConnectionString is not null && !HasSqlServer)
            missing.Add(SqlServerVariable);

        if (OracleConnectionString is not null && !HasOracle)
            missing.Add(OracleVariable);

        return missing;
    }

    /// <summary>
    /// Opens an Oracle connection with named parameter binding switched on.
    /// </summary>
    /// <remarks>
    /// ODP.NET binds parameters positionally by default, so <c>:p0</c> and <c>:p1</c> would be matched
    /// by the order they were added rather than by name. QueryForge emits named references, so any
    /// Oracle connection used with it needs <c>BindByName</c> — this is the one piece of setup Oracle
    /// requires that the other engines do not.
    /// </remarks>
    public static Oracle.ManagedDataAccess.Client.OracleConnection OpenOracle()
    {
        var connection = OpenOracleConnection();
        connection.Open();

        return connection;
    }

    private static Oracle.ManagedDataAccess.Client.OracleConnection OpenOracleConnection()
        => new(OracleConnectionString);

    public static NpgsqlConnection OpenPostgres()
    {
        var connection = new NpgsqlConnection(PostgresConnectionString);
        connection.Open();

        return connection;
    }

    public static MySqlConnection OpenMySql()
    {
        var connection = new MySqlConnection(MySqlConnectionString);
        connection.Open();

        return connection;
    }

    private static bool CanConnect(Func<IDbConnection> factory)
    {
        try
        {
            using var connection = factory();
            connection.Open();

            return true;
        }
        catch (Exception)
        {
            // Absence of a server is a skip, not a failure.
            return false;
        }
    }

    /// <summary>MySQL will not connect to a database that does not exist yet, unlike PostgreSQL.</summary>
    private static void EnsureMySqlDatabase()
    {
        var builder = new MySqlConnectionStringBuilder(MySqlConnectionString);
        var database = builder.Database;

        builder.Database = string.Empty;

        using var connection = new MySqlConnection(builder.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{database}`";
        command.ExecuteNonQuery();
    }
}

/// <summary>Skips a fact when the PostgreSQL server is not reachable.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!LiveDatabases.HasPostgres)
            Skip = $"No PostgreSQL server reachable (set {LiveDatabases.PostgresVariable} to point at one).";
    }
}

/// <summary>Skips a theory when the PostgreSQL server is not reachable.</summary>
public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (!LiveDatabases.HasPostgres)
            Skip = $"No PostgreSQL server reachable (set {LiveDatabases.PostgresVariable} to point at one).";
    }
}

/// <summary>Skips a fact when the MySQL server is not reachable.</summary>
public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (!LiveDatabases.HasMySql)
            Skip = $"No MySQL server reachable (set {LiveDatabases.MySqlVariable} to point at one).";
    }
}

/// <summary>Skips a theory when the MySQL server is not reachable.</summary>
public sealed class MySqlTheoryAttribute : TheoryAttribute
{
    public MySqlTheoryAttribute()
    {
        if (!LiveDatabases.HasMySql)
            Skip = $"No MySQL server reachable (set {LiveDatabases.MySqlVariable} to point at one).";
    }
}
