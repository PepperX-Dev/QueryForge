using FluentAssertions;

namespace PepperX.QueryForge.Tests.Builders;

public class QueryBuilderTests
{
    [Fact]
    public void New_ShouldCreateEmptyQueryWithDefaults()
    {
        // Act
        var query = QueryBuilder.New().Build();

        // Assert
        query.Should().NotBeNull();
        query.SelectColumns.Should().BeEmpty();
        query.Criteria.Groups.Should().BeEmpty();
        query.Paging.Size.Should().Be(12); // Default page size
    }

    [Fact]
    public void FluentChaining_ShouldPopulateAllPropertiesCorrectly()
    {
        // Arrange
        var criteria = new QueryCriteria(
            groups: [new ConditionGroup([new Condition("Id", ConditionOperator.Equals, 1)])]
        );

        // Act
        var query = QueryBuilder
            .Select("Id", "Name", "Email")
            .Where(criteria)
            .Sort(new SortDescriptor("Name", SortOrder.Descending))
            .GroupBy(new GroupByDescriptor("Category"))
            .Page(25, 3)
            .Build();

        // Assert
        query.SelectColumns.Should().BeEquivalentTo("Id", "Name", "Email");

        query.Criteria.Logic.Should().Be(Logic.And);
        query.Criteria.Groups.Should().HaveCount(1);
        query.Criteria.Groups[0].Conditions[0].ColumnName.Should().Be("Id");

        query.SortColumns.Should().HaveCount(1);
        query.SortColumns[0].ColumnName.Should().Be("Name");
        query.SortColumns[0].SortOrder.Should().Be(SortOrder.Descending);

        query.GroupByColumns.Should().HaveCount(1);
        query.GroupByColumns[0].ColumnName.Should().Be("Category");

        query.Paging.Size.Should().Be(25);
        query.Paging.Number.Should().Be(3);
    }

    [Fact]
    public void Build_WithoutSelect_ShouldLeaveSelectColumnsEmpty()
    {
        // Act
        var query = QueryBuilder.Page(10).Build();

        // Assert
        query.SelectColumns.Should().BeEmpty();
    }
}