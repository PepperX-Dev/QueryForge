using FluentAssertions;
using PepperX.QueryForge.Querying;

namespace PepperX.QueryForge.Tests.Querying;

public class InMemoryQueryEngineTests
{
    private sealed record User(int Id, string Name, string? Country, int Age);

    private static readonly User[] Users =
    [
        new(1, "Amir", "Iran", 30),
        new(2, "Bita", "Iran", 25),
        new(3, "Carl", "Canada", 40),
        new(4, "Dana", null, 35),
        new(5, "Emma", "Canada", 22)
    ];

    private static QueryCriteria Criteria(Logic logic, params Condition[] conditions)
        => new([new ConditionGroup(conditions, logic)]);

    [Fact]
    public void Apply_WithoutCriteria_ShouldReturnEverythingPaged()
    {
        var result = InMemoryQueryEngine.Apply(Users, new Query());

        result.Meta.Type.Should().Be(QueryResultType.Flat);
        result.Meta.Total.Rows.Should().Be(5);
        result.Models.Should().HaveCount(5);
    }

    [Fact]
    public void Apply_ShouldPageAndReportTotals()
    {
        var query = new Query { Paging = new QueryPaging(Size: 2, Number: 2) };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().HaveCount(2);
        result.Meta.Total.Rows.Should().Be(5);
        result.Meta.Total.Pages.Should().Be(3);
    }

    [Theory]
    [InlineData(ConditionOperator.Equals, "Iran", 2)]
    [InlineData(ConditionOperator.NotEquals, "Iran", 2)]
    [InlineData(ConditionOperator.Contains, "ana", 2)]
    [InlineData(ConditionOperator.StartsWith, "Can", 2)]
    [InlineData(ConditionOperator.EndsWith, "ada", 2)]
    public void Apply_TextOperators_ShouldFilter(ConditionOperator op, string value, int expected)
    {
        var query = new Query { Criteria = Criteria(Logic.And, new Condition("Country", op, value)) };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().HaveCount(expected);
    }

    [Theory]
    [InlineData(ConditionOperator.LessThan, 30, 2)]
    [InlineData(ConditionOperator.GreaterThan, 30, 2)]
    [InlineData(ConditionOperator.LessThanOrEqualTo, 30, 3)]
    [InlineData(ConditionOperator.GreaterThanOrEqualTo, 30, 3)]
    public void Apply_NumericOperators_ShouldCompareNumericallyNotLexically(
        ConditionOperator op, int value, int expected)
    {
        var query = new Query { Criteria = Criteria(Logic.And, new Condition("Age", op, value)) };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().HaveCount(expected);
    }

    [Fact]
    public void Apply_NumericOperator_WithStringValue_ShouldStillCompareNumerically()
    {
        // A value arriving from JSON may be a string; "9" must not sort above "30".
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, "9"))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().HaveCount(5);
    }

    [Fact]
    public void Apply_Between_ShouldBeInclusive()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.Between, 25, 35))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Select(u => u.Id).Should().BeEquivalentTo([1, 2, 4]);
    }

    [Fact]
    public void Apply_EqualsNull_ShouldMatchNullColumn()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Country", ConditionOperator.Equals, null))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().ContainSingle().Which.Id.Should().Be(4);
    }

    [Fact]
    public void Apply_NotEqualsNull_ShouldMatchNonNullColumns()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Country", ConditionOperator.NotEquals, null))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().HaveCount(4);
    }

    [Fact]
    public void Apply_ConditionMissingValue_ShouldBeSkippedNotTreatedAsMatchNothing()
    {
        // GreaterThan with no value is an unfilled filter, not "greater than null".
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, null))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().HaveCount(5);
    }

    [Fact]
    public void Apply_BetweenWithoutUpperBound_ShouldBeSkipped()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.Between, 25))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().HaveCount(5);
    }

    [Fact]
    public void Apply_OrGroup_ShouldUnionConditions()
    {
        var query = new Query
        {
            Criteria = Criteria(
                Logic.Or,
                new Condition("Country", ConditionOperator.Equals, "Canada"),
                new Condition("Age", ConditionOperator.Equals, 25))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Select(u => u.Id).Should().BeEquivalentTo([2, 3, 5]);
    }

    [Fact]
    public void Apply_AndNotGroup_ShouldNegateTheWholeGroup()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.AndNot, new Condition("Country", ConditionOperator.Equals, "Iran"))
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        // Dana's country is null, so "Country = 'Iran'" is unknown rather than false and stays
        // unknown under negation — the same row SQL would leave out.
        result.Models.Select(u => u.Id).Should().BeEquivalentTo([3, 5]);
    }

    [Fact]
    public void Apply_MultipleGroups_ShouldJoinUsingCriteriaLogic()
    {
        var query = new Query
        {
            Criteria = new QueryCriteria(
                [
                    new ConditionGroup([new Condition("Country", ConditionOperator.Equals, "Iran")]),
                    new ConditionGroup([new Condition("Age", ConditionOperator.GreaterThan, 28)])
                ],
                Logic.And)
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void Apply_MultipleGroupsWithOrCriteria_ShouldUnionGroups()
    {
        var query = new Query
        {
            Criteria = new QueryCriteria(
                [
                    new ConditionGroup([new Condition("Country", ConditionOperator.Equals, "Iran")]),
                    new ConditionGroup([new Condition("Age", ConditionOperator.GreaterThan, 38)])
                ],
                Logic.Or)
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Models.Select(u => u.Id).Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Apply_Sorting_ShouldRespectMultipleColumnsAndDirections()
    {
        var query = new Query
        {
            SortColumns =
            [
                new SortDescriptor("Country", SortOrder.Ascending),
                new SortDescriptor("Age", SortOrder.Descending)
            ]
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        // Null country sorts first, then Canada (40, 22), then Iran (30, 25).
        result.Models.Select(u => u.Id).Should().ContainInOrder(4, 3, 5, 1, 2);
    }

    [Fact]
    public void Apply_Grouped_ShouldReportGroupTotalsNotRowTotals()
    {
        var query = new Query { GroupByColumns = [new GroupByDescriptor("Country")] };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Meta.Type.Should().Be(QueryResultType.Grouped);
        // Three distinct countries: null, Canada, Iran.
        result.Meta.Total.Rows.Should().Be(3);
        result.Groups.Should().HaveCount(3);
    }

    [Fact]
    public void Apply_Grouped_ShouldPageOverGroupsNotRows()
    {
        var query = new Query
        {
            GroupByColumns = [new GroupByDescriptor("Country")],
            Paging = new QueryPaging(Size: 1, Number: 2)
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        result.Groups.Should().ContainSingle();
        result.Groups[0].Key.Should().Be("Canada");
        result.Groups[0].Count.Should().Be(2);
        result.Meta.Total.Pages.Should().Be(3);
    }

    [Fact]
    public void Apply_Grouped_ShouldApplyFilterBeforeGrouping()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, 24)),
            GroupByColumns = [new GroupByDescriptor("Country")]
        };

        var result = InMemoryQueryEngine.Apply(Users, query);

        // Emma (22) is filtered out, so Canada holds a single row.
        var canada = result.Groups.Single(g => Equals(g.Key, "Canada"));
        canada.Count.Should().Be(1);
    }
}
