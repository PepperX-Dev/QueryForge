namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// Internal static engine that holds the Singleton Registry.
/// Bridges the gap between ASP.NET Core DI and Dapper's IDbConnection extensions.
/// </summary>
internal static class DapperEngine
{
    internal static DapperRegistry Registry { get; private set; } = new(new DapperQueryForgeOptions());

    internal static void SetRegistry(DapperRegistry registry)
    {
        Registry = registry;
    }
}