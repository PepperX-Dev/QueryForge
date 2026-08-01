[![Part of PepperX Ecosystem](https://img.shields.io/badge/Part_of-PepperX_Ecosystem-512BD4?style=for-the-badge&logo=github&logoColor=white)](https://github.com/PepperX-Dev)

![PepperX.QueryForge Logo](https://raw.githubusercontent.com/PepperX-Dev/QueryForge/main/icon.png)

# PepperX.QueryForge.EFCore

[![NuGet Version](https://img.shields.io/nuget/v/PepperX.QueryForge.EFCore.svg?style=flat-square&label=PepperX.QueryForge.EFCore)](https://www.nuget.org/packages/PepperX.QueryForge.EFCore/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](../../LICENSE)

## Why this exists

Tired of writing the same "list" endpoint for the hundredth time? A raw `SELECT *` dumped straight to the client, with no real filtering, no pagination, no security — just an unstructured blob, and every bit of that missing logic pushed onto the frontend to deal with?

**PepperX.QueryForge.EFCore** turns that into one call. Declare what data you want — filters, sorting, paging, grouping — and, optionally, the rules that keep it safe. You always get the data back through the same standardized, predictable contract.

The important detail is *how*: your `Query` becomes an **expression tree**, and EF Core generates the SQL. So it runs on every database EF Core supports, and your model keeps working — global query filters, value converters, owned types, inheritance mapping, table splitting. It is your `DbSet`, queried normally.

## At a glance

- 🎯 **One `Query` in, one `QueryResult<T>` out** — identical to the Dapper and In-Memory providers.
- 🗄️ **Every database EF Core supports** — SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Cosmos, and anything else with a provider.
- 🧩 **Composes with your `IQueryable`** — your `Where`, `Include` and `AsNoTracking` all survive.
- 🛡️ **Your entity is the whitelist** — a column that isn't a property never reaches the expression tree, let alone the database.
- 🌳 **Full hierarchical grouping** — multi-level `key / count / items` trees.
- ⚡ **Nothing to register** — no services, no configuration, no schema permissions.

## Install

```bash
dotnet add package PepperX.QueryForge.EFCore
```

```csharp
using PepperX.QueryForge.EFCore;
```

That is the whole setup. There is no `AddQueryForge...` call.

The model used in every example below:

```csharp
public class TestUser
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public bool IsActive { get; set; }
}
```

---

## Example 1 — Flat results

A `Query` has 5 things you can control: `Criteria`, `Paging`, `SelectColumns`, `SortColumns` and `GroupByColumns`. This example touches every one except grouping (that's Example 2).

### A. Client-driven, with validation

The frontend sends a plain `Query` — notice it names no table. The `DbSet` you call this on is the target, chosen server-side, so it is physically impossible for a client to redirect the query somewhere else.

```json
POST /api/users/query
{
  "criteria": {
    "logic": 0,                    // Logic.And -> combine the groups below with AND
    "groups": [
      {
        "logic": 0,                // Logic.And -> Country = 'Germany' AND IsActive = true
        "conditions": [
          { "columnName": "Country", "operator": 0, "value": "Germany" },  // operator 0 = Equals
          { "columnName": "IsActive", "operator": 0, "value": true }
        ]
      }
    ]
  },
  "paging": { "size": 5, "number": 1 },
  "selectColumns": ["UserId", "FirstName", "LastName", "Country", "Department", "Score"],
  "sortColumns": [
    { "columnName": "Score", "sortOrder": 1 }   // sortOrder 1 = Descending
  ]
}
```

> `Logic`: `0`=And, `1`=Or, `2`=AndNot, `3`=OrNot &nbsp;|&nbsp; `ConditionOperator` `0`=Equals &nbsp;|&nbsp; `SortOrder`: `0`=Ascending, `1`=Descending

```csharp
app.MapPost("/api/users/query", async (Query clientQuery, AppDbContext db) =>
{
    clientQuery.Validate(rules =>
    {
        rules.Select(c => c.Deny("Email"));   // never leak this column, even if asked for
        rules.PageSize(p => p.Max(50));       // no data-dump attacks
    }, QueryValidationMode.SilentStrip);

    return await db.Users.ToQueryResultAsync<TestUser>(clientQuery);
});
```

### B. Fully backend-built, no client input at all

```csharp
app.MapGet("/api/users/top-active", async (AppDbContext db) =>
{
    var query = QueryBuilder
        .Where(new QueryCriteria(
            logic: Logic.And,
            groups: [ new ConditionGroup([ new Condition("IsActive", ConditionOperator.Equals, true) ]) ]))
        .Select("UserId", "FirstName", "LastName", "Country", "Score")
        .Sort(new SortDescriptor("Score", SortOrder.Descending))
        .Page(size: 10, number: 1)
        .Build();

    return await db.Users.ToQueryResultAsync<TestUser>(query);
});
```

### The result — always the same shape

```json
{
  "meta": { "total": { "rows": 4, "pages": 1 }, "type": "Flat" },
  "models": [
    { "userId": 16, "firstName": "First16", "lastName": "Last16", "country": "Germany", "department": "IT", "score": 66.00 },
    { "userId": 11, "firstName": "First11", "lastName": "Last11", "country": "Germany", "department": "Marketing", "score": 61.00 },
    { "userId": 6,  "firstName": "First6",  "lastName": "Last6",  "country": "Germany", "department": "Sales", "score": 56.00 },
    { "userId": 1,  "firstName": "First1",  "lastName": "Last1",  "country": "Germany", "department": "HR", "score": 51.00 }
  ]
}
```

### Validation: two modes, your choice

`Validate()` always takes a `QueryValidationMode`:

| Mode | Behavior | Best for |
| :--- | :--- | :--- |
| `SilentStrip` *(used above)* | Quietly removes denied/disallowed columns and clamps paging to your limits. The request still succeeds. | Public APIs — never break the client over a permissions mismatch. |
| `ThrowException` | Throws a `QueryValidationException` listing every violated rule the moment one is found. | Internal APIs / strict environments where an invalid request should fail loudly. |

```csharp
try
{
    clientQuery.Validate(rules => rules.Select(c => c.Deny("Email")), QueryValidationMode.ThrowException);
    return Results.Ok(await db.Users.ToQueryResultAsync<TestUser>(clientQuery));
}
catch (QueryValidationException ex)
{
    // ex.InvalidProperties -> ["Email"]
    return Results.ValidationProblem(ex.InvalidProperties.ToDictionary(x => x, x => new[] { "Denied by security policy" }));
}
```

---

## Example 2 — Grouped results

Add `GroupByColumns` and the flat table turns into a nested tree — with row counts at every level.

### A. Client-driven, with validation

```json
POST /api/users/grouped-query
{
  "criteria": {
    "groups": [
      { "conditions": [ { "columnName": "IsActive", "operator": 0, "value": true } ] }
    ]
  },
  "paging": { "size": 5, "number": 1 },
  "selectColumns": ["UserId", "FirstName", "LastName", "Score"],
  "sortColumns": [ { "columnName": "Score", "sortOrder": 1 } ],
  "groupByColumns": [
    { "columnName": "Country", "sortOrder": 0 },
    { "columnName": "Department", "sortOrder": 0 }
  ]
}
```

```csharp
app.MapPost("/api/users/grouped-query", async (Query clientQuery, AppDbContext db) =>
{
    clientQuery.Validate(rules =>
    {
        rules.GroupBy(c => c.Allow("Country", "Department"));  // only these two levels are groupable
        rules.PageSize(p => p.Max(20));                         // caps top-level groups per page
    }, QueryValidationMode.SilentStrip);

    return await db.Users.ToQueryResultAsync<TestUser>(clientQuery);
});
```

### B. Fully backend-built

```csharp
app.MapGet("/api/users/by-country", async (AppDbContext db) =>
{
    var query = QueryBuilder
        .Select("UserId", "FirstName", "LastName", "Score")
        .Sort(new SortDescriptor("Score", SortOrder.Descending))
        .GroupBy(
            new GroupByDescriptor("Country", SortOrder.Ascending),
            new GroupByDescriptor("Department", SortOrder.Ascending))
        .Page(5, 1)
        .Build();

    return await db.Users.ToQueryResultAsync<TestUser>(query);
});
```

### The result — a real hierarchy

```json
{
  "meta": { "total": { "rows": 4, "pages": 1 }, "type": "Grouped" },
  "groups": [
    {
      "key": "Canada",
      "count": 9,
      "subGroups": [
        { "key": "HR", "count": 5, "items": [ { "userId": 9, "firstName": "First9", "lastName": "Last9", "score": 59.00 } ] },
        { "key": "IT", "count": 4, "items": [ { "userId": 4, "firstName": "First4", "lastName": "Last4", "score": 54.00 } ] }
      ]
    },
    {
      "key": "Germany",
      "count": 12,
      "subGroups": [
        { "key": "IT",        "count": 4, "items": [ { "userId": 16, "firstName": "First16", "lastName": "Last16", "score": 66.00 } ] },
        { "key": "Marketing", "count": 3, "items": [ { "userId": 11, "firstName": "First11", "lastName": "Last11", "score": 61.00 } ] }
      ]
    }
  ]
}
```

### How a grouped query executes

Two things are worth knowing, because they explain both the shape and the cost:

- **Paging applies to the outermost group, not to rows.** `size: 5` means five countries, and each one carries *all* of its rows. That is what makes every `count` a true total rather than a count of whatever fit on the page.
- **Keys are paged in the database; the nesting is assembled in memory.** Three statements run: one `COUNT` over the distinct outer keys, one to fetch the page of keys, and one to fetch the rows belonging to them. Only those rows come back. Deeply grouping a very large filtered set is the one case worth measuring.

---

## Composing with what you already have

The extensions hang off `IQueryable<T>`, so anything you applied first survives — which is exactly what you want for the restrictions that matter:

```csharp
var result = await db.Users
    .Where(u => u.TenantId == currentTenant)   // your rule; the client cannot query it away
    .Include(u => u.Department)
    .AsNoTracking()
    .ToQueryResultAsync<TestUser>(clientQuery);
```

Need the stages separately — to count differently, or to project into a DTO?

```csharp
var page = db.Users
    .ApplyFilter(query)        // WHERE
    .ApplySort(query)          // ORDER BY
    .ApplyPaging(query)        // OFFSET / FETCH
    .ApplyProjection(query);   // narrowed SELECT list

// or all four at once, still unexecuted
var composed = db.Users.ApplyQuery(query);

var names = await composed.Select(u => u.FirstName).ToListAsync();
```

Grouping is deliberately not one of these — a hierarchy is a shape, not a queryable. Use `ToQueryResultAsync` for grouped queries.

## Safety

**Your entity is the whitelist.** A condition, sort, projection or grouping naming anything that is not a readable property of your entity is **dropped**. It never becomes part of the expression tree, so nothing reaches the database to be rejected — and a filter payload cannot be used to discover columns you did not offer.

**Values become SQL parameters, never inlined literals.** They are injected as captured variables, so the statement text stays identical across calls that differ only in their values and the database can reuse a cached plan.

**`SelectColumns` narrows the SELECT list.** Unselected columns are not fetched, and the resulting instances are untracked — a partially-populated entity is not something the change tracker should ever write back.

On top of all that, the core validation engine gives you explicit allow/deny lists and page-size clamps, as shown above.

## Behaviour notes

**Unfilled filters are ignored, not treated as "match nothing".** A `GreaterThan` with no value is a filter the user did not fill in. `Equals` and `NotEquals` are the exceptions: a null value there is a deliberate `IS NULL` / `IS NOT NULL` test.

**Values are coerced to the column's type.** A `"30"` arriving as JSON against an `int` column compares as the number 30, not as text. A value that cannot be coerced at all — `"abc"` for an `int` — drops that condition rather than throwing.

**Null follows SQL's three-valued logic.** A comparison against null is *unknown*, not false, and stays unknown under negation. `AndNot` on `Country = 'Germany'` therefore excludes rows whose country is null. QueryForge builds negated groups so this holds even though EF Core's default null compensation would otherwise let those rows through — which is what keeps this provider's answers identical to the Dapper provider's.

**Text comparison follows your database's collation.** `Contains` on SQL Server with a default collation is case-insensitive; on PostgreSQL or SQLite it is case-sensitive. QueryForge does not override this, because forcing a case fold would stop your indexes being used.

**A null column never matches a text predicate**, `NotContains` included — again, the result SQL gives.

**Text operators only apply to text columns.** A `Contains` aimed at an `int` is dropped rather than forcing a client-side `ToString()` that EF Core could not translate.

## Built for enterprise data grids

QueryForge is architecturally purpose-built to be the backend counterpart for advanced UI data grids like **DevExtreme (`dxDataGrid`)**, **AG Grid**, and **Kendo UI**.

| Grid feature | QueryForge capability |
| :--- | :--- |
| Complex filtering | Deeply nested `AND` / `OR` / `AND NOT` / `OR NOT` groups (`Criteria`) |
| Multi-level grouping | Native `key / count / items` hierarchy trees, any number of levels |
| Server-side paging | Accurate row/page totals, on flat results or on top-level groups |
| Dynamic multi-column sorting | `SortColumns`, independent direction per column |

## Where things stand

### Execution providers

| Provider | Package | Status |
| :--- | :--- | :---: |
| Dapper | `PepperX.QueryForge.Dapper` | ✅ Released |
| Entity Framework Core | `PepperX.QueryForge.EFCore` | ✅ Released |
| In-Memory | `PepperX.QueryForge.InMemory` | ✅ Released |

### Database engine support

Every database with an EF Core provider, because EF Core generates the SQL — SQL Server, PostgreSQL, MySQL/MariaDB, SQLite, Oracle, Cosmos, and others.

The same `Query` produces the same `QueryResult<T>` on all three providers; a shared conformance suite runs the same 90 tests against each on every build.

## 🤝 Contributing & License

This project is part of the [PepperX Ecosystem](https://github.com/PepperX-Dev).

Licensed under the **MIT License** — see [LICENSE](https://github.com/PepperX-Dev/QueryForge/blob/main/LICENSE).
