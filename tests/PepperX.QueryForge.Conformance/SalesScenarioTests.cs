using FluentAssertions;
using Xunit;

namespace PepperX.QueryForge.Conformance;

/// <summary>
/// The library used the way an application actually uses it.
/// </summary>
/// <remarks>
/// Where the conformance suite checks each input in isolation, these are whole requests: the payload
/// a sales dashboard, an export button, or a data grid would send, and the answer the business
/// expects back. Every expected value was worked out from <see cref="SalesData"/> independently of
/// the engine, so a wrong answer fails here rather than being quietly re-asserted.
/// <para>
/// Each provider subclasses this, so a scenario that works on one database has to work on all of
/// them.
/// </para>
/// </remarks>
public abstract class SalesScenarioTests
{
    /// <summary>Executes a query against this provider's seeded copy of <see cref="SalesData"/>.</summary>
    protected abstract Task<QueryResult<SalesOrder>> RunAsync(Query query);

    #region Helpers

    private static QueryCriteria Where(params Condition[] conditions)
        => new([new ConditionGroup(conditions)]);

    private static QueryCriteria WhereAny(params Condition[] conditions)
        => new([new ConditionGroup(conditions, Logic.Or)]);

    private static ConditionGroup All(params Condition[] conditions) => new(conditions);

    private static ConditionGroup None(params Condition[] conditions) => new(conditions, Logic.AndNot);

    private static QueryPaging Everything => new(Size: 100);

    private async Task<int[]> OrderIdsAsync(Query query)
        => (await RunAsync(query)).Models.Select(o => o.OrderId).ToArray();

    private static IEnumerable<SalesOrder> Leaves(IReadOnlyList<HierarchyNode<SalesOrder>> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var item in node.Items ?? [])
                yield return item;

