using PepperX.QueryForge.Conformance;

namespace PepperX.QueryForge.InMemory.Tests;

/// <summary>Runs the shared conformance suite against the in-memory provider.</summary>
public sealed class InMemoryConformanceTests : QueryForgeConformanceTests
{
    private readonly List<Widget> _widgets = WidgetData.Fresh();

    protected override Task<QueryResult<Widget>> RunAsync(Query query)
        => _widgets.ToQueryResultAsync(query);
}
