using FluentAssertions;
using PepperX.QueryForge.Dapper.Compiler;
using PepperX.QueryForge.Dapper.Dialects;

namespace PepperX.QueryForge.Dapper.Tests.Compiler;

/// <summary>
/// Asserts the SQL text and parameters the compiler emits. These are the tests the stored-procedure
/// engine could never have, because its logic lived in T-SQL rather than in C#.
/// </summary>
public class SqlQueryCompilerTests
{
    private static readonly ColumnWhitelist Columns = new(["Id", "Name", "Country", "Age", "City"]);

    private static SqlQueryCompiler ForSqlServer() => new(new SqlServerDialect());

    private static DapperQuery Query(Action<DapperQueryFluent>? configure = null)
    {
        var builder = DapperQueryBuilder.New().ForObject("Users", "dbo");
        configure?.Invoke(builder);

        return builder.Build();
    }

    private static QueryCriteria Criteria(Logic logic, params Condition[] conditions)
        => new([new ConditionGroup(conditions, logic)]);

    #region Structure

    [Fact]
    public void CompileRows_WithoutFilters_ShouldSelectEverythingPaged()
    {
        var sql = ForSqlServer().CompileRows(Query(), Columns);

        sql.Text.Should().Be(
            "SELECT * FROM [dbo].[Users] ORDER BY (SELECT NULL) OFFSET 0 ROWS FETCH NEXT 12 ROWS ONLY");
        sql.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void CompileRows_ShouldTranslatePagingToOffsetAndFetch()
    {
        var query = Query(b => b.Page(size: 25, number: 3));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().EndWith("OFFSET 50 ROWS FETCH NEXT 25 ROWS ONLY");
    }

    [Fact]
    public void CompileRows_ShouldProjectOnlyRequestedColumns()
    {
        var query = Query(b => b.Select("Id", "Name"));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().StartWith("SELECT [Id], [Name] FROM");
    }

    [Fact]
    public void CompileRows_ShouldOrderByEachSortColumnAndDirection()
    {
        var query = Query(b => b.Sort(
            new SortDescriptor("Country"),
            new SortDescriptor("Age", SortOrder.Descending)));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("ORDER BY [Country] ASC, [Age] DESC");
    }

    [Fact]
    public void CompileRowCount_ShouldCountWithTheSameFilterButNoPaging()
    {
        var query = Query(b => b
            .Where(Criteria(Logic.And, new Condition("Country", ConditionOperator.Equals, "Iran")))
            .Page(10, 5));

        var sql = ForSqlServer().CompileRowCount(query, Columns);

        sql.Text.Should().Be("SELECT COUNT(*) FROM [dbo].[Users] WHERE ([Country] = @p0)");
        sql.Text.Should().NotContain("OFFSET");
    }

    #endregion

    #region Parameterization

    [Fact]
    public void CompileRows_ShouldNeverInlineAValue()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Name", ConditionOperator.Equals, "O'Brien; DROP TABLE Users--"))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().NotContain("O'Brien");
        sql.Text.Should().NotContain("DROP TABLE");
        sql.Parameters.Values.Should().Contain("O'Brien; DROP TABLE Users--");
    }

