using Xunit;

namespace PepperX.QueryForge.Dapper.Tests.Integration;

/// <summary>
/// Guards the thing the rest of this folder cannot guard for itself: that the engines someone asked
/// for actually ran.
/// </summary>
/// <remarks>
/// Every suite here skips when its server is unreachable, which is the right behaviour on a developer's
/// machine and the wrong one in CI — a database container that failed to start would produce a green
/// run that tested nothing. Setting <c>QUERYFORGE_REQUIRE_DB=1</c> turns "configured but unreachable"
/// into a failure, so the release gate means what it says.
/// </remarks>
public sealed class EngineReachabilityTests
{
    [SkippableFact]
    public void Every_configured_engine_is_reachable()
    {
        Skip.IfNot(
            LiveDatabases.EnginesAreRequired,
            $"Reachability is advisory here; set {LiveDatabases.RequireVariable}=1 to enforce it.");

        var missing = LiveDatabases.ConfiguredButUnreachable();

        Assert.True(
            missing.Count == 0,
            $"These engines were configured but could not be reached, so their suites silently skipped: "
            + $"{string.Join(", ", missing)}.");
    }
}
