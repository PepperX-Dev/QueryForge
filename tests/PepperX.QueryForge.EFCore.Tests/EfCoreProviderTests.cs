using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PepperX.QueryForge.EFCore;

namespace PepperX.QueryForge.EFCore.Tests;

/// <summary>
/// Exercises the provider against a real EF Core context backed by SQLite.
/// </summary>
/// <remarks>
/// A real relational provider is used rather than the in-memory one on purpose: it forces the
/// expression trees to be translated into SQL, so a fragment EF Core cannot translate fails the test
/// instead of silently evaluating on the client.
/// </remarks>
public sealed class EfCoreProviderTests : IDisposable
{
    public sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Country { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinedOn { get; set; }
    }

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
    }

    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly TestContext _context;

    public EfCoreProviderTests()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite(_connection)
            // Client evaluation of a top-level Where would hide translation failures.
            .ConfigureWarnings(w => w.Throw(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.NonQueryOperationFailed))
            .Options;

        _context = new TestContext(options);
        _context.Database.EnsureCreated();

        _context.Users.AddRange(
            new User { Id = 1, Name = "Amir", Country = "Iran", Age = 30, IsActive = true, JoinedOn = new DateTime(2020, 1, 1) },
            new User { Id = 2, Name = "Bita", Country = "Iran", Age = 25, IsActive = true, JoinedOn = new DateTime(2021, 6, 1) },
            new User { Id = 3, Name = "Carl", Country = "Canada", Age = 40, IsActive = false, JoinedOn = new DateTime(2019, 3, 1) },
            new User { Id = 4, Name = "Dana", Country = null, Age = 35, IsActive = true, JoinedOn = new DateTime(2022, 9, 1) },
            new User { Id = 5, Name = "Emma", Country = "Canada", Age = 22, IsActive = false, JoinedOn = new DateTime(2023, 2, 1) },
            new User { Id = 6, Name = "Farid", Country = "Iran", Age = 45, IsActive = true, JoinedOn = new DateTime(2018, 7, 1) });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static QueryCriteria Criteria(Logic logic, params Condition[] conditions)
        => new([new ConditionGroup(conditions, logic)]);

    #region Flat queries

    [Fact]
    public async Task ToQueryResultAsync_WithoutFilters_ShouldPageAndReportTotals()
    {
        var query = new Query
        {
            Paging = new QueryPaging(Size: 4, Number: 1),
            SortColumns = [new SortDescriptor("Id")]
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Type.Should().Be(QueryResultType.Flat);
        result.Meta.Total.Rows.Should().Be(6);
        result.Meta.Total.Pages.Should().Be(2);
        result.Models.Select(u => u.Id).Should().ContainInOrder(1, 2, 3, 4);
    }

    [Theory]
    [InlineData(ConditionOperator.Equals, 30, 1)]
    [InlineData(ConditionOperator.NotEquals, 30, 5)]
    [InlineData(ConditionOperator.LessThan, 30, 2)]
    [InlineData(ConditionOperator.GreaterThan, 30, 3)]
    [InlineData(ConditionOperator.LessThanOrEqualTo, 30, 3)]
    [InlineData(ConditionOperator.GreaterThanOrEqualTo, 30, 4)]
    public async Task ToQueryResultAsync_ComparisonOperators_ShouldTranslateToSql(
        ConditionOperator op, int value, int expected)
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", op, value)),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Total.Rows.Should().Be(expected);
    }

    [Theory]
    [InlineData(ConditionOperator.Contains, "ra", 3)]
    [InlineData(ConditionOperator.StartsWith, "Can", 2)]
    [InlineData(ConditionOperator.EndsWith, "da", 2)]
    [InlineData(ConditionOperator.NotContains, "ra", 2)]
    public async Task ToQueryResultAsync_TextOperators_ShouldTranslateToSql(
        ConditionOperator op, string value, int expected)
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Country", op, value)),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Total.Rows.Should().Be(expected);
    }

    [Fact]
    public async Task ToQueryResultAsync_Between_ShouldBeInclusive()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.Between, 25, 35)),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Models.Select(u => u.Id).Should().BeEquivalentTo([1, 2, 4]);
    }

    [Fact]
    public async Task ToQueryResultAsync_EqualsNull_ShouldMatchNullColumn()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Country", ConditionOperator.Equals, null))
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Models.Should().ContainSingle().Which.Name.Should().Be("Dana");
    }

    [Fact]
    public async Task ToQueryResultAsync_StringValueForNumericColumn_ShouldBeCoerced()
    {
        // Values arriving from JSON are often strings; the column's type decides the comparison.
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, "9")),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Total.Rows.Should().Be(6);
    }

    [Fact]
    public async Task ToQueryResultAsync_BooleanColumn_ShouldFilter()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("IsActive", ConditionOperator.Equals, true)),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Total.Rows.Should().Be(4);
    }

    [Fact]
    public async Task ToQueryResultAsync_DateColumn_ShouldAcceptAnIsoString()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("JoinedOn", ConditionOperator.GreaterThan, "2021-01-01")),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Models.Select(u => u.Name).Should().BeEquivalentTo("Bita", "Dana", "Emma");
    }

    #endregion

    #region Logic

    [Fact]
    public async Task ToQueryResultAsync_OrGroup_ShouldUnionConditions()
    {
        var query = new Query
        {
            Criteria = Criteria(
                Logic.Or,
                new Condition("Country", ConditionOperator.Equals, "Canada"),
                new Condition("Age", ConditionOperator.GreaterThan, 40)),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Models.Select(u => u.Name).Should().BeEquivalentTo("Carl", "Emma", "Farid");
    }

    [Fact]
    public async Task ToQueryResultAsync_NegatedGroup_ShouldExcludeMatches()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.AndNot, new Condition("Country", ConditionOperator.Equals, "Iran")),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Models.Select(u => u.Name).Should().Contain(["Carl", "Emma"]);
        result.Models.Select(u => u.Name).Should().NotContain("Amir");
    }

    [Fact]
    public async Task ToQueryResultAsync_MultipleGroups_ShouldJoinWithCriteriaLogic()
    {
        var query = new Query
        {
            Criteria = new QueryCriteria(
                [
                    new ConditionGroup([new Condition("Country", ConditionOperator.Equals, "Iran")]),
                    new ConditionGroup([new Condition("Age", ConditionOperator.GreaterThan, 28)])
                ],
                Logic.And),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Models.Select(u => u.Name).Should().BeEquivalentTo("Amir", "Farid");
    }

    #endregion

    #region Safety

    [Fact]
    public async Task ToQueryResultAsync_UnknownColumn_ShouldBeDroppedNotThrown()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("PasswordHash", ConditionOperator.Equals, "x")),
            SortColumns = [new SortDescriptor("NoSuchColumn")],
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Total.Rows.Should().Be(6);
    }

    [Fact]
    public async Task ToQueryResultAsync_ShouldComposeWithAnExistingRestriction()
    {
        // The caller's own Where must survive, so a tenant filter cannot be queried away.
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, 1)),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users
            .Where(u => u.Country == "Iran")
            .ToQueryResultAsync(query);

        result.Meta.Total.Rows.Should().Be(3);
    }

    [Fact]
    public async Task ToQueryResultAsync_ValueThatCannotBeCoerced_ShouldDropTheCondition()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.Equals, "not-a-number")),
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Total.Rows.Should().Be(6);
    }

    #endregion

    #region Sorting and grouping

    [Fact]
    public async Task ApplySort_ShouldApplyEveryLevelInOrder()
    {
        var query = new Query
        {
            SortColumns =
            [
                new SortDescriptor("Country"),
                new SortDescriptor("Age", SortOrder.Descending)
            ]
        };

        var names = await _context.Users.ApplySort(query).Select(u => u.Name).ToListAsync();

        names.Should().ContainInOrder("Dana", "Carl", "Emma", "Farid", "Amir", "Bita");
    }

    [Fact]
    public async Task ToQueryResultAsync_Grouped_ShouldNestRowsAndCountGroups()
    {
        var query = new Query
        {
            GroupByColumns = [new GroupByDescriptor("Country")],
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Meta.Type.Should().Be(QueryResultType.Grouped);
        result.Meta.Total.Rows.Should().Be(3);

        var iran = result.Groups.Single(g => Equals(g.Key, "Iran"));
        iran.Count.Should().Be(3);
        iran.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task ToQueryResultAsync_Grouped_ShouldPageOverGroupsNotRows()
    {
        var query = new Query
        {
            GroupByColumns = [new GroupByDescriptor("Country")],
            Paging = new QueryPaging(Size: 1, Number: 2)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Groups.Should().ContainSingle();
        result.Groups[0].Key.Should().Be("Canada");
        result.Groups[0].Count.Should().Be(2);
        result.Meta.Total.Pages.Should().Be(3);
    }

    [Fact]
    public async Task ToQueryResultAsync_GroupedTwoLevels_ShouldNestAtEachDepth()
    {
        var query = new Query
        {
            GroupByColumns = [new GroupByDescriptor("Country"), new GroupByDescriptor("IsActive")],
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        var iran = result.Groups.Single(g => Equals(g.Key, "Iran"));
        iran.Items.Should().BeNull();
        iran.SubGroups.Should().NotBeNull();
        iran.SubGroups!.Sum(s => s.Count).Should().Be(3);
    }

    [Fact]
    public async Task ToQueryResultAsync_GroupedWithNullKey_ShouldKeepNullAsItsOwnGroup()
    {
        var query = new Query
        {
            GroupByColumns = [new GroupByDescriptor("Country")],
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Groups.Single(g => g.Key is null).Count.Should().Be(1);
    }

    [Fact]
    public async Task ToQueryResultAsync_GroupedWithFilter_ShouldFilterBeforeGrouping()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, 24)),
            GroupByColumns = [new GroupByDescriptor("Country")],
            Paging = new QueryPaging(Size: 50)
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Groups.Single(g => Equals(g.Key, "Canada")).Count.Should().Be(1);
    }

    [Fact]
    public async Task ToQueryResultAsync_GroupedWithNoMatches_ShouldReturnAnEmptyHierarchy()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Age", ConditionOperator.GreaterThan, 999)),
            GroupByColumns = [new GroupByDescriptor("Country")]
        };

        var result = await _context.Users.ToQueryResultAsync(query);

        result.Groups.Should().BeEmpty();
        result.Meta.Total.Rows.Should().Be(0);
    }

    #endregion

    #region Parameterization

    [Fact]
    public void ApplyFilter_ShouldEmitSqlParametersRatherThanInlinedLiterals()
    {
        var query = new Query
        {
            Criteria = Criteria(Logic.And, new Condition("Country", ConditionOperator.Equals, "Iran"))
        };

        var sql = _context.Users.ApplyFilter(query).ToQueryString();

        // ToQueryString() prints a ".param set" preamble showing the value, so assert on the
        // statement itself: an inlined literal there would defeat plan reuse.
        var statement = sql[sql.IndexOf("SELECT", StringComparison.Ordinal)..];

        statement.Should().NotContain("'Iran'");
        statement.Should().MatchRegex(@"WHERE .*= @\w+");
    }

    #endregion
}