    [Fact]
    public void CompileRows_NumericValue_ShouldStayNumericSoComparisonIsNotLexical()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Age", ConditionOperator.GreaterThan, 30))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("[Age] > @p0");
        sql.Parameters["p0"].Should().Be(30).And.BeOfType<int>();
    }

    [Fact]
    public void CompileRows_ShouldNumberParametersSequentially()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Country", ConditionOperator.Equals, "Iran"),
            new Condition("Age", ConditionOperator.GreaterThan, 18))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Parameters.Should().HaveCount(2);
        sql.Text.Should().Contain("@p0").And.Contain("@p1");
    }

    #endregion

    #region Operators

    [Theory]
    [InlineData(ConditionOperator.Equals, "[Age] = @p0")]
    [InlineData(ConditionOperator.NotEquals, "[Age] <> @p0")]
    [InlineData(ConditionOperator.LessThan, "[Age] < @p0")]
    [InlineData(ConditionOperator.GreaterThan, "[Age] > @p0")]
    [InlineData(ConditionOperator.LessThanOrEqualTo, "[Age] <= @p0")]
    [InlineData(ConditionOperator.GreaterThanOrEqualTo, "[Age] >= @p0")]
    public void CompileRows_ComparisonOperators_ShouldEmitTheMatchingSql(ConditionOperator op, string expected)
    {
        var query = Query(b => b.Where(Criteria(Logic.And, new Condition("Age", op, 30))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain(expected);
    }

    [Fact]
    public void CompileRows_Between_ShouldEmitTwoParameters()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Age", ConditionOperator.Between, 18, 65))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("[Age] BETWEEN @p0 AND @p1");
        sql.Parameters["p0"].Should().Be(18);
        sql.Parameters["p1"].Should().Be(65);
    }

    [Theory]
    [InlineData(ConditionOperator.Contains, "LIKE", "%ir%")]
    [InlineData(ConditionOperator.NotContains, "NOT LIKE", "%ir%")]
    [InlineData(ConditionOperator.StartsWith, "LIKE", "ir%")]
    [InlineData(ConditionOperator.EndsWith, "LIKE", "%ir")]
    public void CompileRows_PatternOperators_ShouldWrapTheValueInTheRightWildcards(
        ConditionOperator op, string expectedOperator, string expectedPattern)
    {
        var query = Query(b => b.Where(Criteria(Logic.And, new Condition("Country", op, "ir"))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain($"[Country] {expectedOperator} @p0 ESCAPE");
        sql.Parameters["p0"].Should().Be(expectedPattern);
    }

    [Fact]
    public void CompileRows_PatternOperator_ShouldEscapeWildcardsInsideTheValue()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Name", ConditionOperator.Contains, "100%_x"))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        // The caller's own % and _ must match literally, not act as wildcards.
        sql.Parameters["p0"].Should().Be(@"%100\%\_x%");
    }

    [Fact]
    public void CompileRows_EqualsNull_ShouldBecomeIsNullWithNoParameter()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Country", ConditionOperator.Equals, null))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("[Country] IS NULL");
        sql.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void CompileRows_NotEqualsNull_ShouldBecomeIsNotNull()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Country", ConditionOperator.NotEquals, null))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("[Country] IS NOT NULL");
    }

    [Fact]
    public void CompileRows_ConditionWithMissingValue_ShouldBeDroppedNotEmitted()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Age", ConditionOperator.GreaterThan, null))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().NotContain("WHERE");
    }

    #endregion

    #region Logic

    [Fact]
    public void CompileRows_AndGroup_ShouldJoinConditionsWithAnd()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Country", ConditionOperator.Equals, "Iran"),
            new Condition("Age", ConditionOperator.GreaterThan, 18))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("WHERE ([Country] = @p0 AND [Age] > @p1)");
    }

    [Fact]
    public void CompileRows_OrGroup_ShouldJoinConditionsWithOr()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.Or,
            new Condition("Country", ConditionOperator.Equals, "Iran"),
            new Condition("Age", ConditionOperator.GreaterThan, 18))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("WHERE ([Country] = @p0 OR [Age] > @p1)");
    }

    [Fact]
    public void CompileRows_AndNotGroup_ShouldNegateTheWholeGroup()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.AndNot,
            new Condition("Country", ConditionOperator.Equals, "Iran"))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("WHERE NOT ([Country] = @p0)");
    }

    [Fact]
    public void CompileRows_OrNotGroup_ShouldUseOrInsideAndNegateOutside()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.OrNot,
            new Condition("Country", ConditionOperator.Equals, "Iran"),
            new Condition("Age", ConditionOperator.Equals, 18))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("WHERE NOT ([Country] = @p0 OR [Age] = @p1)");
    }

    [Fact]
    public void CompileRows_MultipleGroups_ShouldJoinUsingCriteriaLogic()
    {
        var criteria = new QueryCriteria(
            [
                new ConditionGroup([new Condition("Country", ConditionOperator.Equals, "Iran")]),
                new ConditionGroup([new Condition("Age", ConditionOperator.GreaterThan, 18)])
            ],
            Logic.Or);

        var sql = ForSqlServer().CompileRows(Query(b => b.Where(criteria)), Columns);

        sql.Text.Should().Contain("WHERE ([Country] = @p0) OR ([Age] > @p1)");
    }

    [Fact]
    public void CompileRows_EmptyGroup_ShouldBeSkippedEntirely()
    {
        var criteria = new QueryCriteria(
            [
                new ConditionGroup([]),
                new ConditionGroup([new Condition("Age", ConditionOperator.GreaterThan, 18)])
            ],
            Logic.And);

        var sql = ForSqlServer().CompileRows(Query(b => b.Where(criteria)), Columns);

        sql.Text.Should().Contain("WHERE ([Age] > @p0)");
        sql.Text.Should().NotContain("()");
    }

    #endregion

    #region Column whitelist

    [Fact]
    public void CompileRows_UnknownFilterColumn_ShouldNeverReachTheSql()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("PasswordHash", ConditionOperator.Equals, "x"))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().NotContain("PasswordHash");
        sql.Text.Should().NotContain("WHERE");
        sql.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void CompileRows_UnknownSortColumn_ShouldBeDropped()
    {
        var query = Query(b => b.Sort(new SortDescriptor("PasswordHash")));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().NotContain("PasswordHash");
    }

    [Fact]
    public void CompileRows_UnknownSelectColumn_ShouldBeDropped()
    {
        var query = Query(b => b.Select("Id", "PasswordHash"));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().StartWith("SELECT [Id] FROM");
    }

    [Fact]
    public void CompileRows_WhenEveryRequestedColumnIsUnknown_ShouldFallBackToStar()
    {
        var query = Query(b => b.Select("PasswordHash", "Salt"));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().StartWith("SELECT * FROM");
    }

    [Fact]
    public void CompileRows_ColumnNameContainingDelimiter_ShouldBeRejectedByTheWhitelist()
    {
        var query = Query(b => b.Where(Criteria(
            Logic.And,
            new Condition("Name] ; DROP TABLE Users --", ConditionOperator.Equals, "x"))));

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().NotContain("DROP TABLE");
    }

    #endregion

    #region Grouping

    [Fact]
    public void CompileGroupKeys_ShouldPageOverDistinctKeysOfTheOutermostLevel()
    {
        var query = Query(b => b
            .GroupBy(new GroupByDescriptor("Country"), new GroupByDescriptor("City"))
            .Page(size: 5, number: 2));

        var sql = ForSqlServer().CompileGroupKeys(query, Columns);

        sql.Text.Should().Be(
            "SELECT DISTINCT [Country] FROM [dbo].[Users] ORDER BY [Country] ASC OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY");
    }

    [Fact]
    public void CompileGroupKeys_ShouldHonourTheGroupSortDirection()
    {
        var query = Query(b => b.GroupBy(new GroupByDescriptor("Country", SortOrder.Descending)));

        var sql = ForSqlServer().CompileGroupKeys(query, Columns);

        sql.Text.Should().Contain("ORDER BY [Country] DESC");
    }

    [Fact]
    public void CompileGroupCount_ShouldCountGroupsNotRows()
    {
        var query = Query(b => b.GroupBy(new GroupByDescriptor("Country")));

        var sql = ForSqlServer().CompileGroupCount(query, Columns);

        sql.Text.Should().Be(
            "SELECT COUNT(*) FROM (SELECT DISTINCT [Country] FROM [dbo].[Users]) qf_groups");
    }

    [Fact]
    public void CompileGroupRows_ShouldRestrictToThePagedKeys()
    {
        var query = Query(b => b.GroupBy(new GroupByDescriptor("Country")));

        var sql = ForSqlServer().CompileGroupRows(query, Columns, ["Iran", "Canada"]);

        sql.Text.Should().Contain("WHERE [Country] IN (@p0, @p1)");
        sql.Parameters["p0"].Should().Be("Iran");
        sql.Parameters["p1"].Should().Be("Canada");
    }

    [Fact]
    public void CompileGroupRows_WithANullKey_ShouldIncludeIsNull()
    {
        var query = Query(b => b.GroupBy(new GroupByDescriptor("Country")));

        var sql = ForSqlServer().CompileGroupRows(query, Columns, ["Iran", null]);

        sql.Text.Should().Contain("([Country] IN (@p0) OR [Country] IS NULL)");
    }

    [Fact]
    public void CompileGroupRows_WithOnlyANullKey_ShouldUseIsNullAlone()
    {
        var query = Query(b => b.GroupBy(new GroupByDescriptor("Country")));

        var sql = ForSqlServer().CompileGroupRows(query, Columns, [null]);

        sql.Text.Should().Contain("WHERE [Country] IS NULL");
        sql.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void CompileGroupRows_ShouldCombineTheFilterWithTheKeyPredicate()
    {
        var query = Query(b => b
            .Where(Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, 18)))
            .GroupBy(new GroupByDescriptor("Country")));

        var sql = ForSqlServer().CompileGroupRows(query, Columns, ["Iran"]);

        sql.Text.Should().Contain("WHERE (([Age] > @p0)) AND [Country] IN (@p1)");
    }

    [Fact]
    public void CompileGroupRows_ShouldNotPage_BecauseCountsNeedEveryRowInTheGroup()
    {
        var query = Query(b => b
            .GroupBy(new GroupByDescriptor("Country"))
            .Page(5, 1));

        var sql = ForSqlServer().CompileGroupRows(query, Columns, ["Iran"]);

        sql.Text.Should().NotContain("OFFSET");
    }

    [Fact]
    public void CompileGroupRows_WithExplicitProjection_ShouldStillIncludeGroupingColumns()
    {
        var query = Query(b => b
            .Select("Id", "Name")
            .GroupBy(new GroupByDescriptor("Country")));

        var sql = ForSqlServer().CompileGroupRows(query, Columns, ["Iran"]);

        // Without Country in the projection the hierarchy could not be rebuilt from the rows.
        sql.Text.Should().StartWith("SELECT [Id], [Name], [Country] FROM");
    }

    [Fact]
    public void CompileGroupKeys_WithOnlyUnknownGroupColumns_ShouldThrow()
    {
        var query = Query(b => b.GroupBy(new GroupByDescriptor("PasswordHash")));

        var act = () => ForSqlServer().CompileGroupKeys(query, Columns);

        act.Should().Throw<InvalidOperationException>().WithMessage("*GroupByColumn*");
    }

    #endregion

    #region Objects

    [Fact]
    public void CompileSchemaProbe_ShouldSelectNoRows()
    {
        var sql = ForSqlServer().CompileSchemaProbe(Query());

        sql.Text.Should().Be("SELECT * FROM [dbo].[Users] WHERE 1 = 0");
    }

    [Fact]
    public void CompileRows_WithoutAnObject_ShouldThrow()
    {
        var act = () => ForSqlServer().CompileRows(new DapperQuery(), Columns);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Query.Object must be specified*");
    }

    [Fact]
    public void CompileRows_TableValuedFunction_ShouldPassArgumentsAsParameters()
    {
        var query = DapperQueryBuilder
            .ForObject(
                "tvf_UsersByTenant",
                "dbo",
                DapperObjectType.TVF,
                new Dictionary<string, object?> { ["TenantId"] = 42 })
            .Build();

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("FROM [dbo].[tvf_UsersByTenant](@p0)");
        sql.Parameters["p0"].Should().Be(42);
    }

    [Fact]
    public void CompileRows_WithEmptySchema_ShouldUseTheDialectDefault()
    {
        var query = DapperQueryBuilder.ForObject("Users").Build();

        var sql = ForSqlServer().CompileRows(query, Columns);

        sql.Text.Should().Contain("FROM [dbo].[Users]");
    }

    #endregion
}
