using Xunit;

namespace PepperX.QueryForge.Dapper.Tests.Integration;

/// <summary>
/// Serializes the test classes that share a real database server.
/// </summary>
/// <remarks>
/// Each fixture drops and recreates its tables on construction, so running two of them concurrently
/// against one server lets each destroy the other's data. SQLite classes stay parallel — every one
/// owns a private in-memory database.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RelationalServerCollection
{
    public const string Name = "relational-server";
}
