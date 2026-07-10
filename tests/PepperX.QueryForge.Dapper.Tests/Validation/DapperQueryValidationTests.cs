using FluentAssertions;
using PepperX.QueryForge;
using PepperX.QueryForge.Dapper;
using Xunit;

namespace PepperX.QueryForge.Dapper.Tests.Validation;

public class DapperQueryValidationTests
{
    [Fact]
    public void Validate_Object_RequireName_ShouldThrowIfMissing()
    {
        var query = new DapperQuery { Object = null };

        var act = () => query.Validate(rules => rules.Object(o => o.RequireName()), QueryValidationMode.ThrowException);

        act.Should().Throw<QueryValidationException>()
           .Which.InvalidProperties.Should().Contain("Object.Name is required.");
    }

    [Fact]
    public void Validate_Object_AllowSchema_ShouldFallbackToDefaultInSilentStrip()
    {
        var query = new DapperQuery 
        { 
            Object = new DapperQueryObject("Users", "secret_schema") 
        };

        query.Validate(rules => rules.Object(o => o.AllowSchema("dbo", "app")), QueryValidationMode.SilentStrip);

        query.Object!.Schema.Should().Be("dbo");
    }

    [Fact]
    public void Validate_Object_DenyTable_ShouldThrowIfTableDenied()
    {
        var query = new DapperQuery 
        { 
            Object = new DapperQueryObject("SecretTable") 
        };

        var act = () => query.Validate(rules => rules.Object(o => o.DenyTable("SecretTable")), QueryValidationMode.ThrowException);

        act.Should().Throw<QueryValidationException>()
           .Which.InvalidProperties.Should().Contain("Object.Name 'SecretTable' is not allowed.");
    }

    [Fact]
    public void Validate_ShouldCombineBaseAndDapperRules_ThrowException()
    {
        var query = new DapperQuery
        {
            Object = new DapperQueryObject("Users", "bad_schema"),
            SelectColumns = ["Password"]
        };

        var act = () => query.Validate(rules =>
        {
            rules.Object(o => o.AllowSchema("dbo")); 
            rules.Select(c => c.Deny("Password"));   
        }, QueryValidationMode.ThrowException);

        var ex = act.Should().Throw<QueryValidationException>().Which;
        ex.InvalidProperties.Should().HaveCount(2); 
    }
}