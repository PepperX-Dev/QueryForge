namespace PepperX.QueryForge.Conformance;

/// <summary>
/// A sales order, shaped the way a real reporting endpoint would expose one.
/// </summary>
/// <remarks>
/// The business-scenario suite queries this rather than an abstract test model, so the tests read
/// like the requests an application actually sends: filter by tier and country, group by region,
/// page a grid, export three columns.
/// <para>
/// <see cref="InternalNotes"/> is the control: no scenario ever asks for it, so any test that finds
/// it populated has caught a column leaking out of the projection.
/// </para>
/// </remarks>
public sealed class SalesOrder
{
    public int OrderId { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    /// <summary>Gold, Silver, Bronze — or null for a customer who has not been assigned one.</summary>
    public string? Tier { get; set; }

    public string Country { get; set; } = string.Empty;

    /// <summary>EMEA, AMER, APAC — or null for a country not yet mapped to a region.</summary>
    public string? Region { get; set; }

    public string Channel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public double Amount { get; set; }

    public double Discount { get; set; }

    public bool Priority { get; set; }

    public DateTime PlacedOn { get; set; }

    /// <summary>Null until the order ships, which makes it the natural "not yet shipped" filter.</summary>
    public DateTime? ShippedOn { get; set; }

    public string Rep { get; set; } = string.Empty;

    /// <summary>Never exposed to a caller. Present so tests can prove it stays that way.</summary>
    public string InternalNotes { get; set; } = string.Empty;
}

/// <summary>The fixed order book every business scenario runs against.</summary>
public static class SalesData
{
    /// <summary>
    /// Thirty-six orders across two years, nine countries, four statuses and three sales reps, with
    /// nulls deliberately present in tier, region and ship date so the awkward cases are reachable.
    /// </summary>
    public static IReadOnlyList<SalesOrder> All { get; } =
    [
        New(1, "ORD-2024-0001", "Adler GmbH", "Gold", "Germany", "EMEA", "Web", "Delivered", 12, 1450.0, 10.0, true, new DateTime(2024, 1, 15), new DateTime(2024, 1, 18), "Ines Braun"),
        New(2, "ORD-2024-0002", "Brightwave Ltd", "Silver", "United Kingdom", "EMEA", "Web", "Delivered", 3, 220.5, 0.0, false, new DateTime(2024, 1, 22), new DateTime(2024, 1, 26), "Ines Braun"),
        New(3, "ORD-2024-0003", "Cedar Foods", "Bronze", "Canada", "AMER", "Store", "Shipped", 40, 3890.0, 15.0, true, new DateTime(2024, 2, 3), new DateTime(2024, 2, 5), "Marc Leblanc"),
        New(4, "ORD-2024-0004", "Delta Systems", null, "United States", "AMER", "Partner", "Pending", 1, 99.99, 0.0, false, new DateTime(2024, 2, 11), null, "Marc Leblanc"),
        New(5, "ORD-2024-0005", "Echo Retail", "Gold", "Germany", "EMEA", "Store", "Delivered", 25, 2750.0, 5.0, false, new DateTime(2024, 2, 19), new DateTime(2024, 2, 23), "Ines Braun"),
        New(6, "ORD-2024-0006", "Fjord Marine", "Silver", "Norway", "EMEA", "Web", "Cancelled", 8, 640.0, 0.0, false, new DateTime(2024, 3, 2), null, "Ines Braun"),
        New(7, "ORD-2024-0007", "Gakuen Corp", "Gold", "Japan", "APAC", "Partner", "Delivered", 60, 7200.0, 20.0, true, new DateTime(2024, 3, 8), new DateTime(2024, 3, 14), "Yuki Tanaka"),
        New(8, "ORD-2024-0008", "Harbor Logistics", null, "United States", "AMER", "Web", "Shipped", 5, 410.0, 0.0, false, new DateTime(2024, 3, 15), new DateTime(2024, 3, 19), "Marc Leblanc"),
        New(9, "ORD-2024-0009", "Iberia Verde", "Bronze", "Spain", "EMEA", "Web", "Delivered", 18, 1320.75, 7.5, false, new DateTime(2024, 3, 27), new DateTime(2024, 4, 1), "Ines Braun"),
        New(10, "ORD-2024-0010", "Jade Textiles", "Silver", "Japan", "APAC", "Store", "Pending", 22, 1980.0, 0.0, false, new DateTime(2024, 4, 4), null, "Yuki Tanaka"),
        New(11, "ORD-2024-0011", "Kestrel Air", "Gold", "United Kingdom", "EMEA", "Partner", "Delivered", 9, 5400.0, 12.5, true, new DateTime(2024, 4, 12), new DateTime(2024, 4, 15), "Ines Braun"),
        New(12, "ORD-2024-0012", "Lumen Studio", null, "Canada", "AMER", "Web", "Cancelled", 2, 150.0, 0.0, false, new DateTime(2024, 4, 20), null, "Marc Leblanc"),
        New(13, "ORD-2024-0013", "Meridian GmbH", "Silver", "Germany", "EMEA", "Web", "Shipped", 14, 1120.0, 0.0, false, new DateTime(2024, 5, 6), new DateTime(2024, 5, 9), "Ines Braun"),
        New(14, "ORD-2024-0014", "Northstar Inc", "Gold", "United States", "AMER", "Store", "Delivered", 33, 4125.0, 10.0, true, new DateTime(2024, 5, 14), new DateTime(2024, 5, 17), "Marc Leblanc"),
        New(15, "ORD-2024-0015", "Orchid Cafe", "Bronze", "Japan", "APAC", "Web", "Delivered", 6, 275.0, 0.0, false, new DateTime(2024, 5, 23), new DateTime(2024, 5, 29), "Yuki Tanaka"),
        New(16, "ORD-2024-0016", "Pinnacle Bau", "Gold", "Germany", "EMEA", "Partner", "Shipped", 50, 6300.0, 15.0, true, new DateTime(2024, 6, 1), new DateTime(2024, 6, 4), "Ines Braun"),
        New(17, "ORD-2024-0017", "Quartz Mining", null, "Canada", "AMER", "Partner", "Pending", 75, 9100.0, 0.0, true, new DateTime(2024, 6, 9), null, "Marc Leblanc"),
        New(18, "ORD-2024-0018", "Riviera Hotels", "Silver", "Spain", "EMEA", "Store", "Delivered", 11, 880.0, 5.0, false, new DateTime(2024, 6, 18), new DateTime(2024, 6, 21), "Ines Braun"),
        New(19, "ORD-2024-0019", "Sakura Foods", "Gold", "Japan", "APAC", "Store", "Delivered", 28, 3360.0, 10.0, false, new DateTime(2024, 6, 25), new DateTime(2024, 6, 28), "Yuki Tanaka"),
        New(20, "ORD-2024-0020", "Tundra Gear", "Bronze", "Norway", "EMEA", "Web", "Cancelled", 4, 310.0, 0.0, false, new DateTime(2024, 7, 2), null, "Ines Braun"),
        New(21, "ORD-2023-0021", "Umbra Design", "Silver", "United Kingdom", "EMEA", "Web", "Delivered", 7, 560.0, 0.0, false, new DateTime(2023, 9, 14), new DateTime(2023, 9, 18), "Ines Braun"),
        New(22, "ORD-2023-0022", "Vertex Labs", "Gold", "United States", "AMER", "Partner", "Delivered", 45, 5850.0, 18.0, true, new DateTime(2023, 10, 5), new DateTime(2023, 10, 9), "Marc Leblanc"),
        New(23, "ORD-2023-0023", "Willow Craft", null, "Canada", "AMER", "Store", "Delivered", 13, 995.0, 0.0, false, new DateTime(2023, 10, 21), new DateTime(2023, 10, 25), "Marc Leblanc"),
        New(24, "ORD-2023-0024", "Xenon Tech", "Silver", "Japan", "APAC", "Web", "Shipped", 19, 1710.0, 5.0, false, new DateTime(2023, 11, 8), new DateTime(2023, 11, 13), "Yuki Tanaka"),
        New(25, "ORD-2023-0025", "Yield Agrar", "Bronze", "Germany", "EMEA", "Store", "Delivered", 30, 2400.0, 0.0, false, new DateTime(2023, 11, 26), new DateTime(2023, 11, 30), "Ines Braun"),
        New(26, "ORD-2023-0026", "Zephyr Wind", "Gold", "Norway", "EMEA", "Partner", "Delivered", 55, 8250.0, 22.5, true, new DateTime(2023, 12, 3), new DateTime(2023, 12, 8), "Ines Braun"),
        New(27, "ORD-2023-0027", "Anchor Foods", "Silver", "United States", "AMER", "Web", "Cancelled", 16, 1280.0, 0.0, false, new DateTime(2023, 12, 15), null, "Marc Leblanc"),
        New(28, "ORD-2023-0028", "Basalt Stone", null, "Spain", "EMEA", "Store", "Delivered", 21, 1575.0, 0.0, false, new DateTime(2023, 12, 27), new DateTime(2023, 12, 30), "Ines Braun"),
        New(29, "ORD-2024-0029", "Copper Rail", "Bronze", "Canada", "AMER", "Web", "Pending", 2, 180.0, 0.0, false, new DateTime(2024, 7, 11), null, "Marc Leblanc"),
        New(30, "ORD-2024-0030", "Dune Ventures", "Gold", "United Arab Emirates", null, "Partner", "Delivered", 38, 6650.0, 17.5, true, new DateTime(2024, 7, 19), new DateTime(2024, 7, 23), "Ines Braun"),
        New(31, "ORD-2024-0031", "Ember Works", "Silver", "Germany", "EMEA", "Web", "Delivered", 10, 760.0, 2.5, false, new DateTime(2024, 7, 26), new DateTime(2024, 7, 30), "Ines Braun"),
        New(32, "ORD-2024-0032", "Flint & Co", "Bronze", "United Kingdom", "EMEA", "Store", "Shipped", 26, 2080.0, 0.0, false, new DateTime(2024, 8, 5), new DateTime(2024, 8, 8), "Ines Braun"),
        New(33, "ORD-2024-0033", "Granite Partners", null, "United States", "AMER", "Partner", "Delivered", 70, 10500.0, 25.0, true, new DateTime(2024, 8, 13), new DateTime(2024, 8, 16), "Marc Leblanc"),
        New(34, "ORD-2024-0034", "Hinoki Interiors", "Gold", "Japan", "APAC", "Web", "Delivered", 17, 2210.0, 10.0, false, new DateTime(2024, 8, 21), new DateTime(2024, 8, 24), "Yuki Tanaka"),
        New(35, "ORD-2024-0035", "Ironbark Ltd", "Silver", "Australia", null, "Web", "Pending", 9, 720.0, 0.0, false, new DateTime(2024, 8, 29), null, "Yuki Tanaka"),
        New(36, "ORD-2024-0036", "Juniper Spa", "Bronze", "Spain", "EMEA", "Store", "Delivered", 5, 395.0, 0.0, false, new DateTime(2024, 9, 4), new DateTime(2024, 9, 7), "Ines Braun")
    ];

