using FluentAssertions;
using PepperX.QueryForge.Dapper.Compiler;
using PepperX.QueryForge.Dapper.Dialects;

namespace PepperX.QueryForge.Dapper.Tests.Compiler;

/// <summary>
/// The two places where engines disagree by default, pinned so they cannot drift apart again.
/// </summary>
/// <remarks>
/// Both of these were found by running the suites against real PostgreSQL and MySQL servers rather
/// than only asserting SQL text, and both would have produced different answers on different
/// databases for the same request.
/// </remarks>
public class DialectPortabilityTests
{
    private static readonly ColumnWhitelist Typed = new(
    [
        new KeyValuePair<string, Type?>("Id", typeof(int)),
        new KeyValuePair<string, Type?>("Name", typeof(string)),
        new KeyValuePair<string, Type?>("Quantity", typeof(int)),
        new KeyValuePair<string, Type?>("Price", typeof(double)),
        new KeyValuePair<string, Type?>("IsActive", typeof(bool)),
        new KeyValuePair<string, Type?>("ReleasedOn", typeof(DateTime)),
        new KeyValuePair<string, Type?>("Category", typeof(string))
    ]);

    private static DapperQuery Query(Action<DapperQueryFluent> configure)
    {
        var builder = DapperQueryBuilder.ForObject("Widget");
        configure(builder);

        return builder.Build();
    }

    private static QueryCriteria Where(params Condition[] conditions) => new([new ConditionGroup(conditions)]);

    #region Null ordering

