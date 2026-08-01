namespace PepperX.QueryForge.Conformance;

/// <summary>
/// The model every conformance test queries.
/// </summary>
/// <remarks>
/// Settable properties and a parameterless constructor, because that is what Dapper and EF Core
/// materialize into and what column projection needs.
/// <para>
/// <see cref="Price"/> is a <see cref="double"/> rather than a <see cref="decimal"/> on purpose:
/// SQLite has no decimal type, and EF Core refuses to order by one there. Using a type every backing
/// store agrees on keeps the suite about QueryForge's behaviour instead of a provider quirk.
/// </para>
/// </remarks>
public sealed class Widget
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Nullable, so null can be exercised as a grouping key and a filter target.</summary>
    public string? Category { get; set; }

    /// <summary>Nullable, and the second grouping level.</summary>
    public string? Region { get; set; }

    public int Quantity { get; set; }

    public double Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime ReleasedOn { get; set; }

    /// <summary>
    /// Never selected, sorted, filtered or grouped by any test. It exists so that "a column the
    /// caller was not offered stays unreachable" is something the suite can actually assert.
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}

/// <summary>The fixed data set every provider is seeded with.</summary>
public static class WidgetData
{
    /// <summary>
    /// Twelve rows chosen to make the interesting cases reachable: nulls in both grouping columns,
    /// repeated categories, a quantity spread where lexical ordering would disagree with numeric
    /// ordering (9 vs 30 vs 100), and a name containing LIKE wildcards.
    /// </summary>
    public static IReadOnlyList<Widget> All { get; } =
    [
        New(1, "Anvil", "Tools", "North", 9, 30.00, true, "2020-01-15"),
        New(2, "Bolt", "Parts", "North", 100, 1.50, true, "2021-06-01"),
        New(3, "Chisel", "Tools", "South", 0, 12.00, false, "2019-03-10"),
        New(4, "Drill", "Tools", "North", 7, 99.99, true, "2022-09-20"),
        New(5, "Engine", "Parts", "South", 2, 500.00, false, "2018-07-05"),
        New(6, "Fuse", null, "North", 50, 2.25, true, "2023-02-11"),
        New(7, "Gasket", null, null, 8, 4.75, false, "2021-11-30"),
        New(8, "Hammer", "Tools", "South", 30, 25.00, true, "2020-05-05"),
        New(9, "50%_off", "Promo", "North", 5, 0.99, true, "2024-01-01"),
        New(10, "Idler", "Parts", null, 12, 45.00, false, "2022-03-03"),
        New(11, "Jack", "Tools", "North", 3, 150.00, true, "2019-12-25"),
        New(12, "Kit", "Promo", "South", 1, 75.00, false, "2023-08-08")
    ];

    /// <summary>A fresh, independent copy — so a provider that mutates rows cannot affect another.</summary>
    public static List<Widget> Fresh() => All.Select(w => new Widget
    {
        Id = w.Id,
        Name = w.Name,
        Category = w.Category,
        Region = w.Region,
        Quantity = w.Quantity,
        Price = w.Price,
        IsActive = w.IsActive,
        ReleasedOn = w.ReleasedOn,
        Secret = w.Secret
    }).ToList();

    private static Widget New(
        int id, string name, string? category, string? region,
        int quantity, double price, bool isActive, string releasedOn)
        => new()
        {
            Id = id,
            Name = name,
            Category = category,
            Region = region,
            Quantity = quantity,
            Price = price,
            IsActive = isActive,
            ReleasedOn = DateTime.Parse(releasedOn, System.Globalization.CultureInfo.InvariantCulture),
            Secret = $"secret-{id}"
        };
}
