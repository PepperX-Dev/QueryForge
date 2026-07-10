using Microsoft.Extensions.DependencyInjection;
using PepperX.QueryForge.Dapper.Hosting;
using PepperX.QueryForge.Dapper.Internals;
using PepperX.QueryForge.Dapper.Providers.MSSQL;

namespace PepperX.QueryForge.Dapper;

/// <summary>
/// Extension methods for configuring QueryForge Dapper services in the DI container.
/// </summary>
public static class DapperServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dapper-specific QueryForge services to the DI container, configures global options, 
    /// and registers the background initialization service.
    /// </summary>
    public static IServiceCollection AddQueryForgeDapper(this IServiceCollection services, Action<DapperQueryForgeOptions> configure)
    {
        var options = new DapperQueryForgeOptions();
        configure(options);

        var registry = new DapperRegistry(options);

        // Register built-in providers
        registry.Register(new MSSQLQueryForgeProvider());
        // registry.Register(new MySQLQueryForgeProvider());
        // registry.Register(new PostgreSQLQueryForgeProvider());
        // registry.Register(new OracleQueryForgeProvider());

        // Add to DI Container
        services.AddSingleton(registry);
        services.AddSingleton(options);

        // Register the Dapper-specific scoped service
        services.AddScoped<IDapperQueryService, DapperQueryService>();

        // Set the static engine for Dapper extension methods
        DapperEngine.SetRegistry(registry);

        // Register the Background Initialization Service
        services.AddHostedService<DapperInitializationHostedService>();

        return services;
    }
}