    /// <summary>A fresh, independent copy, so one provider's run cannot affect another's.</summary>
    public static List<SalesOrder> Fresh() => All.Select(o => new SalesOrder
    {
        OrderId = o.OrderId,
        Number = o.Number,
        Customer = o.Customer,
        Tier = o.Tier,
        Country = o.Country,
        Region = o.Region,
        Channel = o.Channel,
        Status = o.Status,
        Quantity = o.Quantity,
        Amount = o.Amount,
        Discount = o.Discount,
        Priority = o.Priority,
        PlacedOn = o.PlacedOn,
        ShippedOn = o.ShippedOn,
        InternalNotes = o.InternalNotes,
        Rep = o.Rep
    }).ToList();

    private static SalesOrder New(
        int id, string number, string customer, string? tier, string country, string? region,
        string channel, string status, int quantity, double amount, double discount, bool priority,
        DateTime placedOn, DateTime? shippedOn, string rep)
        => new()
        {
            OrderId = id,
            Number = number,
            Customer = customer,
            Tier = tier,
            Country = country,
            Region = region,
            Channel = channel,
            Status = status,
            Quantity = quantity,
            Amount = amount,
            Discount = discount,
            Priority = priority,
            PlacedOn = placedOn,
            ShippedOn = shippedOn,
            Rep = rep,
            InternalNotes = $"internal-{id}"
        };
}
