![PepperX.QueryForge Logo](https://raw.githubusercontent.com/amirhosseinmp02/PepperX/main/icon.png)

# PepperX.QueryForge

[![NuGet Version](https://img.shields.io/nuget/v/PepperX.QueryForge.Dapper.svg?style=flat-square&label=PepperX.QueryForge.Dapper)](https://www.nuget.org/packages/PepperX.QueryForge.Dapper/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)

**PepperX.QueryForge** is a lightweight, provider-agnostic, dynamic query building and execution engine for .NET. Build complex, paginated, and hierarchically grouped queries using a fluent C# API or accept them as JSON payloads from any frontend — then execute them seamlessly across multiple database providers.

---

## 📑 Table of Contents

- [✨ Features](#-features)
- [🖥️ Frontend Grid Compatibility](#️-frontend-grid-compatibility)
- [🧩 Provider Ecosystem](#-provider-ecosystem)
- [📦 Installation](#-installation)
- [🚀 Quick Start](#-quick-start)
- [🏗️ Core Concepts](#️-core-concepts)
- [🔍 Advanced Filtering](#-advanced-filtering)
- [🌳 Hierarchical Grouping](#-hierarchical-grouping)
- [🛡️ Security & Validation](#️-security--validation)
- [🗄️ Database Objects (Views, TVFs, SPs)](#️-database-objects-views-tvfs-sps)
- [⚙️ Configuration & Execution Approaches](#️-configuration--execution-approaches)
- [📚 Enum Reference](#-enum-reference)

---

## ✨ Features

- **Provider-Agnostic Core:** Define query intent once. Execute anywhere — Dapper today, EF Core and InMemory coming soon.
- **Native Hierarchical Grouping:** Generate deeply nested `key/count/items` JSON trees directly from the database engine.
- **Bulletproof Security:** Built-in `SilentStrip` validation prevents schema enumeration, data dumps, and malicious column requests.
- **Zero Boilerplate:** Beautiful Fluent API for building queries in C# or accepting them as raw JSON from frontends.
- **Automated Schema Deployment:** Background `IHostedService` automatically deploys required Stored Procedures on app startup.
- **Native AOT Compatible:** Built on `System.Text.Json` and Dapper — no heavy reflection-based ORM overhead.

---

## 🖥️ Frontend Grid Compatibility

QueryForge is architecturally purpose-built to be the ultimate backend counterpart for advanced enterprise UI data grids like **DevExtreme (`dxDataGrid`)**, **AG Grid**, and **Kendo UI**.

While these grids send complex `loadOptions` (nested filters, multi-level grouping, summaries) that require a lightweight frontend mapper to translate into QueryForge's `Query` JSON structure, QueryForge natively handles the exact heavy-lifting SQL execution these grids demand:

| Grid Feature | QueryForge Backend Capability |
| :--- | :--- |
| **Complex Filtering** | Supports deeply nested `AND` / `OR` / `AND NOT` logic groups. |
| **Multi-Level Grouping** | Natively generates hierarchical `key/count/items` trees with aggregated counts at every level. |
| **Server-Side Paging** | Accurately paginates top-level groups or flat rows with total row/page counts. |
| **Dynamic Sorting** | Supports multi-column sorting with directional (`ASC`/`DESC`) control. |

---

## 🧩 Provider Ecosystem

QueryForge is designed as a modular ecosystem. Install only the provider you need — the core library is included automatically as a dependency.

### Execution Providers

| Provider | Package | Status |
| :--- | :--- | :--- |
| **Dapper** | `PepperX.QueryForge.Dapper` | ✅ Released |
| **Entity Framework Core** | `PepperX.QueryForge.EFCore` | 🔧 In Development |
| **In-Memory** | `PepperX.QueryForge.InMemory` | 📋 Planned |

### Database Engine Support (via Dapper Provider)

| Database Engine | Status |
| :--- | :--- |
| **Microsoft SQL Server** | ✅ Fully Supported |
| **MySQL / MariaDB** | 📋 Planned |
| **PostgreSQL** | 📋 Planned |
| **Oracle** | 📋 Planned |

> **Legend:** ✅ Released &nbsp;|&nbsp; 🔧 In Development &nbsp;|&nbsp; 📋 Planned

---

## 📦 Installation

Install the provider package for your execution engine. The core `PepperX.QueryForge` library is automatically included.

```bash
dotnet add package PepperX.QueryForge.Dapper
```

---

## 🚀 Quick Start

### 1. Register in `Program.cs`

```csharp
using PepperX.QueryForge.Dapper;
using Microsoft.Data.SqlClient;

builder.Services.AddQueryForgeDapper(options =>
{
    options.Approach = DapperExecutionApproach.DevelopAndUseSp;
    
    options.ConnectionFactory = sp => 
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new SqlConnection(config.GetConnectionString("DefaultConnection"));
    };
});
```

### 2. Build and Execute

**Define your Model first**
```csharp
public record User(
    int Id,
    string Name,
    string Email,
    string Country,
    bool IsActive
);
```

**Simple Query (HTTP GET)**
Perfect for simple paging and sorting. Uses `[AsParameters]` to bind from the query string.

```csharp
// GET /api/users?Paging.Size=10&Paging.Number=1&SortColumns[0].ColumnName=Score
app.MapGet("/api/users", async ([AsParameters] Query baseQuery, IDapperQueryService queryService) =>
{
    var dapperQuery = DapperQueryBuilder
        .FromBase(baseQuery)
        .ForObject("Users", "dbo", DapperObjectType.Table)
        .Build();

    return await queryService.QueryAsync<User>(dapperQuery);
});
```

**Complex Query (HTTP POST)**
> 💡 **Pro-Tip:** For complex filtering involving nested `Criteria` groups, `HTTP POST` is the industry standard for CQRS read-models. It avoids URL length limits and complex query-string binding issues.

```csharp
// POST /api/users/query
app.MapPost("/api/users/query", async (Query baseQuery, IDapperQueryService queryService) =>
{
    var dapperQuery = DapperQueryBuilder
        .FromBase(baseQuery)
        .ForObject("Users", "dbo", DapperObjectType.Table)
        .Build();

    return await queryService.QueryAsync<User>(dapperQuery);
});
```

---

## 🏗️ Core Concepts

### The Base `Query` vs. Provider `DapperQuery`

To prevent frontends from spoofing target tables (e.g., sending `"object": {"name": "AdminPasswords"}`), your API endpoints should accept the base `PepperX.QueryForge.Query` class. The backend then securely upgrades it to a `DapperQuery`.

```csharp
// Frontend sends this JSON (Notice: No 'object' property — it's physically impossible to spoof)
// { "paging": { "size": 10 }, "sortColumns": [{"columnName": "Score", "sortOrder": 1}] }

var dapperQuery = DapperQueryBuilder
    .FromBase(baseQuery)             // Maps frontend criteria, paging, and sorting
    .ForObject("Users", "dbo")       // Backend hardcodes the secure target
    .Build();
```

---

## 🔍 Advanced Filtering

QueryForge supports deeply nested logical groups (`AND`, `OR`, `AND NOT`, `OR NOT`) and a comprehensive set of comparison operators.

### C# Builder Syntax

```csharp
var criteria = new QueryCriteria(
    logic: Logic.And,
    groups: [
        new ConditionGroup(
            logic: Logic.Or,
            conditions: [
                new Condition("Country", ConditionOperator.Equals, "USA"),
                new Condition("Country", ConditionOperator.Equals, "Germany")
            ]
        ),
        new ConditionGroup(
            logic: Logic.AndNot,
            conditions: [
                new Condition("Role", ConditionOperator.Equals, "Guest"),
                new Condition("Age", ConditionOperator.LessThan, 21)
            ]
        )
    ]
);

var query = DapperQueryBuilder.New()
    .ForObject("Users")
    .Where(criteria)
    .Build();
```

### Equivalent JSON Payload (from Frontend)

```json
{
  "criteria": {
    "logic": 0,
    "groups": [
      {
        "logic": 1,
        "conditions": [
          { "columnName": "Country", "operator": 0, "value": "USA" },
          { "columnName": "Country", "operator": 0, "value": "Germany" }
        ]
      },
      {
        "logic": 2,
        "conditions": [
          { "columnName": "Role", "operator": 0, "value": "Guest" },
          { "columnName": "Age", "operator": 6, "value": 21 }
        ]
      }
    ]
  }
}
```

### Generated SQL

```sql
WHERE (Country = N'USA' OR Country = N'Germany') 
  AND NOT (Role = N'Guest' AND Age < N'21')
```

### Null Checks & Ranges

```json
{
  "criteria": {
    "groups": [
      {
        "conditions": [
          { "columnName": "DeletedAt", "operator": 0, "value": null },
          { "columnName": "Score", "operator": 10, "value": 75.5, "valueTo": 95.0 }
        ]
      }
    ]
  }
}
```

*Generates:* `WHERE DeletedAt IS NULL AND Score BETWEEN N'75.5' AND N'95.0'`

---

## 🌳 Hierarchical Grouping

By specifying `GroupByColumns`, the engine transforms a flat tabular result into a deeply nested hierarchical tree, complete with aggregated counts at every level.

```csharp
var query = DapperQueryBuilder.New()
    .ForObject("Users")
    .GroupBy(
        new GroupByDescriptor("Country", SortOrder.Ascending),
        new GroupByDescriptor("Department", SortOrder.Ascending)
    )
    .Page(5, 1) // Paginates the TOP-LEVEL groups (Countries)
    .Build();

var result = await queryService.QueryAsync<User>(query);
```

### Resulting JSON Structure

```json
{
  "meta": { "total": { "rows": 2, "pages": 1 }, "type": "Grouped" },
  "groups": [
    {
      "key": "USA",
      "count": 45,
      "subGroups": [
        {
          "key": "IT",
          "count": 20,
          "items": [ { "userId": 1, "name": "Alice" }, { "userId": 2, "name": "Bob" } ]
        },
        {
          "key": "HR",
          "count": 25,
          "items": [ { "userId": 3, "name": "Charlie" } ]
        }
      ]
    },
    {
      "key": "UK",
      "count": 30,
      "subGroups": [
        {
          "key": "Sales",
          "count": 30,
          "items": [ ... ]
        }
      ]
    }
  ]
}
```

---

## 🛡️ Security & Validation

Never trust frontend query payloads. QueryForge includes a powerful validation engine that can either silently sanitize malicious requests or throw strict errors.

### Silent Strip (Recommended for Public APIs)

Silently removes denied columns and clamps pagination limits without throwing errors.

```csharp
dapperQuery.Validate(rules =>
{
    // Dapper-specific: Restrict which tables and schemas can be queried
    rules.Object(o => o.AllowSchema("dbo").AllowTable("Users", "Products"));
    
    // Base: Prevent sensitive column leakage
    rules.Select(c => c.Deny("PasswordHash", "SecretKey", "DeletedAt"));
    
    // Base: Prevent data-dump attacks
    rules.PageSize(p => p.Max(100));
}, QueryValidationMode.SilentStrip);
```

### Strict Validation (Recommended for Internal APIs)

Throws a `QueryValidationException` containing a list of all violated rules.

```csharp
try 
{
    dapperQuery.Validate(rules => rules.Select(c => c.Deny("PasswordHash")), QueryValidationMode.ThrowException);
}
catch (QueryValidationException ex)
{
    // ex.InvalidProperties contains ["PasswordHash"]
    return Results.ValidationProblem(
        ex.InvalidProperties.ToDictionary(x => x, x => new[] { "Denied by security policy" }));
}
```

---

## 🗄️ Database Objects (Views, TVFs, SPs)

QueryForge is not limited to tables. You can seamlessly query Views, Table-Valued Functions, and Stored Procedures — and still apply filters, sorting, and pagination on top of their results.

### Table-Valued Function (TVF)

```csharp
var query = DapperQueryBuilder.New()
    .ForObject("tvf_GetUsersByTenant", "dbo", DapperObjectType.TVF, new Dictionary<string, object?> 
    { 
        { "TenantId", 1 } 
    })
    .Build();
```

### Stored Procedure

```csharp
var query = DapperQueryBuilder.New()
    .ForObject("usp_GetUserReport", "dbo", DapperObjectType.SP, new Dictionary<string, object?> 
    { 
        { "IncludeDeleted", false } 
    })
    .Where(new QueryCriteria(
        groups: [new ConditionGroup([new Condition("Age", ConditionOperator.GreaterThan, 30)])]
    ))
    .Page(20, 1)
    .Build();
```

---

## ⚙️ Configuration & Execution Approaches

Configure how QueryForge interacts with your database schema via `ExecutionApproach`:

| Approach | Description | Best For |
| :--- | :--- | :--- |
| **`DevelopAndUseSp`** *(Default)* | Automatically creates/alters the QueryForge Stored Procedures on app startup via a background `IHostedService`. Requires DDL permissions (e.g., `db_ddladmin`). | Local Dev, CI/CD, Rapid Prototyping |
| **`UseReadySp`** | Assumes the DBA has already deployed the Stored Procedures. Skips initialization entirely. | Strict Production Environments |
| **`RawQuery`** | Generates and executes raw dynamic SQL directly. Does not require SP creation permissions. | Environments with restricted DDL access |

---

## 📚 Enum Reference

When sending JSON payloads from the frontend, enums are serialized as **integers** by default.

### `ConditionOperator`

| Value | Name | SQL Equivalent |
| :---: | :--- | :--- |
| `0` | `Equals` | `= @val` (or `IS NULL` if value is null) |
| `1` | `NotEquals` | `<> @val` (or `IS NOT NULL` if value is null) |
| `2` | `Contains` | `LIKE '%@val%'` |
| `3` | `NotContains` | `NOT LIKE '%@val%'` |
| `4` | `StartsWith` | `LIKE '@val%'` |
| `5` | `EndsWith` | `LIKE '%@val'` |
| `6` | `LessThan` | `< @val` |
| `7` | `GreaterThan` | `> @val` |
| `8` | `LessThanOrEqualTo` | `<= @val` |
| `9` | `GreaterThanOrEqualTo` | `>= @val` |
| `10` | `Between` | `BETWEEN @val AND @valTo` |

### `Logic`

| Value | Name | SQL Equivalent |
| :---: | :--- | :--- |
| `0` | `And` | `(...) AND (...)` |
| `1` | `Or` | `(...) OR (...)` |
| `2` | `AndNot` | `(...) AND NOT (...)` |
| `3` | `OrNot` | `(...) OR NOT (...)` |

### `SortOrder`

| Value | Name | SQL Equivalent |
| :---: | :--- | :--- |
| `0` | `Ascending` | `ORDER BY col ASC` |
| `1` | `Descending` | `ORDER BY col DESC` |

### `ObjectType`

| Value | Name | Description |
| :---: | :--- | :--- |
| `0` | `Auto` | Automatically detects Table, View, TVF, or SP from `sys.objects` |
| `1` | `Table` | Standard database table (`U`) |
| `2` | `View` | Database view (`V`) |
| `3` | `TVF` | Table-Valued Function (`IF`, `TF`, `FT`) |
| `4` | `SP` | Stored Procedure (`P`) |

### `QueryResultType` (Response Only)

| Value | Name | Description |
| :---: | :--- | :--- |
| `0` | `Flat` | Standard flat array of models |
| `1` | `Grouped` | Nested `key/count/items` hierarchy tree |

---

## 🤝 Contributing & License

Contributions, issues, and feature requests are welcome!

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.