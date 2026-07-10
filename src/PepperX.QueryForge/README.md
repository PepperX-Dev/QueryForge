[![Part of PepperX Ecosystem](https://img.shields.io/badge/Part_of-PepperX_Ecosystem-512BD4?style=for-the-badge&logo=github&logoColor=white)](https://github.com/amirhosseinmp02/PepperX)

![PepperX.QueryForge Logo](https://raw.githubusercontent.com/amirhosseinmp02/PepperX/main/icon.png)

# PepperX.QueryForge

[![NuGet Version](https://img.shields.io/nuget/v/PepperX.QueryForge.svg?style=flat-square&label=PepperX.QueryForge)](https://www.nuget.org/packages/PepperX.QueryForge/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)

**PepperX.QueryForge** is the **abstract, provider-agnostic foundation** of the QueryForge ecosystem. It provides the core models, fluent builders, and bulletproof validation engines required to construct dynamic, paginated, and hierarchically grouped queries.

> ⚠️ **Important:** This package defines the *intent* and *structure* of the query but does not execute it. To execute queries against a database, you must install an execution provider like [`PepperX.QueryForge.Dapper`](https://www.nuget.org/packages/PepperX.QueryForge.Dapper).

---

## 📑 Table of Contents

- [✨ Core Features](#-core-features)
- [🧩 The Provider Ecosystem](#-the-provider-ecosystem)
- [🏗️ Core Concepts](#️-core-concepts)
- [🔍 Advanced Filtering](#-advanced-filtering)
- [🌳 Hierarchical Grouping Models](#-hierarchical-grouping-models)
- [🛡️ Security & Validation Engine](#️-security--validation-engine)
- [📚 Enum Reference](#-enum-reference)

---

## ✨ Core Features

- **Abstract & Provider-Agnostic:** Define your query intent once in C# or JSON. Execute it anywhere by plugging in a provider (Dapper, EF Core, InMemory).
- **Bulletproof Security:** Built-in `SilentStrip` validation engine prevents schema enumeration, data dumps, and malicious column requests before the query ever reaches the database.
- **Zero Boilerplate:** Beautiful Fluent API for building queries in C#, or accept them as raw JSON payloads directly from frontends.
- **Complex Logic Trees:** Natively supports deeply nested `AND` / `OR` / `AND NOT` logic groups.

---

## 🧩 The Provider Ecosystem

This core library is automatically included as a dependency when you install an execution provider.

| Provider | Package | Status |
| :--- | :--- | :--- |
| **Dapper (SQL Server)** | [`PepperX.QueryForge.Dapper`](https://www.nuget.org/packages/PepperX.QueryForge.Dapper) | ✅ Released |
| **Entity Framework Core** | `PepperX.QueryForge.EFCore` | 🔧 In Development |
| **In-Memory (Testing)** | `PepperX.QueryForge.InMemory` | 📋 Planned |

---

## 🏗️ Core Concepts

### The Base `Query` vs. Provider Queries

To prevent frontends from spoofing target tables (e.g., sending `"object": {"name": "AdminPasswords"}`), your API endpoints should accept the base `PepperX.QueryForge.Query` class. The backend then securely upgrades it to a provider-specific query (like `DapperQuery`).

```csharp
// Frontend sends this JSON (Notice: No 'object' property — it's physically impossible to spoof)
// { "paging": { "size": 10 }, "sortColumns": [{"columnName": "Score", "sortOrder": 1}] }

// The backend maps the frontend criteria, paging, and sorting securely.
```

---

## 🔍 Advanced Filtering

QueryForge supports deeply nested logical groups and a comprehensive set of comparison operators.

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

---

## 🌳 Hierarchical Grouping Models

By specifying `GroupByColumns`, the core models define how the execution provider should transform a flat tabular result into a deeply nested hierarchical tree.

```csharp
var query = QueryBuilder.New()
    .GroupBy(
        new GroupByDescriptor("Country", SortOrder.Ascending),
        new GroupByDescriptor("Department", SortOrder.Ascending)
    )
    .Page(5, 1) // Paginates the TOP-LEVEL groups
    .Build();
```

### Target JSON Structure (Handled by Providers)

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
          "items": [ { "userId": 1, "name": "Alice" } ]
        }
      ]
    }
  ]
}
```

---

## 🛡️ Security & Validation Engine

Never trust frontend query payloads. The core validation engine can either silently sanitize malicious requests or throw strict errors.

### Silent Strip (Recommended for Public APIs)

Silently removes denied columns and clamps pagination limits without throwing errors.

```csharp
query.Validate(rules =>
{
    // Prevent sensitive column leakage
    rules.Select(c => c.Deny("PasswordHash", "SecretKey", "DeletedAt"));
    
    // Prevent data-dump attacks
    rules.PageSize(p => p.Max(100));
}, ValidationMode.SilentStrip);
```

### Strict Validation (Recommended for Internal APIs)

Throws a `QueryValidationException` containing a list of all violated rules.

```csharp
try 
{
    query.Validate(rules => rules.Select(c => c.Deny("PasswordHash")), ValidationMode.ThrowException);
}
catch (QueryValidationException ex)
{
    // ex.InvalidProperties contains ["PasswordHash"]
}
```

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

---

## 🤝 Contributing & License

This project is part of the [PepperX Ecosystem](https://github.com/amirhosseinmp02/PepperX).
Licensed under the MIT License.