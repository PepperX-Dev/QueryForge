using Xunit;

namespace PepperX.QueryForge.EFCore.Tests.Integration;

/// <summary>
/// Serializes the test classes that share a real database server.
/// </summary>
/// <remarks>
/// Each fixture drops and recreates its tables on construction. Run in parallel against one server,
/// two fixtures will happily drop the tables the other has just seeded — so the suites that talk to a
/// shared server run one at a time. The SQLite classes stay parallel: each owns a private in-memory
/// database and cannot collide.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RelationalServerCollection
{
    public const string Name = "relational-server";
}
