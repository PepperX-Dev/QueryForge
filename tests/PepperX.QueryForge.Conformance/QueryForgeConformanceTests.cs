using FluentAssertions;
using Xunit;

namespace PepperX.QueryForge.Conformance;

/// <summary>
/// The behaviour every QueryForge execution provider must exhibit.
/// </summary>
/// <remarks>
/// Each provider gets a small subclass supplying <see cref="RunAsync"/>; xUnit then runs this whole
/// suite against it. That is what turns "the providers agree" from a claim into something the build
/// enforces — a change that alters one provider's behaviour fails here rather than in a user's
/// application.
/// <para>
/// Every test is written against <see cref="WidgetData.All"/>, and covers the inputs a caller can
/// send: <c>Criteria</c>, <c>Paging</c>, <c>SelectColumns</c>, <c>SortColumns</c> and
/// <c>GroupByColumns</c>, for both flat and grouped results.
/// </para>
/// </remarks>
public abstract class QueryForgeConformanceTests
{
    /// <summary>Executes a query against this provider's seeded copy of <see cref="WidgetData"/>.</summary>
    protected abstract Task<QueryResult<Widget>> RunAsync(Query query);

    #region Helpers

    protected static QueryCriteria Group(Logic logic, params Condition[] conditions)
        => new([new ConditionGroup(conditions, logic)]);

    protected static QueryCriteria Group(params Condition[] conditions)
        => new([new ConditionGroup(conditions)]);

    protected static QueryCriteria Groups(Logic criteriaLogic, params ConditionGroup[] groups)
        => new(groups, criteriaLogic);

    /// <summary>A page big enough that paging never hides a result a filter test cares about.</summary>
    private static QueryPaging AllRows => new(Size: 100);

    private async Task<int[]> IdsAsync(Query query)
    {
        var result = await RunAsync(query);
        return result.Models.Select(w => w.Id).ToArray();
    }

    #endregion

    #region Baseline

    [SkippableFact]
    public async Task NoInputs_ShouldReturnDefaultPageWithCorrectTotals()
    {
        var result = await RunAsync(new Query());

        result.Meta.Type.Should().Be(QueryResultType.Flat);
        result.Meta.Total.Rows.Should().Be(12);
        result.Meta.Total.Pages.Should().Be(1);
        result.Models.Should().HaveCount(12, "the default page size is 12");
    }

    [SkippableFact]
    public async Task NoGrouping_ShouldLeaveGroupsEmpty()
    {
        var result = await RunAsync(new Query());

        result.Groups.Should().BeEmpty();
    }

    #endregion

    #region Paging

    [SkippableTheory]
    [InlineData(5, 1, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(5, 2, new[] { 6, 7, 8, 9, 10 })]
    [InlineData(5, 3, new[] { 11, 12 })]
    [InlineData(12, 1, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 })]
    public async Task Paging_ShouldReturnTheRequestedWindow(int size, int number, int[] expected)
    {
        var ids = await IdsAsync(new Query
        {
            Paging = new QueryPaging(size, number),
            SortColumns = [new SortDescriptor("Id")]
        });

        ids.Should().Equal(expected);
    }

    [SkippableTheory]
    [InlineData(5, 3)]
    [InlineData(12, 1)]
    [InlineData(7, 2)]
    [InlineData(100, 1)]
    public async Task Paging_ShouldReportPageCountForTheSize(int size, int expectedPages)
    {
        var result = await RunAsync(new Query { Paging = new QueryPaging(size) });

        result.Meta.Total.Rows.Should().Be(12, "totals describe the whole filtered set, not the page");
        result.Meta.Total.Pages.Should().Be(expectedPages);
    }

    [SkippableFact]
    public async Task Paging_BeyondTheLastPage_ShouldReturnNoRowsButKeepTotals()
    {
        var result = await RunAsync(new Query { Paging = new QueryPaging(Size: 5, Number: 99) });

        result.Models.Should().BeEmpty();
        result.Meta.Total.Rows.Should().Be(12);
        result.Meta.Total.Pages.Should().Be(3);
    }