    [Fact]
    public void OrderBy_PostgreSql_ShouldPinNullsFirstAscending()
    {
        // PostgreSQL defaults to nulls last, which would order differently from every other engine.
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Sort(new SortDescriptor("Category"))), Typed);

        sql.Text.Should().Contain("ORDER BY \"Category\" ASC NULLS FIRST");
    }

    [Fact]
    public void OrderBy_PostgreSql_ShouldPinNullsLastDescending()
    {
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Sort(new SortDescriptor("Category", SortOrder.Descending))), Typed);

        sql.Text.Should().Contain("ORDER BY \"Category\" DESC NULLS LAST");
    }

    [Fact]
    public void OrderBy_Oracle_ShouldPinNullOrderingToo()
    {
        var sql = new SqlQueryCompiler(new OracleDialect())
            .CompileRows(Query(b => b.Sort(new SortDescriptor("Category"))), Typed);

        sql.Text.Should().Contain("\"Category\" ASC NULLS FIRST");
    }

    [Theory]
    [InlineData(typeof(SqlServerDialect))]
    [InlineData(typeof(MySqlDialect))]
    [InlineData(typeof(SqliteDialect))]
    public void OrderBy_EnginesThatAlreadySortNullsFirst_ShouldNotEmitTheClause(Type dialectType)
    {
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;

        var sql = new SqlQueryCompiler(dialect)
            .CompileRows(Query(b => b.Sort(new SortDescriptor("Category"))), Typed);

        sql.Text.Should().NotContain("NULLS");
    }

    [Fact]
    public void GroupKeys_ShouldCarryTheSameNullOrdering()
    {
        // Group keys are ordered by the same rules, or a grouped page would differ per engine.
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileGroupKeys(Query(b => b.GroupBy(new GroupByDescriptor("Category"))), Typed);

        sql.Text.Should().Contain("ORDER BY \"Category\" ASC NULLS FIRST");
    }

    #endregion

    #region Value coercion

    [Fact]
    public void Value_TextSentForANumericColumn_ShouldBeBoundAsANumber()
    {
        // A JSON client sends "30"; PostgreSQL rejects integer > text outright.
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("Quantity", ConditionOperator.GreaterThan, "30")))), Typed);

        sql.Parameters["p0"].Should().Be(30).And.BeOfType<int>();
    }

    [Fact]
    public void Value_TextSentForADateColumn_ShouldBeBoundAsADate()
    {
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("ReleasedOn", ConditionOperator.GreaterThanOrEqualTo, "2023-01-01")))), Typed);

        sql.Parameters["p0"].Should().Be(new DateTime(2023, 1, 1)).And.BeOfType<DateTime>();
    }

    [Fact]
    public void Value_TextSentForABooleanColumn_ShouldBeBoundAsABoolean()
    {
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("IsActive", ConditionOperator.Equals, "true")))), Typed);

        sql.Parameters["p0"].Should().Be(true).And.BeOfType<bool>();
    }

    [Fact]
    public void Value_BothBetweenBounds_ShouldBeCoerced()
    {
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("Quantity", ConditionOperator.Between, "5", "50")))), Typed);

        sql.Parameters["p0"].Should().Be(5).And.BeOfType<int>();
        sql.Parameters["p1"].Should().Be(50).And.BeOfType<int>();
    }

    [Fact]
    public void Value_NumberSentForATextColumn_ShouldBeBoundAsText()
    {
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("Name", ConditionOperator.Equals, 42)))), Typed);

        sql.Parameters["p0"].Should().Be("42").And.BeOfType<string>();
    }

    [Fact]
    public void Value_ThatCannotRepresentTheColumnType_ShouldBePassedThroughAndMatchNothing()
    {
        // "abc" is not an integer. The filter must not throw, and must not match.
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("Quantity", ConditionOperator.Equals, "abc")))), Typed);

        sql.Parameters["p0"].Should().Be("abc");
    }

    [Fact]
    public void Value_WhenTheColumnTypeIsUnknown_ShouldBeLeftAlone()
    {
        var untyped = new ColumnWhitelist(["Quantity"]);

        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("Quantity", ConditionOperator.GreaterThan, "30")))), untyped);

        sql.Parameters["p0"].Should().Be("30");
    }

    [Fact]
    public void Value_ForAPatternOperator_ShouldStayTextEvenOnANumericColumn()
    {
        // A LIKE pattern is always text; coercing it to the column's type would destroy the wildcards.
        var sql = new SqlQueryCompiler(new PostgreSqlDialect())
            .CompileRows(Query(b => b.Where(Where(
                new Condition("Quantity", ConditionOperator.Contains, "5")))), Typed);

        sql.Parameters["p0"].Should().Be("%5%");
    }

    #endregion

    #region Syntax every engine accepts

    [Theory]
    [InlineData(typeof(SqlServerDialect))]
    [InlineData(typeof(PostgreSqlDialect))]
    [InlineData(typeof(MySqlDialect))]
    [InlineData(typeof(OracleDialect))]
    [InlineData(typeof(SqliteDialect))]
    public void GroupCount_ShouldAliasTheDerivedTableWithoutAs(Type dialectType)
    {
        // Oracle rejects AS in front of a table alias, and every other engine accepts the bare form,
        // so the compiler emits the one spelling that works on all of them.
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;

        var query = DapperQueryBuilder
            .ForObject("Widget")
            .GroupBy(new GroupByDescriptor("Category"))
            .Build();

        var sql = new SqlQueryCompiler(dialect).CompileGroupCount(query, Typed);

        sql.Text.Should().Contain(") qf_groups");
        sql.Text.Should().NotContain(") AS qf_groups");
    }

    [Theory]
    [InlineData(typeof(SqlServerDialect))]
    [InlineData(typeof(PostgreSqlDialect))]
    [InlineData(typeof(MySqlDialect))]
    [InlineData(typeof(OracleDialect))]
    [InlineData(typeof(SqliteDialect))]
    public void EveryStatement_ShouldParameterizeAndQuoteConsistently(Type dialectType)
    {
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;
        var compiler = new SqlQueryCompiler(dialect);

        var query = DapperQueryBuilder
            .ForObject("Widget")
            .Where(Where(new Condition("Category", ConditionOperator.Equals, "Tools")))
            .GroupBy(new GroupByDescriptor("Category"))
            .Sort(new SortDescriptor("Quantity", SortOrder.Descending))
            .Page(10, 2)
            .Build();

        CompiledSql[] statements =
        [
            compiler.CompileSchemaProbe(query),
            compiler.CompileGroupCount(query, Typed),
            compiler.CompileGroupKeys(query, Typed),
            compiler.CompileGroupRows(query, Typed, ["Tools"])
        ];

        foreach (var statement in statements)
        {
            statement.Text.Should().NotContain("'Tools'", "values are always parameters");
            statement.Text.Should().NotContain("Category ", "identifiers are always quoted");
        }
    }

    #endregion
}
