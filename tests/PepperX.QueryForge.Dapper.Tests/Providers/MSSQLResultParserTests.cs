using FluentAssertions;
using PepperX.QueryForge.Dapper.Providers.MSSQL;

namespace PepperX.QueryForge.Dapper.Tests.Providers.MSSQL;

public class MSSQLResultParserTests
{
    private class DummyUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Parse_FlatResult_ShouldReturnModels()
    {
        var raw = new MSSQLResultParser.RawQueryForgeResult
        {
            Meta = """{"Total":{"Rows":2,"Pages":1},"Type":"Flat"}""",
            Data = """[{"Id":1,"Name":"Alice"},{"Id":2,"Name":"Bob"}]"""
        };

        var result = MSSQLResultParser.Parse<DummyUser>(raw);

        result.Meta.Type.Should().Be(QueryResultType.Flat);
        result.Meta.Total.Rows.Should().Be(2);
        result.Models.Should().HaveCount(2);
        result.Models[0].Name.Should().Be("Alice");
    }

    [Fact]
    public void Parse_GroupedResult_ShouldReturnHierarchy()
    {
        var raw = new MSSQLResultParser.RawQueryForgeResult
        {
            Meta = """{"Total":{"Rows":1,"Pages":1},"Type":"Grouped"}""",
            Data = """
        [
          {
            "key": "USA",
            "count": 2,
            "items": [
              {
                "key": "NY",
                "count": 1,
                "items": [{"Id":1,"Name":"Alice"}]
              }
            ]
          }
        ]
        """
        };

        var result = MSSQLResultParser.Parse<DummyUser>(raw);

        result.Meta.Type.Should().Be(QueryResultType.Grouped);
        result.Groups.Should().HaveCount(1);

        var countryNode = result.Groups[0];
        countryNode.Key.Should().Be("USA");
        countryNode.Count.Should().Be(2);

        // The parser intelligently mapped the inner "items" array to C# SubGroups 
        // because it detected the "key" and "count" properties.
        var cityNode = countryNode.SubGroups![0];
        cityNode.Key.Should().Be("NY");
        cityNode.Items![0].Name.Should().Be("Alice");
    }

    [Fact]
    public void Parse_EmptyData_ShouldReturnEmptyModels()
    {
        var raw = new MSSQLResultParser.RawQueryForgeResult
        {
            Meta = """{"Total":{"Rows":0,"Pages":0},"Type":"Flat"}""",
            Data = ""
        };

        var result = MSSQLResultParser.Parse<DummyUser>(raw);

        result.Models.Should().BeEmpty();
    }
}