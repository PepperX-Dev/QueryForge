using PepperX.QueryForge.Conformance;

namespace PepperX.QueryForge.InMemory.Tests;

/// <summary>The business scenarios, executed against a plain in-memory collection.</summary>
public sealed class InMemorySalesScenarioTests : SalesScenarioTests
{
    private readonly List<SalesOrder> _orders = SalesData.Fresh();

    protected override Task<QueryResult<SalesOrder>> RunAsync(Query query)
        => _orders.ToQueryResultAsync(query);
}
