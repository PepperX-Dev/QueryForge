using Microsoft.Data.SqlClient;
using PepperX.QueryForge.Dapper;
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
            
                        ### ⚠️ Prerequisites
                        Before testing these endpoints, you **must** run the `Scripts.sql` file (located in the root of this project) against your SQL Server database. 
            
                        This script will create:
                        1. The `TestUsers` table with sample data.
                        2. The `vw_ActiveUsers` View.
                        3. The `tvf_GetUsersByTenant` Table-Valued Function.
                        4. The `usp_GetUserReport` Stored Procedure.
            
                        *(Note: The core `usp_QueryForge` engine procedures are automatically deployed by the library's background initialization service on startup, so you don't need to create them manually).*
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
                options.Approach = DapperExecutionApproach.DevelopAndUseSp;
                options.ConnectionFactory = sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    return new SqlConnection(config.GetConnectionString("DefaultConnection"));
                };
            });

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
}
