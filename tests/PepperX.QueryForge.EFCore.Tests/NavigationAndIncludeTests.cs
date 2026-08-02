using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PepperX.QueryForge.EFCore;
using Xunit;

namespace PepperX.QueryForge.EFCore.Tests;

/// <summary>
/// Covers what happens when a query carries navigation properties — because a caller names columns by
/// string, and a navigation is a property like any other.
/// </summary>
/// <remarks>
/// SQLite rather than the in-memory provider, so every expression really is translated to SQL. A
/// fragment EF Core cannot translate fails the test here instead of quietly evaluating on the client,
/// which matters because one of the two defects these tests pin was exactly that — an ordering over a
/// collection navigation, which throws. The other two are quiet rather than loud: a reference
/// navigation used as a group key would serialize the whole related entity, and a projection applied
/// over an <c>Include</c> would return empty navigations with nothing in the response to say so.
/// </remarks>
public sealed class NavigationAndIncludeTests : IDisposable
{
    #region Model

    public sealed class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Tier { get; set; }
        public List<Order> Orders { get; set; } = [];
    }

    public sealed class Order
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime PlacedOn { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;      // reference navigation
        public List<OrderLine> Lines { get; set; } = [];     // collection navigation
    }

    public sealed class OrderLine
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    private sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderLine> Lines => Set<OrderLine>();
    }

    #endregion

    private readonly SqliteConnection _connection;
    private readonly ShopContext _context;

    public NavigationAndIncludeTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _context = new ShopContext(
            new DbContextOptionsBuilder<ShopContext>().UseSqlite(_connection).Options);

        _context.Database.EnsureCreated();

        _context.Customers.AddRange(
            new Customer { Id = 1, Name = "Acme", Tier = "Gold" },
            new Customer { Id = 2, Name = "Umbrella", Tier = null });

        _context.Orders.AddRange(
            new Order
            {
                Id = 1, Number = "SO-1", Total = 100m, CustomerId = 1,
                PlacedOn = new DateTime(2024, 1, 1),
                Lines = [ new OrderLine { Id = 1, Sku = "A", Quantity = 2 },
                          new OrderLine { Id = 2, Sku = "B", Quantity = 1 } ]
            },
            new Order
            {
                Id = 2, Number = "SO-2", Total = 250m, CustomerId = 2,
                PlacedOn = new DateTime(2024, 2, 1),
                Lines = [ new OrderLine { Id = 3, Sku = "C", Quantity = 5 } ]
            },
            new Order
            {
                Id = 3, Number = "SO-3", Total = 50m, CustomerId = 1,
                PlacedOn = new DateTime(2024, 3, 1),
                Lines = [ new OrderLine { Id = 4, Sku = "D", Quantity = 3 } ]
            });

        _context.SaveChanges();

        // Otherwise the change tracker would fix navigations up from the seeded graph, and an Include
        // that never ran would look as though it had.
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    #region Ordering by a navigation

    /// <remarks>
    /// EF Core does translate this one — it orders by the related key — so the point is not a crash
    /// but that ordering by a surrogate key is never what naming "Customer" meant.
    /// </remarks>
    [Fact]
    public async Task Sorting_by_a_reference_navigation_is_dropped()
    {
        var query = QueryBuilder.Sort(new SortDescriptor("Customer")).Page(10).Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Meta.Total.Rows.Should().Be(3, "an unusable sort must not change which rows match");
        result.Models.Should().HaveCount(3);
    }

    /// <remarks>
    /// This is the one that used to crash: EF Core cannot translate an ordering over a collection and
    /// throws, so a caller could take the endpoint down with one column name.
    /// </remarks>
    [Fact]
    public async Task Sorting_by_a_collection_navigation_is_dropped_rather_than_throwing()
    {
        var query = QueryBuilder.Sort(new SortDescriptor("Lines")).Page(10).Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Models.Should().HaveCount(3);
    }

    [Fact]
    public async Task An_unusable_sort_does_not_discard_the_usable_ones_beside_it()
    {
        var query = QueryBuilder
            .Sort(new SortDescriptor("Customer"), new SortDescriptor("Total", SortOrder.Descending))
            .Page(10)
            .Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Models.Select(o => o.Total).Should().BeInDescendingOrder();
    }

    [Theory]
    [InlineData("Number")]      // string
    [InlineData("Total")]       // decimal — not a primitive, so worth pinning
    [InlineData("PlacedOn")]    // DateTime
    [InlineData("CustomerId")]  // int
    public async Task Sorting_by_a_scalar_column_still_works(string column)
    {
        var query = QueryBuilder.Sort(new SortDescriptor(column)).Page(10).Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Models.Should().HaveCount(3, $"'{column}' is orderable and must not be dropped");
    }

    #endregion

    #region Grouping by a navigation

    /// <remarks>
    /// Grouping is where a reference navigation does real damage rather than merely being useless: the
    /// key would be the whole related entity, so every one of its columns — including any the caller
    /// was never offered — would be serialized into <c>HierarchyNode.Key</c>.
    /// </remarks>
    [Fact]
    public async Task Grouping_by_a_navigation_falls_back_to_a_flat_result()
    {
        var query = QueryBuilder.GroupBy(new GroupByDescriptor("Customer")).Page(10).Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Meta.Type.Should().Be(QueryResultType.Flat, "no grouping level survived");
        result.Models.Should().HaveCount(3);
        result.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Grouping_still_works_on_a_scalar_beside_an_unusable_level()
    {
        var query = QueryBuilder
            .GroupBy(new GroupByDescriptor("Customer"), new GroupByDescriptor("CustomerId"))
            .Page(10)
            .Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Meta.Type.Should().Be(QueryResultType.Grouped);
        result.Groups.Should().HaveCount(2, "two distinct CustomerId values");
        result.Groups.Sum(g => g.Count).Should().Be(3);
    }

    [Fact]
    public async Task A_group_key_never_carries_a_related_entity()
    {
        var query = QueryBuilder
            .GroupBy(new GroupByDescriptor("Customer"), new GroupByDescriptor("CustomerId"))
            .Page(10)
            .Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Groups.Select(g => g.Key).Should().NotContainItemsAssignableTo<Customer>(
            "a key is a column value; a related entity would leak every column it has");
    }

    #endregion

    #region Filtering by a navigation

    [Fact]
    public async Task Filtering_a_navigation_against_a_value_is_dropped()
    {
        var query = QueryBuilder
            .Where(new QueryCriteria([
                new ConditionGroup([new Condition("Customer", ConditionOperator.Equals, "Acme")])]))
            .Page(10)
            .Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Meta.Total.Rows.Should().Be(3, "the condition cannot bind and is dropped, not applied");
    }

    #endregion

    #region Include and projection

    [Fact]
    public async Task A_projection_is_skipped_when_the_query_carries_an_include()
    {
        var query = QueryBuilder.Select("Id", "Number").Page(10).Build();

        var result = await _context.Orders
            .Include(o => o.Lines)
            .AsNoTracking()
            .ToQueryResultAsync<Order>(query);

        result.Models.Should().HaveCount(3);
        result.Models.Should().OnlyContain(o => o.Lines.Count > 0,
            "projecting would have made EF Core drop the Include and return empty navigations");
    }

    [Fact]
    public async Task An_include_is_still_found_through_AsSplitQuery_and_AsNoTracking()
    {
        var query = QueryBuilder.Select("Id").Page(10).Build();

        var result = await _context.Orders
            .Include(o => o.Lines)
            .AsSplitQuery()
            .AsNoTracking()
            .ToQueryResultAsync<Order>(query);

        result.Models.Should().OnlyContain(o => o.Lines.Count > 0);
    }

    [Fact]
    public async Task An_include_is_still_found_through_ThenInclude()
    {
        var query = QueryBuilder.Select("Id").Page(10).Build();

        var result = await _context.Customers
            .Include(c => c.Orders)
            .ThenInclude(o => o.Lines)
            .AsNoTracking()
            .ToQueryResultAsync<Customer>(query);

        result.Models.Should().OnlyContain(c => c.Orders.Count > 0);
    }

    [Fact]
    public async Task A_projection_still_applies_when_there_is_no_include()
    {
        var query = QueryBuilder.Select("Id", "Number").Page(10).Build();

        var result = await _context.Orders.AsNoTracking().ToQueryResultAsync<Order>(query);

        result.Models.Should().HaveCount(3);
        result.Models.Should().OnlyContain(o => o.Number != string.Empty, "Number was selected");
        result.Models.Should().OnlyContain(o => o.Total == 0m, "Total was not selected");
    }

    /// <remarks>
    /// The caller's own projection has already cost them the include, so there is nothing left to
    /// protect and <c>SelectColumns</c> must still narrow the result.
    /// </remarks>
    [Fact]
    public async Task A_projection_still_applies_when_the_caller_projected_past_the_include()
    {
        var query = QueryBuilder.Select("Number").Page(10).Build();

        var rows = _context.Orders
            .Include(o => o.Lines)
            .AsNoTracking()
            .Select(o => new Order { Id = o.Id, Number = o.Number, Total = o.Total });

        var result = await rows.ToQueryResultAsync<Order>(query);

        result.Models.Should().OnlyContain(o => o.Number != string.Empty, "Number was selected");
        result.Models.Should().OnlyContain(o => o.Total == 0m, "Total was not selected");
    }

    [Fact]
    public void ApplyQuery_leaves_an_included_query_unprojected()
    {
        var query = QueryBuilder.Select("Id").Page(10).Build();

        var sql = _context.Orders.Include(o => o.Lines).ApplyQuery(query).ToQueryString();

        // The join to the included table is the evidence: a projection would have removed it.
        sql.Should().Contain("JOIN \"Lines\"", "the include must survive ApplyQuery");
    }

    #endregion
}
