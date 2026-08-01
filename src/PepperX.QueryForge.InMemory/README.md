[![Part of PepperX Ecosystem](https://img.shields.io/badge/Part_of-PepperX_Ecosystem-512BD4?style=for-the-badge&logo=github&logoColor=white)](https://github.com/PepperX-Dev)

![PepperX.QueryForge Logo](https://raw.githubusercontent.com/PepperX-Dev/QueryForge/main/icon.png)

# PepperX.QueryForge.InMemory

[![NuGet Version](https://img.shields.io/nuget/v/PepperX.QueryForge.InMemory.svg?style=flat-square&label=PepperX.QueryForge.InMemory)](https://www.nuget.org/packages/PepperX.QueryForge.InMemory/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](../../LICENSE)

## Why this exists

Not every list lives in a table. Reference data you cached at startup, results you stitched together from three microservices, rows you just pulled out of a file — the moment a client wants to filter, sort, page and group *that*, you are back to writing the same plumbing by hand.

**PepperX.QueryForge.InMemory** takes the exact `Query` your database endpoints already accept and runs it against any `IEnumerable<T>`. Same input, same `QueryResult<T>` output, no database and no dependencies. And because it is the simplest possible implementation of QueryForge's semantics, it doubles as the reference the database providers are tested against.

## At a glance

- 🎯 **One `Query` in, one `QueryResult<T>` out** — identical to the Dapper and EF Core providers.
- 🌳 **Full hierarchical grouping** — multi-level `key / count / items` trees, same as everywhere else.
- 🧪 **A provider for your tests** — swap a database out without reshaping the calling code.
- 🔌 **Zero dependencies** — it needs nothing but the core package.
- 🗺️ **Works on more than POCOs** — dictionaries, dynamic rows, or models whose property names differ from your API's column names.

## Install

```bash
dotnet add package PepperX.QueryForge.InMemory
```

There is nothing to register. No services, no configuration.

```csharp
using PepperX.QueryForge.InMemory;
```

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

The frontend sends a plain `Query`. Because the source is a collection you chose in code, there is no table name for a client to spoof in the first place.

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
app.MapPost("/api/users/query", (Query clientQuery, IUserCache cache) =>
{
    clientQuery.Validate(rules =>
    {
        rules.Select(c => c.Deny("Email"));   // never leak this column, even if asked for
        rules.PageSize(p => p.Max(50));       // no data-dump attacks
    }, QueryValidationMode.SilentStrip);

    return cache.Users.ToQueryResult(clientQuery);
});
```

### B. Fully backend-built, no client input at all

```csharp
app.MapGet("/api/users/top-active", (IUserCache cache) =>
{
    var query = QueryBuilder
        .Where(new QueryCriteria(
            logic: Logic.And,
            groups: [ new ConditionGroup([ new Condition("IsActive", ConditionOperator.Equals, true) ]) ]))
        .Select("UserId", "FirstName", "LastName", "Country", "Score")
        .Sort(new SortDescriptor("Score", SortOrder.Descending))
        .Page(size: 10, number: 1)
        .Build();

    return cache.Users.ToQueryResult(query);
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
| `SilentStrip` | Quietly removes denied/disallowed columns and clamps paging to your limits. The request still succeeds. | Public APIs — never break the client over a permissions mismatch. |
| `ThrowException` | Throws a `QueryValidationException` listing every violated rule. | Internal APIs where an invalid request should fail loudly. |

```csharp
try
{
    clientQuery.Validate(rules => rules.Select(c => c.Deny("Email")), QueryValidationMode.ThrowException);
    return Results.Ok(cache.Users.ToQueryResult(clientQuery));
}
catch (QueryValidationException ex)
{
    // ex.InvalidProperties -> ["Email"]
    return Results.ValidationProblem(ex.InvalidProperties.ToDictionary(x => x, x => new[] { "Denied by security policy" }));
}
```

---

## Example 2 — Grouped results

Add `GroupByColumns` and the flat list turns into a nested tree, with row counts at every level.

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
app.MapPost("/api/users/grouped-query", (Query clientQuery, IUserCache cache) =>
{
    clientQuery.Validate(rules =>
    {
        rules.GroupBy(c => c.Allow("Country", "Department"));  // only these two levels are groupable
        rules.PageSize(p => p.Max(20));                         // caps top-level groups per page
    }, QueryValidationMode.SilentStrip);

    return cache.Users.ToQueryResult(clientQuery);
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

**Paging applies to the outermost group, not to rows.** `size: 5` means five countries, each carrying all of its rows — which is what makes every `count` a true total rather than a count of what fit on the page.

---

## Composable pieces

`ToQueryResult` is the whole thing. When you want the parts, each stage is its own extension and the sequence stays lazy:

```csharp
var page = users
    .ApplyFilter(query)        // Criteria
    .ApplySort(query)          // SortColumns
    .ApplyPaging(query)        // Paging
    .ApplyProjection(query);   // SelectColumns

// or all four at once, still lazy
var composed = users.ApplyQuery(query);
```

Grouping is deliberately not one of these — a hierarchy is a shape, not a sequence. Use `ToQueryResult` for grouped queries.

There is also an async wrapper, so an in-memory source can stand in for a database provider without reshaping the calling code:

```csharp
var result = await users.ToQueryResultAsync(query);
```

The work is synchronous; the method exists purely so the call site does not have to change.

## Sources that aren't POCOs

By default, columns are read as properties by name, case-insensitively. Pass your own accessor for anything else.

**Dictionary rows** — rows from a CSV, a document store, or a dynamic API:

```csharp
List<Dictionary<string, object?>> rows = LoadCsv();

var result = rows.ToQueryResult(query, InMemoryAccessors.ForDictionary<Dictionary<string, object?>>());
```

**Renamed columns** — when the names your API exposes are a contract, and shouldn't have to track how the model happens to be written:

```csharp
var accessor = InMemoryAccessors.WithColumnMap<TestUser>(new Dictionary<string, string>
{
    ["name"] = nameof(TestUser.FirstName),
    ["dept"] = nameof(TestUser.Department)
});

var result = users.ToQueryResult(query, accessor);
```

**Anything else** — the accessor is just a function:

```csharp
var result = rows.ToQueryResult(query, (row, column) => column switch
{
    "Total" => row.Price * row.Quantity,   // a computed column
    _       => row[column]
});
```

> With the default accessor, the model's properties act as a whitelist: a condition naming a column that does not exist is dropped, exactly as the SQL providers drop names missing from the table. When you supply your own accessor you own that decision, so every column name is taken at face value — validate explicitly if the names come from a client.

## Using it as a test double

This is the provider's other job. Because it satisfies the same contract as the database providers, a test can swap the storage out and keep everything else:

```csharp
public interface IUserQueries
{
    Task<QueryResult<TestUser>> QueryAsync(Query query);
}

// production
public sealed class SqlUserQueries(IDapperQueryService svc) : IUserQueries
{
    public Task<QueryResult<TestUser>> QueryAsync(Query query) =>
        svc.QueryAsync<TestUser>(DapperQueryBuilder.FromBase(query).ForObject("TestUsers").Build());
}

// tests
public sealed class FakeUserQueries(IEnumerable<TestUser> users) : IUserQueries
{
    public Task<QueryResult<TestUser>> QueryAsync(Query query) => users.ToQueryResultAsync(query);
}
```

Your assertions about filtering, paging and grouping then hold for the real thing too — a shared conformance suite in this repository runs the same 90 tests against all three providers on every build.

## Behaviour notes

**Unfilled filters are ignored, not treated as "match nothing".** A `GreaterThan` with no value is a filter the user did not fill in. `Equals` and `NotEquals` are the exceptions: a null value there is a deliberate IS NULL / IS NOT NULL test.

**Values are coerced before comparison.** A `"30"` arriving as JSON against an `int` column compares as the number 30, not as text — so `9` does not sort above `30`.

**Null follows SQL's three-valued logic.** A comparison against null is *unknown*, not false, and stays unknown under negation. So `AndNot` on `Country = 'Germany'` excludes rows whose country is null, matching what the database providers return.

**Text matching is case-insensitive.** There is no collation to follow here, so ordinal-ignore-case is used. On the database providers this follows the store's collation instead — case-insensitive on SQL Server by default, case-sensitive on PostgreSQL.

**Everything is materialized.** Filtering and sorting walk the sequence; this is the right tool for thousands of rows, not millions. For a large table, use the Dapper or EF Core provider and let the database do the work.

## Where things stand

### Execution providers

| Provider | Package | Status |
| :--- | :--- | :---: |
| Dapper | `PepperX.QueryForge.Dapper` | ✅ Released |
| Entity Framework Core | `PepperX.QueryForge.EFCore` | ✅ Released |
| In-Memory | `PepperX.QueryForge.InMemory` | ✅ Released |

The same `Query` produces the same `QueryResult<T>` on all three.

## 🤝 Contributing & License

This project is part of the [PepperX Ecosystem](https://github.com/PepperX-Dev).

Licensed under the **MIT License** — see [LICENSE](https://github.com/PepperX-Dev/QueryForge/blob/main/LICENSE).
