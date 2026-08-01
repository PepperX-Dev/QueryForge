<p align="center">
  <a href="https://github.com/PepperX-Dev"><img src="https://img.shields.io/badge/Part_of-PepperX_Ecosystem-512BD4?style=for-the-badge&logo=github&logoColor=white" alt="Part of PepperX Ecosystem"></a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/PepperX-Dev/QueryForge/main/icon.png" alt="QueryForge" width="140" />
</p>

<h1 align="center">The QueryForge Ecosystem</h1>

<p align="center">
  <strong>A high-performance, provider-agnostic query engine for the .NET ecosystem.</strong><br/>
  Build dynamic, paginated, and hierarchically grouped queries with a fluent API — execute anywhere.
</p>

<p align="center">
  <a href="https://github.com/PepperX-Dev/QueryForge"><img src="https://img.shields.io/badge/GitHub-PepperX--Dev%2FQueryForge-181717?style=flat-square&logo=github" alt="GitHub"></a>
  <a href="https://www.nuget.org/profiles/AmirHosseinMp02"><img src="https://img.shields.io/badge/NuGet-Profile-0078D4?style=flat-square&logo=nuget" alt="NuGet"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square" alt="License"></a>
</p>

<br>

## 🧩 The QueryForge Suite

QueryForge is a modular query engine. It decouples the *intent* of a query from its *execution*, allowing you to define your query once and run it against multiple data sources through different providers.

| Package | Description | Status | NuGet |
| :--- | :--- | :---: | :--- |
| **[PepperX.QueryForge](./src/PepperX.QueryForge)** | The **provider-agnostic foundation**. Core models (`Query`), fluent builders, the validation engine, and a dependency-free **in-memory execution engine**. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge) |
| **[PepperX.QueryForge.Dapper](./src/PepperX.QueryForge.Dapper)** | Compiles query models into **parameterized SQL** and executes it with Dapper against **SQL Server, PostgreSQL, MySQL/MariaDB, Oracle and SQLite**. Nothing is deployed to your database. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge.Dapper?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge.Dapper) |
| **[PepperX.QueryForge.EFCore](./src/PepperX.QueryForge.EFCore)** | **Entity Framework Core** provider. Translates query models into expression trees so EF Core generates the SQL — works on every database EF Core supports and honours your model. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge.EFCore?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge.EFCore) |
| **[PepperX.QueryForge.InMemory](./src/PepperX.QueryForge.InMemory)** | **In-memory** provider for any `IEnumerable<T>` — cached data, composed API results, or as a test double for the database providers. Zero dependencies. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge.InMemory?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge.InMemory) |

### Database engine support

| Engine | Dapper provider | EF Core provider |
| :--- | :---: | :---: |
| Microsoft SQL Server | ✅ | ✅ |
| PostgreSQL | ✅ | ✅ |
| MySQL / MariaDB | ✅ | ✅ |
| Oracle | ✅ | ✅ |
| SQLite | ✅ | ✅ |
| Anything else with an EF Core provider | — | ✅ |

The EF Core column is ✅ throughout because EF Core generates the SQL; QueryForge only builds the
expression tree.

### One `Query`, the same answer everywhere

Two shared suites run against **every provider** on every build: 90 tests taking each input apart
(`Criteria`, `Paging`, `SelectColumns`, `SortColumns`, `GroupByColumns`, flat and grouped) and 35
business scenarios posting whole requests — dashboards, drill-down grids, exports, search, and raw
client JSON — against a seeded order book.

Both the Dapper and EF Core providers run those suites against **real database servers** — SQLite,
PostgreSQL, MySQL, SQL Server and Oracle — not just asserted SQL text. That is what surfaced the differences
worth standardising: null ordering (engines disagree by default, and EF Core inherited the problem by
deferring `ORDER BY` to the database) and loosely-typed filter values (`"30"` against an integer
column, which strict engines reject outright). Both are handled, so the same request returns the same
answer wherever it runs. See [tests/README.md](./tests/README.md).

---

## 🏛️ Architecture & Philosophy

QueryForge is built around a core set of engineering principles:

*   **🛡️ Security by Default:** Column names are checked against what the target actually exposes and
    anything unrecognised is dropped, so a filter payload cannot be used to probe your schema. Values
    are always parameters, never inlined text. On top of that, the validation engine (`SilentStrip` or
    `ThrowException`) gives you explicit allow/deny lists and page-size clamps.
*   **🧩 Provider-Agnostic Core:** Define your query intent once using the abstract `Query` model, and
    execute it anywhere. A cross-provider parity suite asserts that the same `Query` returns the same
    `QueryResult<T>` from every provider.
*   **🔌 Nothing to Deploy:** No stored procedures, no schema permissions, no startup migration step.
    Every provider builds its query at call time.
*   **🔄 Automated CI/CD:** All packages are built, tested, and published via GitHub Actions using OIDC Trusted Publishing.

---

## 📂 Repository Structure

This repository is an **Umbrella Monorepo**.

*   **`/src`**: Contains the source code for all QueryForge libraries. Each library folder contains its own dedicated `README.md` with deep-dive technical documentation, C# examples, and API references.
*   **`/tests`**: Comprehensive `xUnit` test suites ensuring bulletproof reliability across all engines.
*   **`/samples`**: Runnable ASP.NET Core Minimal API projects demonstrating real-world integration.

---

## 🤝 Contributing & License

Contributions, issues, and feature requests are welcome!

Unless otherwise specified, all packages in QueryForge are licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

<br>

<p align="center">
  <sub>Engineered with ❤️ and C# by <a href="https://github.com/PepperX-Dev">PepperX-Dev</a></sub>
</p>