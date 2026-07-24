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
| **[PepperX.QueryForge](./src/PepperX.QueryForge)** | The **abstract, provider-agnostic foundation** of QueryForge. It provides the core models (`Query`), fluent builders, and bulletproof validation engines required to construct dynamic, paginated, and hierarchically grouped queries. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge) |
| **[PepperX.QueryForge.Dapper](./src/PepperX.QueryForge.Dapper)** | High-performance execution provider for Dapper. Translates abstract query models into optimized SQL, seamlessly handling hierarchical grouping, complex filtering, and Stored Procedure/TVF integration. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge.Dapper?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge.Dapper) |
| **PepperX.QueryForge.EFCore** | **Entity Framework Core** execution provider for QueryForge. | 🔧 Dev | *Coming Soon* |
| **PepperX.QueryForge.InMemory** | In-memory execution provider designed for **IEnumerable** based types without database dependencies. | 📋 Planned | *Coming Soon* |

---

## 🏛️ Architecture & Philosophy

QueryForge is built around a core set of engineering principles:

*   **🛡️ Security by Default:** Built-in validation engines (`SilentStrip` or `ThrowException`) prevent schema enumeration, data dumps, and malicious payloads.
*   **🧩 Provider-Agnostic Core:** Define your query intent once using the abstract `Query` model, and execute it anywhere.
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