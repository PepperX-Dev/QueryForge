using FluentAssertions;
using PepperX.QueryForge.Dapper.Compiler;
using PepperX.QueryForge.Dapper.Dialects;

namespace PepperX.QueryForge.Dapper.Tests.Compiler;

/// <summary>
/// Compiles the same query for every dialect, so the differences between engines stay visible and
/// deliberate rather than accidental.
/// </summary>
public class DialectTests
{
    private static readonly ColumnWhitelist Columns = new(["Id", "Name", "Country", "Age"]);

    public static TheoryData<ISqlDialect> AllDialects() =>
    [
        new SqlServerDialect(),
        new PostgreSqlDialect(),
        new MySqlDialect(),
        new OracleDialect(),
        new SqliteDialect()
    ];

    private static DapperQuery SimpleQuery() =>
        DapperQueryBuilder
            .ForObject("Users")
            .Where(new QueryCriteria([
                new ConditionGroup([new Condition("Country", ConditionOperator.Equals, "Iran")])
            ]))
            .Sort(new SortDescriptor("Age", SortOrder.Descending))
            .Page(size: 10, number: 3)
            .Build();

    #region Identifier quoting

    [Theory]
    [InlineData(typeof(SqlServerDialect), "[Name]")]
    [InlineData(typeof(PostgreSqlDialect), "\"Name\"")]
    [InlineData(typeof(MySqlDialect), "`Name`")]
    [InlineData(typeof(OracleDialect), "\"Name\"")]
    [InlineData(typeof(SqliteDialect), "\"Name\"")]
    public void QuoteIdentifier_ShouldUseTheEngineDelimiters(Type dialectType, string expected)
    {
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;

        dialect.QuoteIdentifier("Name").Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(AllDialects))]
    public void QuoteIdentifier_ShouldNeutralizeItsOwnClosingDelimiter(ISqlDialect dialect)
    {
        // A name carrying the delimiter must not be able to close the quote and inject SQL.
        var quoted = dialect.QuoteIdentifier("Na]me\"x`y");

        var delimiter = dialect.QuoteIdentifier("x")[^1];
        var body = quoted[1..^1];

        // Every occurrence of the closing delimiter inside the body must be doubled.
        for (var i = 0; i < body.Length; i++)
        {
            if (body[i] != delimiter) continue;

            (i + 1 < body.Length && body[i + 1] == delimiter).Should().BeTrue(
                "an unescaped {0} would end the identifier early", delimiter);
            i++;
        }
    }

    #endregion

    #region Paging

    [Theory]
    [InlineData(typeof(SqlServerDialect), "OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    [InlineData(typeof(PostgreSqlDialect), "LIMIT 10 OFFSET 20")]
    [InlineData(typeof(MySqlDialect), "LIMIT 10 OFFSET 20")]
    [InlineData(typeof(OracleDialect), "OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    [InlineData(typeof(SqliteDialect), "LIMIT 10 OFFSET 20")]
    public void CompileRows_ShouldPageUsingTheEngineSyntax(Type dialectType, string expected)
    {
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;

        var sql = new SqlQueryCompiler(dialect).CompileRows(SimpleQuery(), Columns);

        sql.Text.Should().EndWith(expected);
    }

    [Fact]
    public void CompileRows_SqlServerWithoutSorting_ShouldStillEmitAnOrderByBecausePagingNeedsOne()
    {
        var query = DapperQueryBuilder.ForObject("Users").Build();

        var sql = new SqlQueryCompiler(new SqlServerDialect()).CompileRows(query, Columns);

        sql.Text.Should().Contain("ORDER BY (SELECT NULL)");
    }

    [Fact]
    public void CompileRows_PostgresWithoutSorting_ShouldNotNeedAFabricatedOrderBy()
    {
        var query = DapperQueryBuilder.ForObject("Users").Build();

        var sql = new SqlQueryCompiler(new PostgreSqlDialect()).CompileRows(query, Columns);

        sql.Text.Should().NotContain("ORDER BY");
    }

    #endregion

    #region Parameters and schemas

    [Theory]
    [InlineData(typeof(SqlServerDialect), "@p0")]
    [InlineData(typeof(PostgreSqlDialect), "@p0")]
    [InlineData(typeof(MySqlDialect), "@p0")]
    [InlineData(typeof(OracleDialect), ":p0")]
    [InlineData(typeof(SqliteDialect), "@p0")]
    public void CompileRows_ShouldUseTheEngineParameterPrefix(Type dialectType, string expected)
    {
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;

        var sql = new SqlQueryCompiler(dialect).CompileRows(SimpleQuery(), Columns);

        sql.Text.Should().Contain(expected);
    }

    [Theory]
    [InlineData(typeof(SqlServerDialect), "[dbo].[Users]")]
    [InlineData(typeof(PostgreSqlDialect), "\"public\".\"Users\"")]
    [InlineData(typeof(MySqlDialect), "`Users`")]
    [InlineData(typeof(SqliteDialect), "\"Users\"")]
    public void CompileRows_WithNoExplicitSchema_ShouldApplyTheEngineDefault(Type dialectType, string expected)
    {
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;

        var sql = new SqlQueryCompiler(dialect).CompileRows(DapperQueryBuilder.ForObject("Users").Build(), Columns);

        sql.Text.Should().Contain($"FROM {expected}");
    }

    [Theory]
    [MemberData(nameof(AllDialects))]
    public void CompileRows_ShouldParameterizeEveryValueOnEveryEngine(ISqlDialect dialect)
    {
        var sql = new SqlQueryCompiler(dialect).CompileRows(SimpleQuery(), Columns);

        sql.Text.Should().NotContain("Iran");
        sql.Parameters.Values.Should().Contain("Iran");
    }

    #endregion

    #region Capability differences

    [Fact]
    public void CompileRows_MySqlTableValuedFunction_ShouldFailClearly()
    {
        var query = DapperQueryBuilder
            .ForObject("fn_Users", type: DapperObjectType.TVF)
            .Build();

        var act = () => new SqlQueryCompiler(new MySqlDialect()).CompileRows(query, Columns);

        act.Should().Throw<NotSupportedException>().WithMessage("*table-valued functions*");
    }

    [Fact]
    public void CompileRows_OracleTableValuedFunction_ShouldUnnestWithTable()
    {
        var query = DapperQueryBuilder
            .ForObject(
                "FN_USERS",
                "APP",
                DapperObjectType.TVF,
                new Dictionary<string, object?> { ["TENANT"] = 7 })
            .Build();

        var sql = new SqlQueryCompiler(new OracleDialect()).CompileRows(query, Columns);

        sql.Text.Should().Contain("FROM TABLE(\"APP\".\"FN_USERS\"(:p0))");
    }

    [Fact]
    public void BuildStoredProcedureCall_SqlServer_ShouldUseNamedArguments()
    {
        var sql = new SqlServerDialect().BuildStoredProcedureCall(
            "dbo", "usp_Report", [new KeyValuePair<string, string>("TenantId", "@p0")]);

        sql.Should().Be("EXEC [dbo].[usp_Report] @TenantId = @p0");
    }

    [Fact]
    public void BuildStoredProcedureCall_MySql_ShouldUsePositionalArguments()
    {
        var sql = new MySqlDialect().BuildStoredProcedureCall(
            "", "usp_Report", [new KeyValuePair<string, string>("TenantId", "@p0")]);

        sql.Should().Be("CALL `usp_Report`(@p0)");
    }

    [Fact]
    public void BuildStoredProcedureCall_PostgreSql_ShouldSelectFromTheFunction()
    {
        var sql = new PostgreSqlDialect().BuildStoredProcedureCall(
            "public", "report", [new KeyValuePair<string, string>("tenant_id", "@p0")]);

        sql.Should().Be("SELECT * FROM \"public\".\"report\"(tenant_id => @p0)");
    }

    [Fact]
    public void BuildStoredProcedureCall_Oracle_ShouldExplainTheAlternative()
    {
        var act = () => new OracleDialect().BuildStoredProcedureCall("APP", "REPORT", []);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*REF CURSOR*")
            .WithMessage("*pipelined function*");
    }

    [Theory]
    [MemberData(nameof(AllDialects))]
    public void EscapeLikeValue_ShouldNeutralizeWildcards(ISqlDialect dialect)
    {
        var escaped = dialect.EscapeLikeValue("50%_off");

        escaped.Should().NotBe("50%_off");
        escaped.Should().Contain(@"\%").And.Contain(@"\_");
    }

    #endregion
}