            foreach (var item in Leaves(node.SubGroups ?? []))
                yield return item;
        }
    }

    #endregion

    #region Sales reporting

    [SkippableFact]
    public async Task Report_DeliveredGermanOrders_LargestFirst()
    {
        // "Show me everything we delivered in Germany, biggest order first."
        var result = await RunAsync(new Query
        {
            Criteria = Where(
                new Condition("Status", ConditionOperator.Equals, "Delivered"),
                new Condition("Country", ConditionOperator.Equals, "Germany")),
            SortColumns = [new SortDescriptor("Amount", SortOrder.Descending)],
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(4);
        result.Models.Select(o => o.OrderId).Should().Equal(5, 25, 1, 31);
        result.Models.First().Amount.Should().Be(2750.00);
    }

    [SkippableFact]
    public async Task Report_TopThreeOrdersByValue()
    {
        // "What are our three biggest orders, ever?"
        var result = await RunAsync(new Query
        {
            SortColumns = [new SortDescriptor("Amount", SortOrder.Descending)],
            Paging = new QueryPaging(Size: 3, Number: 1)
        });

        result.Models.Select(o => o.OrderId).Should().Equal(33, 17, 26);
        result.Models.Select(o => o.Amount).Should().Equal(10500.00, 9100.00, 8250.00);
        result.Meta.Total.Rows.Should().Be(36, "the total describes the whole book, not the page");
    }

    [SkippableFact]
    public async Task Report_FirstQuarterOfTheYear()
    {
        // "Everything placed in Q1 2024." A date range is a Between.
        var ids = await OrderIdsAsync(new Query
        {
            Criteria = Where(new Condition(
                "PlacedOn", ConditionOperator.Between, "2024-01-01", "2024-03-31")),
            SortColumns = [new SortDescriptor("OrderId")],
            Paging = Everything
        });

        ids.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9);
    }

    [SkippableFact]
    public async Task Report_HighValueGoldCustomers_NewestFirst_FirstPage()
    {
        // "Gold customers spending over 2000, newest first — first five."
        var result = await RunAsync(new Query
        {
            Criteria = Where(
                new Condition("Tier", ConditionOperator.Equals, "Gold"),
                new Condition("Amount", ConditionOperator.GreaterThan, 2000)),
            SortColumns = [new SortDescriptor("PlacedOn", SortOrder.Descending)],
            Paging = new QueryPaging(Size: 5, Number: 1)
        });

        result.Meta.Total.Rows.Should().Be(10);
        result.Meta.Total.Pages.Should().Be(2);
        result.Models.Select(o => o.OrderId).Should().Equal(34, 30, 19, 16, 14);
    }

    [SkippableFact]
    public async Task Report_MidSizedOrders_ByQuantityRange()
    {
        // "Orders of between 10 and 30 units." Between is inclusive at both ends.
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Quantity", ConditionOperator.Between, 10, 30)),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(15);
        result.Models.Should().OnlyContain(o => o.Quantity >= 10 && o.Quantity <= 30);
    }

    #endregion

    #region Operational queries

    [SkippableFact]
    public async Task Operations_OrdersNotYetShipped()
    {
        // "What is still sitting unshipped?" — a null ship date is the whole question.
        var ids = await OrderIdsAsync(new Query
        {
            Criteria = Where(new Condition("ShippedOn", ConditionOperator.Equals, null)),
            SortColumns = [new SortDescriptor("OrderId")],
            Paging = Everything
        });

        ids.Should().Equal(4, 6, 10, 12, 17, 20, 27, 29, 35);
    }

    [SkippableFact]
    public async Task Operations_ShippedOrdersOnly()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("ShippedOn", ConditionOperator.NotEquals, null)),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(27);
        result.Models.Should().OnlyContain(o => o.ShippedOn != null);
    }

    [SkippableFact]
    public async Task Operations_CustomersWithoutATier()
    {
        // "Which accounts has nobody tiered yet?"
        var ids = await OrderIdsAsync(new Query
        {
            Criteria = Where(new Condition("Tier", ConditionOperator.Equals, null)),
            SortColumns = [new SortDescriptor("OrderId")],
            Paging = Everything
        });

        ids.Should().Equal(4, 8, 12, 17, 23, 28, 33);
    }

    [SkippableFact]
    public async Task Operations_EverythingExceptCancelled()
    {
        // "Exclude the cancellations." A negated group.
        var result = await RunAsync(new Query
        {
            Criteria = new QueryCriteria([None(new Condition("Status", ConditionOperator.Equals, "Cancelled"))]),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(32);
        result.Models.Should().NotContain(o => o.Status == "Cancelled");
    }

    [SkippableFact]
    public async Task Operations_LivePriorityOrders()
    {
        // "Priority orders that haven't been cancelled" — one plain group, one negated.
        var ids = await OrderIdsAsync(new Query
        {
            Criteria = new QueryCriteria(
                [
                    All(new Condition("Priority", ConditionOperator.Equals, true)),
                    None(new Condition("Status", ConditionOperator.Equals, "Cancelled"))
                ],
                Logic.And),
            SortColumns = [new SortDescriptor("OrderId")],
            Paging = Everything
        });

        ids.Should().Equal(1, 3, 7, 11, 14, 16, 17, 22, 26, 30, 33);
    }

    [SkippableFact]
    public async Task Operations_GoldOrSilverAccounts()
    {
        // "Our top two tiers." One group, OR inside.
        var result = await RunAsync(new Query
        {
            Criteria = WhereAny(
                new Condition("Tier", ConditionOperator.Equals, "Gold"),
                new Condition("Tier", ConditionOperator.Equals, "Silver")),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(21);
        result.Models.Should().OnlyContain(o => o.Tier == "Gold" || o.Tier == "Silver");
    }

    #endregion

    #region Search

    [SkippableFact]
    public async Task Search_CustomerNameContains()
    {
        // The search box on the orders screen.
        var ids = await OrderIdsAsync(new Query
        {
            Criteria = Where(new Condition("Customer", ConditionOperator.Contains, "GmbH")),
            SortColumns = [new SortDescriptor("OrderId")],
            Paging = Everything
        });

        ids.Should().Equal(1, 13);
    }

    [SkippableFact]
    public async Task Search_OrderNumberPrefix_FindsAYearsOrders()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Number", ConditionOperator.StartsWith, "ORD-2023")),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(8);
        result.Models.Should().OnlyContain(o => o.PlacedOn.Year == 2023);
    }

    [SkippableFact]
    public async Task Search_WithNoMatches_ReturnsAnEmptyPageNotAnError()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Customer", ConditionOperator.Contains, "Nonexistent Holdings")),
            Paging = Everything
        });

        result.Models.Should().BeEmpty();
        result.Meta.Total.Rows.Should().Be(0);
        result.Meta.Total.Pages.Should().Be(0);
    }

    #endregion

    #region Dashboards and grouping

    [SkippableFact]
    public async Task Dashboard_OrdersByRegion_IncludingUnmappedCountries()
    {
        // "Break the book down by region." Two countries have no region yet; they must not vanish.
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Region")],
            Paging = Everything
        });

        result.Meta.Type.Should().Be(QueryResultType.Grouped);
        result.Meta.Total.Rows.Should().Be(4, "AMER, APAC, EMEA and the unmapped group");

        result.Groups.Single(g => Equals(g.Key, "EMEA")).Count.Should().Be(17);
        result.Groups.Single(g => Equals(g.Key, "AMER")).Count.Should().Be(11);
        result.Groups.Single(g => Equals(g.Key, "APAC")).Count.Should().Be(6);
        result.Groups.Single(g => g.Key is null).Count.Should().Be(2);

        result.Groups.Sum(g => g.Count).Should().Be(36, "every order lands in exactly one region");
    }

    [SkippableFact]
    public async Task Dashboard_SalesRepLeaderboard()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Rep")],
            Paging = Everything
        });

        result.Groups.Single(g => Equals(g.Key, "Ines Braun")).Count.Should().Be(18);
        result.Groups.Single(g => Equals(g.Key, "Marc Leblanc")).Count.Should().Be(11);
        result.Groups.Single(g => Equals(g.Key, "Yuki Tanaka")).Count.Should().Be(7);
    }

    [SkippableFact]
    public async Task Dashboard_CountryThenChannel_DrillDown()
    {
        // The classic two-level drill-down a grid renders.
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Country"), new GroupByDescriptor("Channel")],
            Paging = Everything
        });

        var germany = result.Groups.Single(g => Equals(g.Key, "Germany"));

        germany.Count.Should().Be(6);
        germany.Items.Should().BeNull("a parent level holds sub-groups");
        germany.SubGroups.Should().HaveCount(3);

        germany.SubGroups!.Single(g => Equals(g.Key, "Web")).Count.Should().Be(3);
        germany.SubGroups!.Single(g => Equals(g.Key, "Store")).Count.Should().Be(2);
        germany.SubGroups!.Single(g => Equals(g.Key, "Partner")).Count.Should().Be(1);
    }

    [SkippableFact]
    public async Task Dashboard_PartnerChannelByStatus()
    {
        // "How is the partner channel doing?" — filter first, then group.
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Channel", ConditionOperator.Equals, "Partner")),
            GroupByColumns = [new GroupByDescriptor("Status")],
            Paging = Everything
        });

        result.Groups.Sum(g => g.Count).Should().Be(9);
        result.Groups.Single(g => Equals(g.Key, "Delivered")).Count.Should().Be(6);
        result.Groups.Single(g => Equals(g.Key, "Pending")).Count.Should().Be(2);
        result.Groups.Single(g => Equals(g.Key, "Shipped")).Count.Should().Be(1);
    }

    [SkippableFact]
    public async Task Dashboard_TierBreakdown_PagedOverGroups()
    {
        // A grid showing two tiers at a time. Page size counts groups, not orders.
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Tier")],
            Paging = new QueryPaging(Size: 2, Number: 1)
        });

        result.Meta.Total.Rows.Should().Be(4, "Bronze, Gold, Silver and the untiered group");
        result.Meta.Total.Pages.Should().Be(2);

        // Ascending with nulls first: untiered, then Bronze.
        result.Groups.Should().HaveCount(2);
        result.Groups[0].Key.Should().BeNull();
        result.Groups[0].Count.Should().Be(7);
        result.Groups[1].Key.Should().Be("Bronze");
        result.Groups[1].Count.Should().Be(8);
    }

    [SkippableFact]
    public async Task Dashboard_DrillDownRowsAreOrderedByTheRequestedSort()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Country", ConditionOperator.Equals, "Japan")),
            GroupByColumns = [new GroupByDescriptor("Channel")],
            SortColumns = [new SortDescriptor("Amount", SortOrder.Descending)],
            Paging = Everything
        });

        var store = result.Groups.Single(g => Equals(g.Key, "Store"));

        store.Items!.Select(o => o.Amount).Should().BeInDescendingOrder();
    }

    [SkippableFact]
    public async Task Dashboard_EveryOrderAppearsExactlyOnceAcrossTheHierarchy()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Region"), new GroupByDescriptor("Status")],
            Paging = Everything
        });

        var ids = Leaves(result.Groups).Select(o => o.OrderId).ToList();

        ids.Should().HaveCount(36);
        ids.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Grids, exports and paging behaviour

    [SkippableFact]
    public async Task Grid_SecondPage_ContinuesWhereTheFirstEnded()
    {
        // EMEA deliveries, by country then value — the second page of three per page.
        var query = new Query
        {
            Criteria = Where(
                new Condition("Region", ConditionOperator.Equals, "EMEA"),
                new Condition("Status", ConditionOperator.Equals, "Delivered")),
            SortColumns =
            [
                new SortDescriptor("Country"),
                new SortDescriptor("Amount", SortOrder.Descending)
            ],
            Paging = new QueryPaging(Size: 3, Number: 2)
        };

        var result = await RunAsync(query);

        result.Meta.Total.Rows.Should().Be(12);
        result.Meta.Total.Pages.Should().Be(4);
        result.Models.Select(o => o.OrderId).Should().Equal(31, 26, 28);
    }

    [SkippableFact]
    public async Task Grid_PagingThroughEverything_YieldsEachOrderExactlyOnce()
    {
        // Scrolling a grid to the end must not drop or repeat a row.
        var seen = new List<int>();

        for (var page = 1; page <= 5; page++)
        {
            var result = await RunAsync(new Query
            {
                SortColumns = [new SortDescriptor("OrderId")],
                Paging = new QueryPaging(Size: 8, Number: page)
            });

            seen.AddRange(result.Models.Select(o => o.OrderId));
        }

        seen.Should().HaveCount(36);
        seen.Should().OnlyHaveUniqueItems();
        seen.Should().BeInAscendingOrder();
    }

    [SkippableFact]
    public async Task Export_OnlyTheChosenColumnsComeBack()
    {
        // "Export order number, customer and amount." Nothing else should travel.
        var result = await RunAsync(new Query
        {
            SelectColumns = ["Number", "Customer", "Amount"],
            SortColumns = [new SortDescriptor("Amount", SortOrder.Descending)],
            Paging = new QueryPaging(Size: 3)
        });

        var top = result.Models.First();

        top.Number.Should().Be("ORD-2024-0033");
        top.Customer.Should().Be("Granite Partners");
        top.Amount.Should().Be(10500.00);

        top.Country.Should().BeEmpty("Country was not exported");
        top.Quantity.Should().Be(0, "Quantity was not exported");
        top.Tier.Should().BeNull("Tier was not exported");
    }

    [SkippableFact]
    public async Task Export_InternalNotesNeverLeaveTheDatabase()
    {
        // The column no endpoint offers. If it ever arrives, something is leaking.
        var result = await RunAsync(new Query
        {
            SelectColumns = ["Number", "Customer"],
            Paging = Everything
        });

        result.Models.Should().OnlyContain(o => o.InternalNotes == string.Empty);
    }

    [SkippableFact]
    public async Task Grid_RequestingAnUnknownColumn_IsIgnoredRatherThanFailing()
    {
        // An older client sends a column that has since been removed.
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("LegacyCode", ConditionOperator.Equals, "X1")),
            SortColumns = [new SortDescriptor("AlsoGone")],
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(36, "an unknown filter is dropped, not treated as match-nothing");
    }

    [SkippableFact]
    public async Task Grid_EmptyFilterFromAnUntouchedForm_ReturnsEverything()
    {
        // The user opened the filter panel and changed nothing.
        var result = await RunAsync(new Query
        {
            Criteria = Where(
                new Condition("Country", ConditionOperator.Contains, null),
                new Condition("Amount", ConditionOperator.GreaterThan, null)),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(36);
    }

    #endregion

    #region Safety

    [SkippableFact]
    public async Task Safety_InjectionAttemptInAColumnName_ChangesNothing()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition(
                "Amount\"; DROP TABLE SalesOrder; --", ConditionOperator.GreaterThan, 0)),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(36);

        // And the data is still there afterwards.
        var after = await RunAsync(new Query { Paging = Everything });
        after.Meta.Total.Rows.Should().Be(36);
    }

    [SkippableFact]
    public async Task Safety_InjectionAttemptInAValue_IsTreatedAsText()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition(
                "Customer", ConditionOperator.Equals, "'; DELETE FROM SalesOrder; --")),
            Paging = Everything
        });

        result.Models.Should().BeEmpty("no customer is called that");

        var after = await RunAsync(new Query { Paging = Everything });
        after.Meta.Total.Rows.Should().Be(36);
    }

    [SkippableFact]
    public async Task Safety_WildcardsInASearchTermAreLiteral()
    {
        // A user typing "%" into search must not match every customer.
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Customer", ConditionOperator.Contains, "%")),
            Paging = Everything
        });

        result.Models.Should().BeEmpty("no customer name contains a percent sign");
    }

    [SkippableFact]
    public async Task Safety_ValidationClampsAnOversizedPageRequest()
    {
        // A client asking for 10,000 rows at once.
        var query = new Query { Paging = new QueryPaging(Size: 10_000, Number: 1) };

        query.Validate(rules => rules.PageSize(p => p.Max(25)), QueryValidationMode.SilentStrip);

        var result = await RunAsync(query);

        result.Models.Should().HaveCount(25);
        result.Meta.Total.Rows.Should().Be(36);
    }

    [SkippableFact]
    public async Task Safety_ValidationStripsADeniedColumnBeforeItReachesTheProvider()
    {
        var query = new Query
        {
            SelectColumns = ["Number", "Customer", "InternalNotes"],
            Paging = new QueryPaging(Size: 5)
        };

        query.Validate(rules => rules.Select(c => c.Deny("InternalNotes")), QueryValidationMode.SilentStrip);

        var result = await RunAsync(query);

        result.Models.Should().OnlyContain(o => o.InternalNotes == string.Empty);
        result.Models.Should().OnlyContain(o => o.Customer != string.Empty);
    }

    #endregion

    #region Client payloads

    [SkippableFact]
    public async Task ClientPayload_DeserializedFromJson_BehavesTheSameAsAHandBuiltQuery()
    {
        // Exactly what a browser posts: enums as numbers, values loosely typed.
        const string json =
            """
            {
              "criteria": {
                "logic": 0,
                "groups": [
                  {
                    "logic": 0,
                    "conditions": [
                      { "columnName": "Tier", "operator": 0, "value": "Gold" },
                      { "columnName": "Amount", "operator": 7, "value": "2000" }
                    ]
                  }
                ]
              },
              "paging": { "size": 5, "number": 1 },
              "sortColumns": [ { "columnName": "PlacedOn", "sortOrder": 1 } ]
            }
            """;

        var query = System.Text.Json.JsonSerializer.Deserialize<Query>(
            json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var result = await RunAsync(query);

        // Same answer as Report_HighValueGoldCustomers_NewestFirst_FirstPage, which built it in C#.
        result.Meta.Total.Rows.Should().Be(10);
        result.Models.Select(o => o.OrderId).Should().Equal(34, 30, 19, 16, 14);
    }

    [SkippableFact]
    public async Task ClientPayload_NumericValueArrivingAsText_StillComparesNumerically()
    {
        // JSON has no int/decimal distinction the way the database does.
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Quantity", ConditionOperator.GreaterThan, "9")),
            Paging = Everything
        });

        result.Models.Should().OnlyContain(o => o.Quantity > 9);
        result.Meta.Total.Rows.Should().Be(24);
    }

    [SkippableFact]
    public async Task ClientPayload_BooleanFilterFromACheckbox()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Where(new Condition("Priority", ConditionOperator.Equals, true)),
            Paging = Everything
        });

        result.Meta.Total.Rows.Should().Be(11);
        result.Models.Should().OnlyContain(o => o.Priority);
    }

    #endregion
}
