using Microsoft.Extensions.Hosting;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper.Hosting;

/// <summary>
/// An IHostedService that automatically initializes the database schema (e.g., deploys SPs) 
/// at application startup if the configured Approach requires it.
/// </summary>
internal class DapperInitializationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DapperRegistry _registry;

    public DapperInitializationHostedService(IServiceProvider serviceProvider, DapperRegistry registry)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_registry.Options.Approach == DapperExecutionApproach.DevelopAndUseSp)
        {
            if (_registry.Options.ConnectionFactory == null)
            {
                throw new InvalidOperationException(
                    "DapperQueryForgeOptions.ConnectionFactory must be configured in AddQueryForgeDapper() when using DapperExecutionApproach.DevelopAndUseSp.");
            }

            using var connection = _registry.Options.ConnectionFactory(_serviceProvider);

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            var provider = _registry.Resolve(connection);
            await provider.InitializeAsync(connection, _registry.Options, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}