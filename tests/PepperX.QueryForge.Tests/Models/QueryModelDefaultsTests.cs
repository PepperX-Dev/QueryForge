using FluentAssertions;

namespace PepperX.QueryForge.Tests.Models;

public class QueryModelDefaultsTests
{
    [Fact]
    public void Query_Defaults_ShouldNeverBeNull()
    {
        // Act
        var query = new Query();

        // Assert
        query.Criteria.Should().NotBeNull();
        query.Criteria.Groups.Should().NotBeNull().And.BeEmpty();

        query.Paging.Should().NotBeNull();
        query.Paging.Size.Should().Be(12);
        query.Paging.Number.Should().Be(1);

        query.SelectColumns.Should().NotBeNull().And.BeEmpty();
        query.SortColumns.Should().NotBeNull().And.BeEmpty();
        query.GroupByColumns.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ConditionGroup_Defaults_ShouldNeverBeNull()
    {
        // Act
        var group = new ConditionGroup();

        // Assert
        group.Conditions.Should().NotBeNull().And.BeEmpty();
        group.Logic.Should().Be(Logic.And);
    }

    [Fact]
    public void ConditionGroup_WithNullConditions_ShouldDefaultToEmptyArray()
    {
        // Act
        var group = new ConditionGroup(conditions: null);

        // Assert
        group.Conditions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void QueryCriteria_Defaults_ShouldNeverBeNull()
    {
        // Act
        var criteria = new QueryCriteria();

        // Assert
        criteria.Groups.Should().NotBeNull().And.BeEmpty();
        criteria.Logic.Should().Be(Logic.And);
    }

    [Fact]
    public void QueryCriteria_WithNullGroups_ShouldDefaultToEmptyArray()
    {
        // Act
        var criteria = new QueryCriteria(groups: null);

        // Assert
        criteria.Groups.Should().NotBeNull().And.BeEmpty();
    }
}