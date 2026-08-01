using System.Data;
using PepperX.QueryForge.Dapper.Compiler;

namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// Holds one executor per registered database engine and resolves the right one for a connection.
/// </summary>
/// <remarks>
/// Executors are kept for the lifetime of the registry rather than built per request, because each
/// one owns a <see cref="SchemaCache"/> whose value comes from being reused.
/// </remarks>
internal sealed class DapperRegistry(DapperQueryForgeOptions options)
{
    private readonly Dictionary<DapperDatabaseProvider, QueryExecutor> _executors = new();

    /// <summary>The global configuration options.</summary>
    public DapperQueryForgeOptions Options { get; } = options;

    /// <summary>Registers a dialect, replacing any previous registration for the same engine.</summary>
    public void Register(ISqlDialect dialect)
    {
        ArgumentNullException.ThrowIfNull(dialect);

        var compiler = new SqlQueryCompiler(dialect);
        _executors[dialect.ProviderType] = new QueryExecutor(compiler, new SchemaCache(compiler));
    }

    /// <summary>Resolves the executor for an explicitly named engine.</summary>
    public QueryExecutor Resolve(DapperDatabaseProvider type)
    {
        if (!_executors.TryGetValue(type, out var executor))
            throw new NotSupportedException($"No QueryForge dialect is registered for '{type}'.");

        return executor;
    }

    /// <summary>
    /// Resolves the executor for a connection by inspecting its ADO.NET provider type.
    /// </summary>
    /// <remarks>
    /// Matching on the type name rather than the type itself keeps this package free of a reference
    /// to every database driver — the caller brings only the driver they actually use.
    /// </remarks>
    public QueryExecutor Resolve(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var typeName = connection.GetType().Name;

        var type = typeName switch
        {
            "SqlConnection" => DapperDatabaseProvider.MSSQL,
            "MySqlConnection" => DapperDatabaseProvider.MySQL,
            "MariaDbConnection" => DapperDatabaseProvider.MySQL,
            "NpgsqlConnection" => DapperDatabaseProvider.PostgreSQL,
            "OracleConnection" => DapperDatabaseProvider.Oracle,
            "SqliteConnection" => DapperDatabaseProvider.SQLite,
            _ => throw new NotSupportedException(
                $"QueryForge cannot infer a database engine from connection type '{typeName}'. " +
                "Supported connections are SqlConnection, NpgsqlConnection, MySqlConnection, OracleConnection and SqliteConnection.")
        };

        return Resolve(type);
    }
}
