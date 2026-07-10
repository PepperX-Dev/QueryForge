using System.Data;

namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// A Singleton registry that holds all registered Dapper database providers.
/// </summary>
internal class DapperRegistry
{
    private readonly Dictionary<DapperDatabaseProvider, IDapperQueryForgeProvider> _providers = new();

    /// <summary>The global Dapper configuration options.</summary>
    public DapperQueryForgeOptions Options { get; }

    public DapperRegistry(DapperQueryForgeOptions options)
    {
        Options = options;
    }

    /// <summary>Registers a provider implementation into the registry.</summary>
    public void Register(IDapperQueryForgeProvider provider)
    {
        _providers[provider.ProviderType] = provider;
    }

    /// <summary>Resolves a provider by its explicit database type.</summary>
    public IDapperQueryForgeProvider Resolve(DapperDatabaseProvider type)
    {
        if (!_providers.TryGetValue(type, out var provider))
            throw new NotSupportedException($"The Dapper provider for '{type}' is not registered.");

        return provider;
    }

    /// <summary>
    /// Automatically detects the database type from the IDbConnection 
    /// and resolves the correct provider.
    /// </summary>
    public IDapperQueryForgeProvider Resolve(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var typeName = connection.GetType().Name;
        var type = typeName switch
        {
            "SqlConnection" => DapperDatabaseProvider.MSSQL,
            "MySqlConnection" => DapperDatabaseProvider.MySQL,
            "NpgsqlConnection" => DapperDatabaseProvider.PostgreSQL,
            "OracleConnection" => DapperDatabaseProvider.Oracle,
            _ => throw new NotSupportedException($"The database provider for connection type '{typeName}' is not supported by QueryForge Dapper.")
        };

        return Resolve(type);
    }
}