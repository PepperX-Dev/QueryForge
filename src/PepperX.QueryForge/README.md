[![Part of PepperX Ecosystem](https://img.shields.io/badge/Part_of-PepperX_Ecosystem-512BD4?style=for-the-badge&logo=github&logoColor=white)](https://github.com/amirhosseinmp02/PepperX)

![PepperX.QueryForge Logo](https://raw.githubusercontent.com/amirhosseinmp02/PepperX/main/icon.png)

# PepperX.QueryForge

[![NuGet Version](https://img.shields.io/nuget/v/PepperX.QueryForge.svg?style=flat-square&label=PepperX.QueryForge)](https://www.nuget.org/packages/PepperX.QueryForge/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](../../LICENSE)

## What this is

**PepperX.QueryForge** is the abstract core of the QueryForge ecosystem: the shared vocabulary for describing *"filter this, sort it like that, page it, maybe group it"* — as a plain C# object or a plain JSON payload — without tying that intent to any particular database or library.

It has **no execution logic**. It doesn't run SQL. It just defines the models, a fluent builder, and a validation engine that every provider (Dapper today, EF Core and In-Memory next) builds on top of.

> To actually run a `Query` against a database, install a provider — e.g. [`PepperX.QueryForge.Dapper`](https://www.nuget.org/packages/PepperX.QueryForge.Dapper). It's included automatically as a dependency.

## At a glance

- 🧱 **One shape, `Query`** — filters, sorting, paging, and grouping, whether it comes from C# or from a client as JSON.
- 🔍 **Nested filter logic** — `AND` / `OR` / `AND NOT` / `OR NOT` groups, any depth.
- 🌳 **Grouping models** — describes multi-level `key / count / items` hierarchies for providers to build.
- 🛡️ **Validation engine** — silently strip or hard-block invalid columns/paging, shared by every provider.
- 🔌 **Provider-agnostic** — write the intent once, plug in whichever execution engine you need.

## Install

```bash
dotnet add package PepperX.QueryForge
```

(You'll rarely install this directly — it comes along with whichever provider you pick.)

## The `Query` object

```csharp
public class Query
{
    public QueryCriteria Criteria { get; set; }                       // filters
    public QueryPaging Paging { get; set; }                           // { Size, Number }
    public IReadOnlyList<string> SelectColumns { get; set; }          // projection
    public IReadOnlyList<SortDescriptor> SortColumns { get; set; }    // ORDER BY
    public IReadOnlyList<GroupByDescriptor> GroupByColumns { get; set; } // GROUP BY / hierarchy
}
```

Build one by hand, deserialize it straight from a client's JSON body, or use the fluent builder:

```csharp
var query = QueryBuilder.New()
    .Select("Id", "Name", "Country")
    .Sort(new SortDescriptor("Name"))
    .Page(size: 20, number: 1)
    .Build();
```

## Filtering

`Criteria` is a tree: groups of conditions, joined by a `Logic`, where each group is itself joined to the others by a `Logic`.

```csharp
var criteria = new QueryCriteria(
    logic: Logic.And,
    groups:
    [
        new ConditionGroup(
            logic: Logic.Or,
            conditions:
            [
                new Condition("Country", ConditionOperator.Equals, "USA"),
                new Condition("Country", ConditionOperator.Equals, "Germany")
            ])
    ]);

var query = QueryBuilder.Where(criteria).Build();
```

Equivalent JSON from a client:

```json
{
  "criteria": {
    "logic": 0,                  // Logic.And -> combine the groups below with AND (only one group here)
    "groups": [
      {
        "logic": 1,              // Logic.Or  -> Country = 'USA' OR Country = 'Germany'
        "conditions": [
          { "columnName": "Country", "operator": 0, "value": "USA" },      // operator 0 = Equals
          { "columnName": "Country", "operator": 0, "value": "Germany" }   // operator 0 = Equals
        ]
      }
    ]
  }
}
```

> `Logic`: `0`=And, `1`=Or, `2`=AndNot, `3`=OrNot

`ConditionOperator` covers the usual set: `Equals`, `NotEquals`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `LessThan`, `GreaterThan`, `LessThanOrEqualTo`, `GreaterThanOrEqualTo`, `Between`.

## Grouping

Add `GroupByColumns` and a provider turns a flat result into a nested tree instead — this library just defines that shape (`HierarchyNode<T>`: `Key`, `Count`, `SubGroups`, `Items`), a provider does the actual grouping:

```csharp
var query = QueryBuilder.New()
    .GroupBy(new GroupByDescriptor("Country"), new GroupByDescriptor("Department"))
    .Page(5, 1) // paginates the top-level groups
    .Build();
```

## Validation

Never trust a `Query` that came from outside your API. `Validate()` supports two modes:

| Mode | Behavior |
| :--- | :--- |
| `SilentStrip` | Quietly removes denied/disallowed columns and clamps paging — the request still succeeds. |
| `ThrowException` | Throws a `QueryValidationException` listing every violated rule. |

```csharp
query.Validate(rules =>
{
    rules.Select(c => c.Deny("PasswordHash", "SecretKey"));
    rules.PageSize(p => p.Max(100));
}, QueryValidationMode.SilentStrip);
```

## The result contract

Every provider returns the same `QueryResult<TModel>`, whichever mode you used:

```csharp
public record QueryResult<TModel>
{
    public QueryResultMeta Meta { get; init; }                     // { Total: { Rows, Pages }, Type }
    public IReadOnlyList<TModel> Models { get; init; }             // populated when Type == Flat
    public IReadOnlyList<HierarchyNode<TModel>> Groups { get; init; } // populated when Type == Grouped
}
```

## Providers built on this core

| Provider | Package | Status |
| :--- | :--- | :---: |
| Dapper (SQL Server) | [`PepperX.QueryForge.Dapper`](https://www.nuget.org/packages/PepperX.QueryForge.Dapper) | ✅ Released |
| Entity Framework Core | `PepperX.QueryForge.EFCore` | 🔧 In Development |
| In-Memory | `PepperX.QueryForge.InMemory` | 📋 Planned |

## 🤝 Contributing & License

This project is part of the [PepperX Ecosystem](https://github.com/amirhosseinmp02/PepperX).
Licensed under the MIT License.