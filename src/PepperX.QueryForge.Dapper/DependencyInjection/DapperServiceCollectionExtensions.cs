using Microsoft.Extensions.DependencyInjection;
using PepperX.QueryForge.Dapper.Compiler;
using PepperX.QueryForge.Dapper.Dialects;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Extension methods for registering QueryForge's Dapper provider.
/// </summary>
public static class DapperServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Dapper execution provider with every built-in database dialect.
    /// </summary>
    /// <remarks>
    /// Nothing is written to your database and no schema permissions are needed — QueryForge builds
    /// parameterized statements at query time. The engine used for a given call is inferred from the
    /// connection you hand it, so a single registration serves an application that talks to more
    /// than one database.
    /// </remarks>
    public static IServiceCollection AddQueryForgeDapper(
        this IServiceCollection services,
        Action<DapperQueryForgeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DapperQueryForgeOptions();
        configure?.Invoke(options);

        var registry = new DapperRegistry(options);

        registry.Register(new SqlServerDialect());
        registry.Register(new PostgreSqlDialect());
        registry.Register(new MySqlDialect());
        registry.Register(new OracleDialect());
        registry.Register(new SqliteDialect());

        services.AddSingleton(registry);
        services.AddSingleton(options);
        services.AddScoped<IDapperQueryService, DapperQueryService>();

        // Backs the IDbConnection extension method, which has no access to the container.
        DapperEngine.SetRegistry(registry);

        return services;
    }

    /// <summary>
    /// Registers an additional or replacement dialect, for a database QueryForge does not ship with
    /// or to override the built-in behaviour for one it does.
    /// </summary>
    public static IServiceCollection AddQueryForgeDialect(this IServiceCollection services, ISqlDialect dialect)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dialect);

        var registry = services
            .Select(d => d.ImplementationInstance)
            .OfType<DapperRegistry>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Call AddQueryForgeDapper() before AddQueryForgeDialect().");

        registry.Register(dialect);

        return services;
    }
}
