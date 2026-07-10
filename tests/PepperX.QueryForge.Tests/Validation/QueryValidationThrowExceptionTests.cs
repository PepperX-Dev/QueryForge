using FluentAssertions;

namespace PepperX.QueryForge.Tests.Validation;

public class QueryValidationThrowExceptionTests
{
    [Fact]
    public void Validate_SelectColumns_Deny_ShouldThrowWithPropertyName()
    {
        // Arrange
        var query = new Query { SelectColumns = ["Id", "Password"] };

        // Act
        var act = () => query.Validate(rules => rules.Select(c => c.Deny("Password")), QueryValidationMode.ThrowException);

        // Assert
        act.Should().Throw<QueryValidationException>()
           .Which.InvalidProperties.Should().Contain("Password");
    }

    [Fact]
    public void Validate_Paging_Max_ShouldThrow()
    {
        // Arrange
        var query = new Query { Paging = new QueryPaging(Size: 100) };

        // Act
        var act = () => query.Validate(rules => rules.PageSize(p => p.Max(50)), QueryValidationMode.ThrowException);

        // Assert
        act.Should().Throw<QueryValidationException>();
    }

    [Fact]
    public void Validate_Criteria_ShouldThrowWithColumnName()
    {
        // Arrange
        var query = new Query
        {
            Criteria = new QueryCriteria(groups:
            [
                new ConditionGroup(conditions: [new Condition("SecretColumn", ConditionOperator.Equals, 1)])
            ])
        };

        // Act
        var act = () => query.Validate(rules => rules.Where(c => c.Deny("SecretColumn")), QueryValidationMode.ThrowException);

        // Assert
        act.Should().Throw<QueryValidationException>()
           .Which.InvalidProperties.Should().Contain("SecretColumn");
    }

    [Fact]
    public void Validate_MultipleErrors_ShouldCollectAllInException()
    {
        // Arrange
        var query = new Query
        {
            SelectColumns = ["BadCol1", "BadCol2"],
            SortColumns = [new SortDescriptor("BadCol3")]
        };

        // Act
        var act = () => query.Validate(rules =>
        {
            rules.Select(c => c.Deny("BadCol1", "BadCol2"));
            rules.Sort(c => c.Deny("BadCol3"));
        }, QueryValidationMode.ThrowException);

        // Assert
        var exception = act.Should().Throw<QueryValidationException>().Which;
        exception.InvalidProperties.Should().HaveCount(3);
        exception.InvalidProperties.Should().Contain("BadCol1");
        exception.InvalidProperties.Should().Contain("BadCol2");
        exception.InvalidProperties.Should().Contain("BadCol3");
    }
}