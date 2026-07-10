using FluentAssertions;
using PepperX.QueryForge;
using PepperX.QueryForge.Dapper;
using Xunit;

namespace PepperX.QueryForge.Dapper.Tests.Builders;

public class DapperQueryBuilderTests
{
    [Fact]
    public void New_ShouldCreateEmptyDapperQuery()
    {
        var query = DapperQueryBuilder.New().Build();

        query.Should().NotBeNull();
        query.Object.Should().BeNull();
        query.SelectColumns.Should().BeEmpty();
    }

    [Fact]
    public void FromBase_ShouldUpgradeBaseQueryAndPreserveProperties()
    {
        var baseQuery = QueryBuilder.New()
            .Select("Id", "Name")
            .Page(20, 2)
            .Build();

        var dapperQuery = DapperQueryBuilder
            .FromBase(baseQuery)
            .ForObject("Users", "dbo", DapperObjectType.Table)
            .Build();

        dapperQuery.Object.Should().NotBeNull();
        dapperQuery.Object!.Name.Should().Be("Users");
        dapperQuery.SelectColumns.Should().BeEquivalentTo("Id", "Name");
        dapperQuery.Paging.Size.Should().Be(20);
    }

    [Fact]
    public void ForObject_ShouldSetAllObjectProperties()
    {
        var parameters = new Dictionary<string, object?> { { "TenantId", 1 } };
        
        var query = DapperQueryBuilder.New()
            .ForObject("MyTable", "app", DapperObjectType.View, parameters)
            .Build();

        query.Object!.Name.Should().Be("MyTable");
        query.Object.Schema.Should().Be("app");
        query.Object.Type.Should().Be(DapperObjectType.View);
        query.Object.Parameters.Should().ContainKey("TenantId");
    }

    [Fact]
    public void FluentChaining_ShouldReturnDapperQueryFluent()
    {
        var fluent = DapperQueryBuilder.New()
            .Select("Id")
            .Where(new QueryCriteria())
            .Sort(new SortDescriptor("Id"))
            .GroupBy(new GroupByDescriptor("Category"))
            .Page(10);

        fluent.Should().BeOfType<DapperQueryFluent>();
    }
}