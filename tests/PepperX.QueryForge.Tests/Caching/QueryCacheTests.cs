using PepperX.QueryForge.Caching;
using Xunit;

namespace PepperX.QueryForge.Tests.Caching;

public class QueryCacheTests
{
    private sealed record TestModel(int Id, string Name);

    [Fact]
    public async Task MemoryQueryCache_SetAndGet_ReturnsCachedResult()
    {
        using var cache = new MemoryQueryCache();
        var key = "test:query:1";
        var result = new QueryResult<TestModel>
        {
            Models = [new TestModel(1, "Alice"), new TestModel(2, "Bob")],
            Meta = new QueryResultMeta(new QueryResultMetaTotal(2, 1), QueryResultType.Flat)
        };

        var options = new QueryCacheOptions { Enabled = true, Expiration = TimeSpan.FromMinutes(10) };
        await cache.SetAsync(key, result, options);

        var cached = await cache.GetAsync<TestModel>(key);

        Assert.NotNull(cached);
        Assert.Equal(2, cached.Models.Count);
        Assert.Equal("Alice", cached.Models[0].Name);
    }

    [Fact]
    public async Task MemoryQueryCache_Expiration_EvictsEntry()
    {
        using var cache = new MemoryQueryCache();
        var key = "test:query:expired";
        var result = new QueryResult<TestModel>
        {
            Models = [new TestModel(1, "Alice")],
            Meta = new QueryResultMeta(new QueryResultMetaTotal(1, 1), QueryResultType.Flat)
        };

        // Expire immediately
        var options = new QueryCacheOptions { Enabled = true, Expiration = TimeSpan.FromMilliseconds(1) };
        await cache.SetAsync(key, result, options);

        await Task.Delay(20);

        var cached = await cache.GetAsync<TestModel>(key);
        Assert.Null(cached);
    }

    [Fact]
    public async Task MemoryQueryCache_InvalidateTag_EvictsTaggedEntries()
    {
        using var cache = new MemoryQueryCache();
        var key1 = "test:query:users:1";
        var key2 = "test:query:users:2";
        var key3 = "test:query:orders:1";

        var dummy = new QueryResult<TestModel> { Models = [new TestModel(1, "Test")] };

        await cache.SetAsync(key1, dummy, new QueryCacheOptions { Tags = ["users", "admin"] });
        await cache.SetAsync(key2, dummy, new QueryCacheOptions { Tags = ["users"] });
        await cache.SetAsync(key3, dummy, new QueryCacheOptions { Tags = ["orders"] });

        // Invalidate "users" tag
        await cache.InvalidateTagAsync("users");

        Assert.Null(await cache.GetAsync<TestModel>(key1));
        Assert.Null(await cache.GetAsync<TestModel>(key2));
        Assert.NotNull(await cache.GetAsync<TestModel>(key3));
    }

    [Fact]
    public async Task MemoryQueryCache_Remove_RemovesEntry()
    {
        using var cache = new MemoryQueryCache();
        var key = "test:query:remove";
        var dummy = new QueryResult<TestModel> { Models = [new TestModel(1, "Test")] };

        await cache.SetAsync(key, dummy, new QueryCacheOptions());
        Assert.NotNull(await cache.GetAsync<TestModel>(key));

        await cache.RemoveAsync(key);
        Assert.Null(await cache.GetAsync<TestModel>(key));
    }

    [Fact]
    public void QueryCacheKeyGenerator_IdenticalQueries_ProduceIdenticalKeys()
    {
        var q1 = new Query
        {
            Criteria = new QueryCriteria
            {
                Groups =
                [
                    new ConditionGroup
                    {
                        Conditions = [new Condition("Name", ConditionOperator.Equals, "Test")]
                    }
                ]
            },
            Paging = new QueryPaging { Number = 1, Size = 20 },
            SortColumns = [new SortDescriptor("Id", SortOrder.Ascending)]
        };

        var q2 = new Query
        {
            Criteria = new QueryCriteria
            {
                Groups =
                [
                    new ConditionGroup
                    {
                        Conditions = [new Condition("Name", ConditionOperator.Equals, "Test")]
                    }
                ]
            },
            Paging = new QueryPaging { Number = 1, Size = 20 },
            SortColumns = [new SortDescriptor("Id", SortOrder.Ascending)]
        };

        var key1 = QueryCacheKeyGenerator.GenerateKey<TestModel>(q1);
        var key2 = QueryCacheKeyGenerator.GenerateKey<TestModel>(q2);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void QueryCacheKeyGenerator_DifferentCriteria_ProduceDifferentKeys()
    {
        var q1 = new Query
        {
            Criteria = new QueryCriteria
            {
                Groups = [new ConditionGroup { Conditions = [new Condition("Name", ConditionOperator.Equals, "A")] }]
            }
        };

        var q2 = new Query
        {
            Criteria = new QueryCriteria
            {
                Groups = [new ConditionGroup { Conditions = [new Condition("Name", ConditionOperator.Equals, "B")] }]
            }
        };

        var key1 = QueryCacheKeyGenerator.GenerateKey<TestModel>(q1);
        var key2 = QueryCacheKeyGenerator.GenerateKey<TestModel>(q2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void QueryCacheKeyGenerator_DifferentPaging_ProduceDifferentKeys()
    {
        var q1 = new Query { Paging = new QueryPaging { Number = 1, Size = 10 } };
        var q2 = new Query { Paging = new QueryPaging { Number = 2, Size = 10 } };

        var key1 = QueryCacheKeyGenerator.GenerateKey<TestModel>(q1);
        var key2 = QueryCacheKeyGenerator.GenerateKey<TestModel>(q2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void QueryCacheKeyGenerator_ExplicitKey_IsHonored()
    {
        var q = new Query
        {
            Cache = new QueryCacheOptions { Key = "my-custom-key" }
        };

        var key = QueryCacheKeyGenerator.GenerateKey<TestModel>(q);
        Assert.Equal("my-custom-key", key);
    }

    [Fact]
    public void Query_WithCache_FluentExtension_SetsOptionsCorrectly()
    {
        var q = new Query().WithCache(TimeSpan.FromMinutes(15), tags: ["users", "test"]);

        Assert.NotNull(q.Cache);
        Assert.True(q.Cache.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(15), q.Cache.Expiration);
        Assert.Contains("users", q.Cache.Tags);
        Assert.Contains("test", q.Cache.Tags);

        q.WithoutCache();
        Assert.False(q.Cache.Enabled);
    }
}
