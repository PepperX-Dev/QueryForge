using FluentAssertions;

namespace PepperX.QueryForge.Tests.Validation;

public class QueryValidationSilentStripTests
{
    [Fact]
    public void Validate_SelectColumns_Deny_ShouldStripInPlace()
    {
        // Arrange
        var query = new Query { SelectColumns = ["Id", "Name", "PasswordHash", "SecretKey"] };

        // Act
        query.Validate(rules => rules.Select(c => c.Deny("PasswordHash", "SecretKey")), QueryValidationMode.SilentStrip);

        // Assert
        query.SelectColumns.Should().BeEquivalentTo("Id", "Name");
    }

    [Fact]
    public void Validate_SelectColumns_Allow_ShouldKeepOnlyAllowed()
    {
        // Arrange
        var query = new Query { SelectColumns = ["Id", "Name", "Secret"] };

        // Act
        query.Validate(rules => rules.Select(c => c.Allow("Id", "Name")), QueryValidationMode.SilentStrip);

        // Assert
        query.SelectColumns.Should().BeEquivalentTo("Id", "Name");
    }

    [Fact]
    public void Validate_Paging_Max_ShouldClampSizeDown()
    {
        // Arrange
        var query = new Query { Paging = new QueryPaging(Size: 500) };

        // Act
        query.Validate(rules => rules.PageSize(p => p.Max(100)), QueryValidationMode.SilentStrip);

        // Assert
        query.Paging.Size.Should().Be(100);
    }

    [Fact]
    public void Validate_Paging_Min_ShouldClampSizeUp()
    {
        // Arrange
        var query = new Query { Paging = new QueryPaging(Size: 2) };

        // Act
        query.Validate(rules => rules.PageSize(p => p.Min(10)), QueryValidationMode.SilentStrip);

        // Assert
        query.Paging.Size.Should().Be(10);
    }

    [Fact]
    public void Validate_Criteria_ShouldStripDeniedConditionColumns()
    {
        // Arrange
        var query = new Query
        {
            Criteria = new QueryCriteria(groups:
            [
                new ConditionGroup(conditions:
                [
                    new Condition("Id", ConditionOperator.Equals, 1),
                    new Condition("InternalToken", ConditionOperator.Equals, "abc")
                ])
            ])
        };

        // Act
        query.Validate(rules => rules.Where(c => c.Deny("InternalToken")), QueryValidationMode.SilentStrip);

        // Assert
        query.Criteria.Groups[0].Conditions.Should().HaveCount(1);
        query.Criteria.Groups[0].Conditions[0].ColumnName.Should().Be("Id");
    }

    [Fact]
    public void Validate_SortColumns_Deny_ShouldStrip()
    {
        // Arrange
        var query = new Query { SortColumns = [new SortDescriptor("Id"), new SortDescriptor("InternalNotes")] };

        // Act
        query.Validate(rules => rules.Sort(c => c.Deny("InternalNotes")), QueryValidationMode.SilentStrip);

        // Assert
        query.SortColumns.Should().HaveCount(1);
        query.SortColumns[0].ColumnName.Should().Be("Id");
    }

    [Fact]
    public void Validate_ShouldBeCaseInsensitive()
    {
        // Arrange
        var query = new Query { SelectColumns = ["ID", "name"] };

        // Act
        query.Validate(rules => rules.Select(c => c.Deny("id", "NAME")), QueryValidationMode.SilentStrip);

        // Assert
        query.SelectColumns.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyQuery_ShouldNotThrowAndRemainEmpty()
    {
        // Arrange
        var query = new Query();

        // Act
        var act = () => query.Validate(rules =>
        {
            rules.Select(c => c.Allow("Id"));
            rules.PageSize(p => p.Max(50));
        }, QueryValidationMode.SilentStrip);

        // Assert
        act.Should().NotThrow();
        query.SelectColumns.Should().BeEmpty(); // Allowlist doesn't force columns to be added
    }
}