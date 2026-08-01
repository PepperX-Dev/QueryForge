using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PepperX.QueryForge.Dapper;
using PepperX.QueryForge.EFCore;
using PepperX.QueryForge.InMemory;
using System.Data;

namespace PepperX.QueryForge.Sample.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "PepperX.QueryForge Sample API";
                    document.Info.Description = """
                        # 🚀 Welcome to PepperX.QueryForge
            
                        This API demonstrates the full capabilities of the QueryForge dynamic query engine using .NET 10 Minimal APIs.

                        Every endpoint accepts the **same** `Query` body. Groups 1-5 execute it through
                        **Dapper**, group 6 through **Entity Framework Core**, and group 7 against an
                        **in-memory** collection — so you can post one payload to all three and compare
                        the responses.
            
                        ### ⚠️ Prerequisites
                        Before testing these endpoints, you **must** run the `Scripts.sql` file (located in the root of this project) against your SQL Server database. 
            
                        This script will create:
                        1. The `TestUsers` table with sample data.
                        2. The `vw_ActiveUsers` View.
                        3. The `tvf_GetUsersByTenant` Table-Valued Function.
                        4. The `usp_GetUserReport` Stored Procedure.
            
                        *(Note: QueryForge itself needs nothing deployed to your database. It compiles
                        parameterized SQL at query time, so no schema permissions are required.)*
                        """;

                    return Task.CompletedTask;
                });
            });

            builder.Services.AddScoped<IDbConnection>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new SqlConnection(config.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddQueryForgeDapper(options =>
            {
                options.ConnectionFactory = sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    return new SqlConnection(config.GetConnectionString("DefaultConnection"));
                };
            });

            builder.Services.AddDbContext<SampleDbContext>(options =>
            {
                var config = builder.Configuration;
                options.UseSqlServer(config.GetConnectionString("DefaultConnection"));
            });

            // The in-memory provider needs no registration at all; this is just the sample data.
            builder.Services.AddSingleton<IReadOnlyList<TestUser>>(SampleData.Users);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                // Exposes /openapi/v1.json — consumed by .http files, Postman, or frontend codegen
                app.MapOpenApi();
            }

            // ==========================================
            // ENDPOINTS
            // ==========================================

            // ==========================================
            // GROUP 1: CORE FLAT QUERIES
            // ==========================================
            var coreApi = app.MapGroup("/api/users").WithTags("1. Core Flat Queries");

            coreApi.MapPost("/query", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("StandardFlatQuery")
            .WithSummary("Standard flat query via injected service.")
            .WithDescription("Basic paging and sorting. Returns a flat JSON array of `TestUser`.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            coreApi.MapPost("/query-raw", async (Query q, IDbConnection conn) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await conn.QueryForgeAsync<TestUser>(dq);
            })
            .WithName("RawConnectionQuery")
            .WithSummary("Query via IDbConnection extension method.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            coreApi.MapPost("/projection", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("ColumnProjection")
            .WithSummary("Select specific columns to save bandwidth.")
            .WithDescription("Use `selectColumns` to only fetch specific fields. Unselected fields will be null/default.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            coreApi.MapPost("/deep-pagination", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("DeepPagination")
            .WithSummary("Fetch page 5 of a large dataset.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            // ==========================================
            // GROUP 2: ADVANCED FILTERING
            // ==========================================
            var filterApi = app.MapGroup("/api/users/filters").WithTags("2. Advanced Filtering");

            filterApi.MapPost("/text-search", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("TextSearch")
            .WithSummary("Contains, StartsWith, and EndsWith operators.")
            .WithDescription("Operator 2=Contains, 4=StartsWith, 5=EndsWith. Automatically escapes SQL wildcards (`%`, `_`).")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            filterApi.MapPost("/null-checks", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("NullChecks")
            .WithSummary("IS NULL and IS NOT NULL checks.")
            .WithDescription("Pass `value: null` with Operator 0 (Equals) to generate `IS NULL`. Use Operator 1 (NotEquals) for `IS NOT NULL`.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            filterApi.MapPost("/range", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("RangeFiltering")
            .WithSummary("Between operator for numeric or date ranges.")
            .WithDescription("Use Operator 10 (Between). Requires both `value` and `valueTo`.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            filterApi.MapPost("/complex-logic", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("ComplexLogic")
            .WithSummary("Nested OR, AND NOT logic.")
            .WithDescription("Logic Enums: 0=And, 1=Or, 2=AndNot, 3=OrNot.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            // ==========================================
            // GROUP 3: SECURITY & VALIDATION
            // ==========================================
            var secApi = app.MapGroup("/api/users/security").WithTags("3. Security & Validation");

            secApi.MapPost("/silent-strip", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                dq.Validate(r => { r.Select(c => c.Deny("DeletedAt")); r.PageSize(p => p.Max(100)); }, QueryValidationMode.SilentStrip);
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("SilentStripValidation")
            .WithSummary("Silently removes denied columns and clamps page size.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            secApi.MapPost("/strict-validation", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table).Build();
                try
                {
                    dq.Validate(r => r.Select(c => c.Deny("PasswordHash", "SecretKey")), QueryValidationMode.ThrowException);
                    return Results.Ok(await svc.QueryAsync<TestUser>(dq));
                }
                catch (QueryValidationException ex)
                {
                    return Results.ValidationProblem(ex.InvalidProperties.ToDictionary(x => x, x => new[] { "Denied by security policy" }));
                }
            })
            .WithName("StrictValidation")
            .WithSummary("Throws 400 Bad Request if denied columns are requested.")
            .Accepts<Query>("application/json")
            .Produces<QueryResult<TestUser>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(); // Native .NET 10 OpenAPI 400 response mapping

            // ==========================================
            // GROUP 4: HIERARCHIES & GROUPING
            // ==========================================
            var grpApi = app.MapGroup("/api/users/hierarchy").WithTags("4. Hierarchies & Grouping");

            grpApi.MapPost("/grouped-2-level", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table)
                    .GroupBy(new GroupByDescriptor("Country"), new GroupByDescriptor("Department")).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("TwoLevelGrouping")
            .WithSummary("Groups by Country -> Department.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            grpApi.MapPost("/grouped-3-level", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table)
                    .GroupBy(new GroupByDescriptor("Country"), new GroupByDescriptor("Department"), new GroupByDescriptor("Role")).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("ThreeLevelGrouping")
            .WithSummary("Groups by Country -> Department -> Role.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            grpApi.MapPost("/grouped-filtered", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("TestUsers", "dbo",DapperObjectType.Table)
                    .GroupBy(new GroupByDescriptor("Country")).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("FilteredGrouping")
            .WithSummary("Applies complex WHERE clause before grouping.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            // ==========================================
            // GROUP 5: DATABASE OBJECTS (VIEWS, TVFs, SPs)
            // ==========================================
            var objApi = app.MapGroup("/api/objects").WithTags("5. Database Objects");

            objApi.MapPost("/view", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("vw_ActiveUsers", "dbo",DapperObjectType.View).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("QueryView")
            .WithSummary("Executes against a Database View.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            objApi.MapPost("/tvf", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("tvf_GetUsersByTenant", "dbo",DapperObjectType.TVF, new Dictionary<string, object?> { { "TenantId", 1 } }).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("QueryTVF")
            .WithSummary("Executes against a Table-Valued Function with parameters.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            objApi.MapPost("/stored-procedure", async (Query q, IDapperQueryService svc) =>
            {
                var dq = DapperQueryBuilder.FromBase(q).ForObject("usp_GetUserReport", "dbo",DapperObjectType.SP, new Dictionary<string, object?> { { "IncludeDeleted", false } }).Build();
                return await svc.QueryAsync<TestUser>(dq);
            })
            .WithName("QueryStoredProcedure")
            .WithSummary("Executes against a Stored Procedure.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            // ==========================================
            // GROUP 6: ENTITY FRAMEWORK CORE PROVIDER
            // ==========================================
            var efApi = app.MapGroup("/api/efcore/users").WithTags("6. EF Core Provider");

            efApi.MapPost("/query", async (Query q, SampleDbContext db) =>
                await db.Users.AsNoTracking().ToQueryResultAsync<TestUser>(q))
            .WithName("EfCoreFlatQuery")
            .WithSummary("Flat query through EF Core.")
            .WithDescription("The same Query body as the Dapper endpoints. EF Core generates the SQL, so this works on any database EF Core supports.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            efApi.MapPost("/grouped-query", async (Query q, SampleDbContext db) =>
            {
                q.Validate(rules => rules.GroupBy(c => c.Allow("Country", "Department", "Role")),
                    QueryValidationMode.SilentStrip);

                return await db.Users.AsNoTracking().ToQueryResultAsync<TestUser>(q);
            })
            .WithName("EfCoreGroupedQuery")
            .WithSummary("Grouped query through EF Core.")
            .WithDescription("Send groupByColumns to get a nested key/count/items tree. Paging applies to the outermost group.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            efApi.MapPost("/scoped", async (Query q, SampleDbContext db) =>
                // A server-side restriction the client cannot query away.
                await db.Users.AsNoTracking().Where(u => u.IsActive).ToQueryResultAsync<TestUser>(q))
            .WithName("EfCoreScopedQuery")
            .WithSummary("Composes with an existing IQueryable restriction.")
            .WithDescription("Demonstrates that a Where applied before QueryForge survives — the pattern to use for tenant isolation.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            efApi.MapPost("/sql", (Query q, SampleDbContext db) =>
                Results.Text(db.Users.ApplyQuery(q).ToQueryString(), "text/plain"))
            .WithName("EfCoreShowSql")
            .WithSummary("Returns the SQL EF Core would run, without executing it.")
            .WithDescription("Useful for seeing that values become parameters rather than inlined literals.")
            .Accepts<Query>("application/json").Produces<string>();

            // ==========================================
            // GROUP 7: IN-MEMORY PROVIDER
            // ==========================================
            var memApi = app.MapGroup("/api/inmemory/users").WithTags("7. In-Memory Provider");

            memApi.MapPost("/query", (Query q, IReadOnlyList<TestUser> users) =>
                users.ToQueryResult(q))
            .WithName("InMemoryFlatQuery")
            .WithSummary("Flat query against a plain in-memory collection.")
            .WithDescription("No database involved. Runs against the sample data held in memory, and needs no connection string to try.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            memApi.MapPost("/grouped-query", (Query q, IReadOnlyList<TestUser> users) =>
                users.ToQueryResult(q))
            .WithName("InMemoryGroupedQuery")
            .WithSummary("Grouped query against an in-memory collection.")
            .WithDescription("Produces the same hierarchy shape as the Dapper and EF Core endpoints.")
            .Accepts<Query>("application/json").Produces<QueryResult<TestUser>>();

            memApi.MapPost("/validated", (Query q, IReadOnlyList<TestUser> users) =>
            {
                try
                {
                    q.Validate(rules =>
                    {
                        rules.Select(c => c.Deny("DeletedAt"));
                        rules.PageSize(p => p.Max(50));
                    }, QueryValidationMode.ThrowException);

                    return Results.Ok(users.ToQueryResult(q));
                }
                catch (QueryValidationException ex)
                {
                    return Results.ValidationProblem(
                        ex.InvalidProperties.ToDictionary(x => x, x => new[] { "Denied by security policy" }));
                }
            })
            .WithName("InMemoryValidatedQuery")
            .WithSummary("Strict validation, no database required.")
            .WithDescription("Ask for DeletedAt or a page size above 50 to see a 400 response.")
            .Accepts<Query>("application/json")
            .Produces<QueryResult<TestUser>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

            app.Run();
        }
    }

    public class TestUser
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int Age { get; set; }
        public decimal Score { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Minimal EF Core context over the same TestUsers table the Dapper endpoints use, so the two
    /// providers can be pointed at identical data.
    /// </summary>
    public class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
    {
        public DbSet<TestUser> Users => Set<TestUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestUser>(entity =>
            {
                entity.ToTable("TestUsers");
                entity.HasKey(u => u.UserId);
            });
        }
    }

    /// <summary>
    /// Seed rows for the in-memory endpoints, so that group 7 is runnable with no database at all.
    /// </summary>
    public static class SampleData
    {
        public static IReadOnlyList<TestUser> Users { get; } = Build();

        private static TestUser[] Build()
        {
            string[] countries = ["Germany", "Canada", "Iran", "Japan"];
            string[] departments = ["HR", "IT", "Sales", "Marketing"];
            string[] roles = ["Lead", "Member"];

            return Enumerable.Range(1, 40).Select(i => new TestUser
            {
                UserId = i,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Email = $"user{i}@example.com",
                Country = countries[i % countries.Length],
                City = $"City{i % 7}",
                Department = departments[i % departments.Length],
                Age = 20 + (i % 40),
                Score = 40 + (i % 60),
                IsActive = i % 3 != 0,
                CreatedOn = new DateTime(2020, 1, 1).AddDays(i * 7),
                DeletedAt = i % 11 == 0 ? new DateTime(2024, 1, 1) : null,
                Role = roles[i % roles.Length]
            }).ToArray();
        }
    }
}