    [SkippableTheory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Paging_WithNonPositiveSize_ShouldFallBackToTheDefault(int size)
    {
        var result = await RunAsync(new Query { Paging = new QueryPaging(Size: size) });

        result.Models.Should().HaveCount(12);
        result.Meta.Total.Pages.Should().Be(1);
    }

    [SkippableTheory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Paging_WithNonPositiveNumber_ShouldBeTreatedAsTheFirstPage(int number)
    {
        var ids = await IdsAsync(new Query
        {
            Paging = new QueryPaging(Size: 3, Number: number),
            SortColumns = [new SortDescriptor("Id")]
        });

        ids.Should().Equal(1, 2, 3);
    }

    [SkippableFact]
    public async Task Paging_ShouldApplyAfterFiltering()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.Equals, "Tools")),
            Paging = new QueryPaging(Size: 2, Number: 1)
        });

        result.Meta.Total.Rows.Should().Be(5, "five widgets are Tools");
        result.Meta.Total.Pages.Should().Be(3);
        result.Models.Should().HaveCount(2);
    }

    #endregion

    #region SortColumns

    [SkippableFact]
    public async Task Sort_Ascending_ShouldOrderByTheColumn()
    {
        var ids = await IdsAsync(new Query
        {
            SortColumns = [new SortDescriptor("Quantity")],
            Paging = AllRows
        });

        ids.Should().Equal(3, 12, 5, 11, 9, 4, 7, 1, 10, 8, 6, 2);
    }

    [SkippableFact]
    public async Task Sort_Descending_ShouldReverseTheOrder()
    {
        var ids = await IdsAsync(new Query
        {
            SortColumns = [new SortDescriptor("Quantity", SortOrder.Descending)],
            Paging = AllRows
        });

        ids.Should().Equal(2, 6, 8, 10, 1, 7, 4, 9, 11, 5, 12, 3);
    }

    [SkippableFact]
    public async Task Sort_ShouldBeNumericNotLexical()
    {
        var ids = await IdsAsync(new Query
        {
            SortColumns = [new SortDescriptor("Quantity")],
            Paging = new QueryPaging(Size: 4)
        });

        // Lexically "0" < "1" < "100" < "12"; numerically 0 < 1 < 2 < 3.
        ids.Should().Equal(3, 12, 5, 11);
    }

    [SkippableFact]
    public async Task Sort_MultipleColumns_ShouldApplyEachLevelInOrder()
    {
        var ids = await IdsAsync(new Query
        {
            SortColumns =
            [
                new SortDescriptor("Category"),
                new SortDescriptor("Quantity", SortOrder.Descending)
            ],
            Paging = AllRows
        });

        // Nulls first, then Parts, Promo, Tools — each internally by quantity descending.
        ids.Should().Equal(6, 7, 2, 10, 5, 9, 12, 8, 1, 4, 11, 3);
    }

    [SkippableFact]
    public async Task Sort_ShouldPlaceNullsFirstWhenAscending()
    {
        var ids = await IdsAsync(new Query
        {
            SortColumns = [new SortDescriptor("Category"), new SortDescriptor("Id")],
            Paging = new QueryPaging(Size: 2)
        });

        ids.Should().Equal(6, 7);
    }

    [SkippableFact]
    public async Task Sort_ShouldPlaceNullsLastWhenDescending()
    {
        var ids = await IdsAsync(new Query
        {
            SortColumns = [new SortDescriptor("Category", SortOrder.Descending), new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids[^2..].Should().Equal(6, 7);
    }

    [SkippableFact]
    public async Task Sort_ByUnknownColumn_ShouldBeIgnored()
    {
        var result = await RunAsync(new Query
        {
            SortColumns = [new SortDescriptor("NoSuchColumn")],
            Paging = AllRows
        });

        result.Models.Should().HaveCount(12);
    }

    [SkippableFact]
    public async Task Sort_ByBoolean_ShouldOrderFalseBeforeTrue()
    {
        var result = await RunAsync(new Query
        {
            SortColumns = [new SortDescriptor("IsActive"), new SortDescriptor("Id")],
            Paging = new QueryPaging(Size: 1)
        });

        result.Models.Single().IsActive.Should().BeFalse();
    }

    [SkippableFact]
    public async Task Sort_ByDate_ShouldOrderChronologically()
    {
        var ids = await IdsAsync(new Query
        {
            SortColumns = [new SortDescriptor("ReleasedOn")],
            Paging = new QueryPaging(Size: 3)
        });

        ids.Should().Equal(5, 3, 11);
    }

    #endregion

    #region SelectColumns

    [SkippableFact]
    public async Task Select_ShouldPopulateOnlyTheRequestedColumns()
    {
        var result = await RunAsync(new Query
        {
            SelectColumns = ["Id", "Name"],
            SortColumns = [new SortDescriptor("Id")],
            Paging = new QueryPaging(Size: 1)
        });

        var widget = result.Models.Single();

        widget.Id.Should().Be(1);
        widget.Name.Should().Be("Anvil");
        widget.Category.Should().BeNull("Category was not selected");
        widget.Quantity.Should().Be(0, "Quantity was not selected");
        widget.Price.Should().Be(0);
    }

    [SkippableFact]
    public async Task Select_Empty_ShouldReturnEveryColumn()
    {
        var result = await RunAsync(new Query
        {
            SortColumns = [new SortDescriptor("Id")],
            Paging = new QueryPaging(Size: 1)
        });

        var widget = result.Models.Single();

        widget.Category.Should().Be("Tools");
        widget.Quantity.Should().Be(9);
    }

    [SkippableFact]
    public async Task Select_ShouldIgnoreUnknownColumnsButHonourTheRest()
    {
        var result = await RunAsync(new Query
        {
            SelectColumns = ["Id", "NoSuchColumn"],
            SortColumns = [new SortDescriptor("Id")],
            Paging = new QueryPaging(Size: 1)
        });

        var widget = result.Models.Single();

        widget.Id.Should().Be(1);
        widget.Name.Should().BeEmpty("Name was not among the selected columns");
    }

    [SkippableFact]
    public async Task Select_WhenEveryColumnIsUnknown_ShouldFallBackToEverything()
    {
        var result = await RunAsync(new Query
        {
            SelectColumns = ["Nope", "AlsoNope"],
            SortColumns = [new SortDescriptor("Id")],
            Paging = new QueryPaging(Size: 1)
        });

        // Blanking the whole model is never what an unusable selection meant.
        result.Models.Single().Name.Should().Be("Anvil");
    }

    [SkippableFact]
    public async Task Select_ShouldNotAffectFilteringOrSorting()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.Equals, "Promo")),
            SelectColumns = ["Id"],
            SortColumns = [new SortDescriptor("Quantity", SortOrder.Descending)],
            Paging = AllRows
        });

        ids.Should().Equal(9, 12);
    }

    [SkippableFact]
    public async Task Select_ShouldNeverExposeAColumnThatWasNotAskedFor()
    {
        var result = await RunAsync(new Query
        {
            SelectColumns = ["Id", "Name"],
            Paging = AllRows
        });

        result.Models.Should().OnlyContain(w => w.Secret == string.Empty);
    }

    #endregion

    #region Criteria — comparison operators

    [SkippableTheory]
    [InlineData(ConditionOperator.Equals, 30, new[] { 8 })]
    [InlineData(ConditionOperator.NotEquals, 30, new[] { 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12 })]
    [InlineData(ConditionOperator.LessThan, 5, new[] { 3, 5, 11, 12 })]
    [InlineData(ConditionOperator.GreaterThan, 30, new[] { 2, 6 })]
    [InlineData(ConditionOperator.LessThanOrEqualTo, 3, new[] { 3, 5, 11, 12 })]
    [InlineData(ConditionOperator.GreaterThanOrEqualTo, 50, new[] { 2, 6 })]
    public async Task Criteria_ComparisonOperators_ShouldFilterNumerically(
        ConditionOperator op, int value, int[] expected)
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Quantity", op, value)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(expected);
    }

    [SkippableFact]
    public async Task Criteria_Between_ShouldIncludeBothBounds()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Quantity", ConditionOperator.Between, 3, 9)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 4, 7, 9, 11);
    }

    [SkippableFact]
    public async Task Criteria_NumericValueSentAsString_ShouldStillCompareNumerically()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Quantity", ConditionOperator.GreaterThan, "30")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(2, 6);
    }

    [SkippableFact]
    public async Task Criteria_OnBooleanColumn_ShouldFilter()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("IsActive", ConditionOperator.Equals, true)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 2, 4, 6, 8, 9, 11);
    }

    [SkippableFact]
    public async Task Criteria_OnDateColumn_ShouldAcceptAnIsoString()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("ReleasedOn", ConditionOperator.GreaterThanOrEqualTo, "2023-01-01")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(6, 9, 12);
    }

    [SkippableFact]
    public async Task Criteria_OnDoubleColumn_ShouldCompareNumerically()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Price", ConditionOperator.GreaterThan, 100)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(5, 11);
    }

    #endregion

    #region Criteria — text operators

    [SkippableFact]
    public async Task Criteria_Contains_ShouldMatchAnywhereInTheValue()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Name", ConditionOperator.Contains, "am")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(8);
    }

    [SkippableFact]
    public async Task Criteria_StartsWith_ShouldAnchorToTheStart()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.StartsWith, "P")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(2, 5, 9, 10, 12);
    }

    [SkippableFact]
    public async Task Criteria_EndsWith_ShouldAnchorToTheEnd()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.EndsWith, "ols")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 3, 4, 8, 11);
    }

    [SkippableFact]
    public async Task Criteria_NotContains_ShouldExcludeMatches()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.NotContains, "ool")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        // Rows 6 and 7 have a null Category and never match a text predicate, negated or not.
        ids.Should().Equal(2, 5, 9, 10, 12);
    }

    [SkippableFact]
    public async Task Criteria_TextOperators_ShouldTreatCallerWildcardsLiterally()
    {
        // "50%_off" must be found by its text; the % and _ must not act as wildcards.
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Name", ConditionOperator.Contains, "%_")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(9);
    }

    [SkippableFact]
    public async Task Criteria_StartsWithWildcard_ShouldNotMatchEverything()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Name", ConditionOperator.StartsWith, "%")),
            Paging = AllRows
        });

        ids.Should().BeEmpty("no name actually starts with a literal percent sign");
    }

    [SkippableFact]
    public async Task Criteria_TextMatching_IsExactWhenTheCasingMatches()
    {
        // Case sensitivity itself follows the backing store's collation, which QueryForge does not
        // override — forcing a case fold would stop indexes being used. What every provider does
        // guarantee is that an exact-case term matches, so that is what the suite pins.
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.Equals, "Tools")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 3, 4, 8, 11);
    }

    [SkippableFact]
    public async Task Criteria_TextOperatorOnNullColumn_ShouldNotMatch()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.Contains, "o")),
            Paging = AllRows
        });

        result.Models.Should().NotContain(w => w.Id == 6 || w.Id == 7);
    }

    #endregion

    #region Criteria — nulls and unusable conditions

    [SkippableFact]
    public async Task Criteria_EqualsNull_ShouldMatchOnlyNullValues()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.Equals, null)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(6, 7);
    }

    [SkippableFact]
    public async Task Criteria_NotEqualsNull_ShouldMatchOnlyNonNullValues()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(new Condition("Category", ConditionOperator.NotEquals, null)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 2, 3, 4, 5, 8, 9, 10, 11, 12);
    }

    [SkippableTheory]
    [InlineData(ConditionOperator.GreaterThan)]
    [InlineData(ConditionOperator.LessThan)]
    [InlineData(ConditionOperator.Contains)]
    [InlineData(ConditionOperator.StartsWith)]
    public async Task Criteria_ConditionWithNoValue_ShouldBeIgnoredNotMatchNothing(ConditionOperator op)
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("Quantity", op)),
            Paging = AllRows
        });

        result.Meta.Total.Rows.Should().Be(12, "an unfilled filter is not a filter");
    }

    [SkippableFact]
    public async Task Criteria_BetweenWithoutAnUpperBound_ShouldBeIgnored()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("Quantity", ConditionOperator.Between, 5)),
            Paging = AllRows
        });

        result.Meta.Total.Rows.Should().Be(12);
    }

    [SkippableFact]
    public async Task Criteria_OnUnknownColumn_ShouldBeIgnored()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("NoSuchColumn", ConditionOperator.Equals, "x")),
            Paging = AllRows
        });

        result.Meta.Total.Rows.Should().Be(12);
    }

    [SkippableFact]
    public async Task Criteria_WithEmptyGroup_ShouldIgnoreItAndKeepTheRest()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Groups(
                Logic.And,
                new ConditionGroup([]),
                new ConditionGroup([new Condition("Category", ConditionOperator.Equals, "Promo")])),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(9, 12);
    }

    [SkippableFact]
    public async Task Criteria_WithNoGroupsAtAll_ShouldMatchEverything()
    {
        var result = await RunAsync(new Query
        {
            Criteria = new QueryCriteria(),
            Paging = AllRows
        });

        result.Meta.Total.Rows.Should().Be(12);
    }

    #endregion

    #region Criteria — logic

    [SkippableFact]
    public async Task Criteria_AndGroup_ShouldRequireEveryCondition()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(
                Logic.And,
                new Condition("Category", ConditionOperator.Equals, "Tools"),
                new Condition("Quantity", ConditionOperator.GreaterThan, 5)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 4, 8);
    }

    [SkippableFact]
    public async Task Criteria_OrGroup_ShouldRequireAnyCondition()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(
                Logic.Or,
                new Condition("Category", ConditionOperator.Equals, "Promo"),
                new Condition("Quantity", ConditionOperator.GreaterThan, 50)),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(2, 9, 12);
    }

    [SkippableFact]
    public async Task Criteria_AndNotGroup_ShouldNegateTheWholeGroup()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(Logic.AndNot, new Condition("Category", ConditionOperator.Equals, "Tools")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        // NOT (Category = 'Tools') is not true for a null category, matching SQL's three-valued logic.
        ids.Should().Equal(2, 5, 9, 10, 12);
    }

    [SkippableFact]
    public async Task Criteria_OrNotGroup_ShouldOrInsideThenNegate()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Group(
                Logic.OrNot,
                new Condition("Category", ConditionOperator.Equals, "Tools"),
                new Condition("Category", ConditionOperator.Equals, "Parts")),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(9, 12);
    }

    [SkippableFact]
    public async Task Criteria_MultipleGroups_JoinedWithAnd_ShouldIntersect()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Groups(
                Logic.And,
                new ConditionGroup([new Condition("Category", ConditionOperator.Equals, "Tools")]),
                new ConditionGroup([new Condition("Region", ConditionOperator.Equals, "North")])),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 4, 11);
    }

    [SkippableFact]
    public async Task Criteria_MultipleGroups_JoinedWithOr_ShouldUnion()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Groups(
                Logic.Or,
                new ConditionGroup([new Condition("Category", ConditionOperator.Equals, "Promo")]),
                new ConditionGroup([new Condition("Quantity", ConditionOperator.GreaterThanOrEqualTo, 50)])),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(2, 6, 9, 12);
    }

    [SkippableFact]
    public async Task Criteria_MixedGroupLogic_ShouldEvaluateEachGroupOnItsOwnTerms()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Groups(
                Logic.And,
                new ConditionGroup(
                    [
                        new Condition("Region", ConditionOperator.Equals, "North"),
                        new Condition("Region", ConditionOperator.Equals, "South")
                    ],
                    Logic.Or),
                new ConditionGroup([new Condition("IsActive", ConditionOperator.Equals, true)])),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 2, 4, 6, 8, 9, 11);
    }

    [SkippableFact]
    public async Task Criteria_ThreeGroups_ShouldAllApply()
    {
        var ids = await IdsAsync(new Query
        {
            Criteria = Groups(
                Logic.And,
                new ConditionGroup([new Condition("IsActive", ConditionOperator.Equals, true)]),
                new ConditionGroup([new Condition("Quantity", ConditionOperator.GreaterThan, 2)]),
                new ConditionGroup([new Condition("Region", ConditionOperator.Equals, "North")])),
            SortColumns = [new SortDescriptor("Id")],
            Paging = AllRows
        });

        ids.Should().Equal(1, 2, 4, 6, 9, 11);
    }

    #endregion

    #region GroupByColumns — single level

    [SkippableFact]
    public async Task Group_SingleLevel_ShouldReturnGroupedMetadata()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = AllRows
        });

        result.Meta.Type.Should().Be(QueryResultType.Grouped);
        result.Models.Should().BeEmpty("a grouped result carries Groups, not Models");
        result.Meta.Total.Rows.Should().Be(4, "totals count groups: null, Parts, Promo, Tools");
    }

    [SkippableFact]
    public async Task Group_SingleLevel_ShouldCarryKeysCountsAndItems()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = AllRows
        });

        var tools = result.Groups.Single(g => Equals(g.Key, "Tools"));

        tools.Count.Should().Be(5);
        tools.Items.Should().HaveCount(5);
        tools.SubGroups.Should().BeNull("a leaf level holds items, never sub-groups");
        tools.Items!.Select(w => w.Id).Should().BeEquivalentTo([1, 3, 4, 8, 11]);
    }

    [SkippableFact]
    public async Task Group_ShouldOrderKeysAscendingByDefault()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = AllRows
        });

        result.Groups.Select(g => g.Key).Should().Equal(new object?[] { null, "Parts", "Promo", "Tools" });
    }

    [SkippableFact]
    public async Task Group_Descending_ShouldReverseKeyOrder()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category", SortOrder.Descending)],
            Paging = AllRows
        });

        result.Groups.Select(g => g.Key).Should().Equal(new object?[] { "Tools", "Promo", "Parts", null });
    }

    [SkippableFact]
    public async Task Group_ShouldKeepNullAsItsOwnGroup()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = AllRows
        });

        var nullGroup = result.Groups.Single(g => g.Key is null);

        nullGroup.Count.Should().Be(2);
        nullGroup.Items!.Select(w => w.Id).Should().BeEquivalentTo([6, 7]);
    }

    [SkippableFact]
    public async Task Group_ShouldPageOverGroupsRatherThanRows()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = new QueryPaging(Size: 2, Number: 1)
        });

        result.Groups.Should().HaveCount(2);
        result.Groups.Select(g => g.Key).Should().Equal(new object?[] { null, "Parts" });
        result.Meta.Total.Rows.Should().Be(4);
        result.Meta.Total.Pages.Should().Be(2);
    }

    [SkippableFact]
    public async Task Group_SecondPageOfGroups_ShouldContinueWhereTheFirstStopped()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = new QueryPaging(Size: 2, Number: 2)
        });

        result.Groups.Select(g => g.Key).Should().Equal("Promo", "Tools");
    }

    [SkippableFact]
    public async Task Group_PagedGroup_ShouldStillCarryEveryOneOfItsRows()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category", SortOrder.Descending)],
            Paging = new QueryPaging(Size: 1, Number: 1)
        });

        // Page size bounds the number of groups, not the rows inside them.
        var tools = result.Groups.Single();

        tools.Key.Should().Be("Tools");
        tools.Count.Should().Be(5);
        tools.Items.Should().HaveCount(5);
    }

    [SkippableFact]
    public async Task Group_BeyondTheLastPage_ShouldReturnNoGroups()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = new QueryPaging(Size: 2, Number: 9)
        });

        result.Groups.Should().BeEmpty();
        result.Meta.Total.Rows.Should().Be(4);
    }

    #endregion

    #region GroupByColumns — multiple levels

    [SkippableFact]
    public async Task Group_TwoLevels_ShouldNestAndKeepCountsAtEachDepth()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category"), new GroupByDescriptor("Region")],
            Paging = AllRows
        });

        var tools = result.Groups.Single(g => Equals(g.Key, "Tools"));

        tools.Count.Should().Be(5, "a parent counts every leaf row beneath it");
        tools.Items.Should().BeNull("a parent level holds sub-groups, never items");
        tools.SubGroups.Should().HaveCount(2);

        var north = tools.SubGroups!.Single(g => Equals(g.Key, "North"));
        north.Count.Should().Be(3);
        north.Items!.Select(w => w.Id).Should().BeEquivalentTo([1, 4, 11]);
    }

    [SkippableFact]
    public async Task Group_TwoLevels_SubGroupCountsShouldSumToTheParentCount()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category"), new GroupByDescriptor("Region")],
            Paging = AllRows
        });

        foreach (var group in result.Groups)
            group.SubGroups!.Sum(s => s.Count).Should().Be(group.Count);
    }

    [SkippableFact]
    public async Task Group_TwoLevels_ShouldKeepNullAtTheInnerLevelToo()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category"), new GroupByDescriptor("Region")],
            Paging = AllRows
        });

        var parts = result.Groups.Single(g => Equals(g.Key, "Parts"));

        parts.SubGroups!.Select(g => g.Key).Should().Contain(new object?[] { null });
    }

    [SkippableFact]
    public async Task Group_ThreeLevels_ShouldNestAllTheWayDown()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns =
            [
                new GroupByDescriptor("Category"),
                new GroupByDescriptor("Region"),
                new GroupByDescriptor("IsActive")
            ],
            Paging = AllRows
        });

        var tools = result.Groups.Single(g => Equals(g.Key, "Tools"));
        var north = tools.SubGroups!.Single(g => Equals(g.Key, "North"));

        north.SubGroups.Should().NotBeNull();
        north.SubGroups!.Sum(s => s.Count).Should().Be(3);
        north.SubGroups!.SelectMany(s => s.Items!).Should().HaveCount(3);
    }

    [SkippableFact]
    public async Task Group_InnerLevelSortOrder_ShouldBeHonoured()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns =
            [
                new GroupByDescriptor("Category"),
                new GroupByDescriptor("Region", SortOrder.Descending)
            ],
            Paging = AllRows
        });

        var tools = result.Groups.Single(g => Equals(g.Key, "Tools"));

        tools.SubGroups!.Select(g => g.Key).Should().Equal("South", "North");
    }

    #endregion

    #region GroupByColumns — combined with other inputs

    [SkippableFact]
    public async Task Group_WithFilter_ShouldFilterBeforeGrouping()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("IsActive", ConditionOperator.Equals, true)),
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = AllRows
        });

        result.Meta.Total.Rows.Should().Be(4, "active widgets still span null, Parts, Promo and Tools");

        var tools = result.Groups.Single(g => Equals(g.Key, "Tools"));
        tools.Count.Should().Be(4, "Chisel is the only inactive Tools widget");
    }

    [SkippableFact]
    public async Task Group_WithSort_ShouldOrderItemsInsideEachGroup()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            SortColumns = [new SortDescriptor("Quantity", SortOrder.Descending)],
            Paging = AllRows
        });

        var tools = result.Groups.Single(g => Equals(g.Key, "Tools"));

        tools.Items!.Select(w => w.Quantity).Should().Equal(30, 9, 7, 3, 0);
    }

    [SkippableFact]
    public async Task Group_WithSelect_ShouldShapeItemsButKeepGroupingColumns()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category")],
            SelectColumns = ["Id", "Name"],
            Paging = AllRows
        });

        var tools = result.Groups.Single(g => Equals(g.Key, "Tools"));
        var item = tools.Items!.First();

        item.Id.Should().NotBe(0);
        item.Name.Should().NotBeEmpty();
        item.Quantity.Should().Be(0, "Quantity was not selected");
        item.Category.Should().Be("Tools", "the grouping column survives so the hierarchy stays rebuildable");
    }

    [SkippableFact]
    public async Task Group_WithFilterMatchingNothing_ShouldReturnAnEmptyHierarchy()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("Quantity", ConditionOperator.GreaterThan, 100_000)),
            GroupByColumns = [new GroupByDescriptor("Category")],
            Paging = AllRows
        });

        result.Groups.Should().BeEmpty();
        result.Meta.Total.Rows.Should().Be(0);
        result.Meta.Total.Pages.Should().Be(0);
    }

    [SkippableFact]
    public async Task Group_ByUnknownColumn_ShouldFallBackToAFlatResult()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("NoSuchColumn")],
            Paging = AllRows
        });

        result.Meta.Type.Should().Be(QueryResultType.Flat);
        result.Models.Should().HaveCount(12);
    }

    [SkippableFact]
    public async Task Group_ByBooleanColumn_ShouldProduceTwoGroups()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("IsActive")],
            Paging = AllRows
        });

        result.Groups.Should().HaveCount(2);
        result.Groups.Sum(g => g.Count).Should().Be(12);
    }

    [SkippableFact]
    public async Task Group_EveryRowShouldAppearExactlyOnce()
    {
        var result = await RunAsync(new Query
        {
            GroupByColumns = [new GroupByDescriptor("Category"), new GroupByDescriptor("Region")],
            Paging = AllRows
        });

        var ids = Flatten(result.Groups).Select(w => w.Id).ToList();

        ids.Should().HaveCount(12);
        ids.Should().OnlyHaveUniqueItems();
    }

    private static IEnumerable<Widget> Flatten(IReadOnlyList<HierarchyNode<Widget>> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Items is not null)
            {
                foreach (var item in node.Items)
                    yield return item;
            }

            if (node.SubGroups is null) continue;

            foreach (var item in Flatten(node.SubGroups))
                yield return item;
        }
    }

    #endregion

    #region Everything at once

    [SkippableFact]
    public async Task AllInputsTogether_Flat_ShouldApplyEachOne()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Groups(
                Logic.And,
                new ConditionGroup([new Condition("IsActive", ConditionOperator.Equals, true)]),
                new ConditionGroup(
                    [
                        new Condition("Category", ConditionOperator.Equals, "Tools"),
                        new Condition("Category", ConditionOperator.Equals, "Promo")
                    ],
                    Logic.Or)),
            SelectColumns = ["Id", "Name", "Quantity"],
            SortColumns = [new SortDescriptor("Quantity", SortOrder.Descending)],
            Paging = new QueryPaging(Size: 2, Number: 1)
        });

        result.Meta.Type.Should().Be(QueryResultType.Flat);
        result.Meta.Total.Rows.Should().Be(5, "active Tools or Promo widgets: 1, 4, 8, 9, 11");
        result.Meta.Total.Pages.Should().Be(3);

        result.Models.Select(w => w.Id).Should().Equal(8, 1);
        result.Models.Should().OnlyContain(w => w.Category == null, "Category was not selected");
    }

    [SkippableFact]
    public async Task AllInputsTogether_Grouped_ShouldApplyEachOne()
    {
        var result = await RunAsync(new Query
        {
            Criteria = Group(new Condition("Quantity", ConditionOperator.GreaterThanOrEqualTo, 3)),
            SelectColumns = ["Id", "Quantity"],
            SortColumns = [new SortDescriptor("Quantity", SortOrder.Descending)],
            GroupByColumns =
            [
                new GroupByDescriptor("Category", SortOrder.Descending),
                new GroupByDescriptor("Region")
            ],
            Paging = new QueryPaging(Size: 2, Number: 1)
        });

        result.Meta.Type.Should().Be(QueryResultType.Grouped);
        result.Meta.Total.Rows.Should().Be(4, "null, Parts, Promo and Tools all keep a qualifying row");
        result.Meta.Total.Pages.Should().Be(2);

        result.Groups.Select(g => g.Key).Should().Equal("Tools", "Promo");

        var tools = result.Groups.First();
        tools.Count.Should().Be(4, "Chisel has quantity 0 and is filtered out");
        tools.SubGroups.Should().NotBeNull();

        var north = tools.SubGroups!.Single(g => Equals(g.Key, "North"));
        north.Items!.Select(w => w.Quantity).Should().Equal(9, 7, 3);
        north.Items!.Should().OnlyContain(w => w.Name == string.Empty, "Name was not selected");
    }

    #endregion
}
