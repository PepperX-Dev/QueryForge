using System.Text.Json;
using FluentAssertions;
using PepperX.QueryForge.Dapper;
using PepperX.QueryForge.Dapper.Providers.MSSQL;
using Xunit;

namespace PepperX.QueryForge.Dapper.Tests.Providers.MSSQL;

public class MSSQLParameterMapperTests
{
    [Fact]
    public void Map_WithNullObject_ShouldThrow()
    {
        var query = new DapperQuery { Object = null };

        var act = () => MSSQLParameterMapper.Map(query);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Object must be specified*");
    }

    [Fact]
    public void Map_WithValidQuery_ShouldReturnAnonymousObjectWithExpectedKeys()
    {
        var query = DapperQueryBuilder.New()
            .ForObject("Users")
            .Select("Id")
            .Build();

        var result = MSSQLParameterMapper.Map(query);

        // Serialize the anonymous object to verify its shape
        var json = JsonSerializer.Serialize(result);

        json.Should().Contain("\"Object\":");
        json.Should().Contain("\"Criteria\":");
        json.Should().Contain("\"Paging\":");
        json.Should().Contain("\"SelectColumns\":");
    }

    [Fact]
    public void Map_ShouldSerializeEnumsAsStrings()
    {
        var query = DapperQueryBuilder.New()
            .ForObject("Users")
            .Where(new QueryCriteria(logic: Logic.Or))
            .Build();

        var result = MSSQLParameterMapper.Map(query);
        var json = JsonSerializer.Serialize(result);

        // Because the inner JSON is serialized as a string, the quotes are escaped.
        // We just verify the enum value "Or" and the property "Logic" made it through.
        json.Should().Contain("Logic");
        json.Should().Contain("Or");
    }
}