using PepperX.QueryForge.Dapper.Dialects;

namespace PepperX.QueryForge.Dapper.Internals;

/// <summary>
/// Holds the registry that backs the <see cref="IDbConnection"/> extension methods, which run
/// outside the DI container and so cannot resolve it.
/// </summary>
internal static class DapperEngine
{
    internal static DapperRegistry Registry { get; private set; } = CreateDefault();

    internal static void SetRegistry(DapperRegistry registry) => Registry = registry;

    /// <summary>
    /// A registry carrying the built-in dialects, so that <c>QueryForgeAsync</c> works on a
    /// connection even in an application that never called <c>AddQueryForgeDapper</c>.
    /// </summary>
    private static DapperRegistry CreateDefault()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());

        registry.Register(new SqlServerDialect());
        registry.Register(new PostgreSqlDialect());
        registry.Register(new MySqlDialect());
        registry.Register(new OracleDialect());
        registry.Register(new SqliteDialect());

        return registry;
    }
}
