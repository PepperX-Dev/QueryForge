using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using PepperX.QueryForge.Dapper.Internals;

namespace PepperX.QueryForge.Dapper.Tests.Services;

public class DapperQueryServiceTests
{
    private sealed class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Fact]
    public async Task QueryAsync_WithoutConnection_AndNoFactory_ShouldExplainHowToFixIt()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions { ConnectionFactory = null });
        var service = new DapperQueryService(registry, new FakeServiceProvider());

        var query = DapperQueryBuilder.ForObject("Users").Build();

        var act = async () => await service.QueryAsync<object>(query);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ConnectionFactory was not configured*")
            .WithMessage("*takes an IDbConnection*");
    }

    [Fact]
    public async Task QueryAsync_WithAnUnsupportedConnection_ShouldThrow()
    {
        var registry = new DapperRegistry(new DapperQueryForgeOptions());
        var service = new DapperQueryService(registry, new FakeServiceProvider());

        var query = DapperQueryBuilder.ForObject("Users").Build();

        var act = async () => await service.QueryAsync<object>(new FirebirdConnection(), query);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    private sealed record UserDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }

    [Fact]
    public async Task QueryAsync_WithCacheHit_ReturnsCachedResultWithoutExecutingQuery()
    {
        var cache = new PepperX.QueryForge.Caching.MemoryQueryCache();
        var options = new DapperQueryForgeOptions { Cache = cache };
        var registry = new DapperRegistry(options);
        registry.Register(new PepperX.QueryForge.Dapper.Dialects.SqlServerDialect());
        var service = new DapperQueryService(registry, new FakeServiceProvider());

        var query = DapperQueryBuilder.ForObject("Users")
            .WithCache(TimeSpan.FromMinutes(5))
            .Build();

        var expectedResult = new QueryResult<UserDto>
        {
            Models = [new UserDto { Id = 1, Name = "CachedUser" }],
            Meta = new QueryResultMeta(new QueryResultMetaTotal(1, 1), QueryResultType.Flat)
        };

        var cacheKey = PepperX.QueryForge.Caching.QueryCacheKeyGenerator.GenerateKey<UserDto>(query, "Users", "dapper");
        await cache.SetAsync(cacheKey, expectedResult, query.Cache!);

        // Using a stub connection named SqlConnection — returns cached result without running query!
        var result = await service.QueryAsync<UserDto>(new SqlConnection(), query);

        result.Should().NotBeNull();
        result.Models.Should().HaveCount(1);
        result.Models[0].Name.Should().Be("CachedUser");
    }

    private sealed class SqlConnection : IDbConnection
    {
        [AllowNull]
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 0;
        public string Database => "";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Dispose() { }
        public void Open() { }
    }

    private sealed class FirebirdConnection : IDbConnection
    {
        // IDbConnection.ConnectionString allows null on the way in; the stub keeps a non-null value.
        [AllowNull]
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 0;
        public string Database => "";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Dispose() { }
        public void Open() { }
    }
}
