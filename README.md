<p align="center">
  <img src="https://raw.githubusercontent.com/amirhosseinmp02/PepperX/main/icon.png" alt="PepperX Ecosystem" width="140" />
</p>

<h1 align="center">The PepperX Ecosystem</h1>

<p align="center">
  <strong>A suite of high-performance, enterprise-grade engines and tools for the .NET ecosystem.</strong><br/>
  Designed for easy, bulletproof security, and seamless integration.
</p>

<p align="center">
  <a href="https://github.com/amirhosseinmp02/PepperX"><img src="https://img.shields.io/badge/GitHub-PepperX-181717?style=flat-square&logo=github" alt="GitHub"></a>
  <a href="https://www.nuget.org/profiles/AmirHosseinMp02"><img src="https://img.shields.io/badge/NuGet-Profile-0078D4?style=flat-square&logo=nuget" alt="NuGet"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square" alt="License"></a>
</p>

<br>

## 🧩 The Tool Suite

PepperX is a modular ecosystem. Each tool is engineered to solve specific, complex enterprise challenges without relying on heavy, black-box third-party dependencies.

| Package | Description | Status | NuGet |
| :--- | :--- | :---: | :--- |
| **[PepperX.QueryForge](./src/PepperX.QueryForge)** | **Abstract, provider-agnostic foundation** of the QueryForge ecosystem. It provides the core models, fluent builders, and bulletproof validation engines required to construct dynamic, paginated, and hierarchically grouped queries. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge) |
| **[PepperX.QueryForge.Dapper](./src/PepperX.QueryForge.Dapper)** | High-performance execution provider for the `PepperX.QueryForge` core library. It translates abstract query models into optimized SQL and executes them against **Microsoft SQL Server** using Dapper, seamlessly handling hierarchical grouping, complex filtering, and Stored Procedure/TVF integration. | ✅ Released | [![NuGet](https://img.shields.io/nuget/v/PepperX.QueryForge.Dapper?style=flat-square&label=)](https://www.nuget.org/packages/PepperX.QueryForge.Dapper) |
| **PepperX.QueryForge.EFCore** | Entity Framework Core execution provider for QueryForge. | 🔧 Dev | *Coming Soon* |

---

## 🏛️ Architecture & Philosophy

All libraries under the PepperX umbrella share a core set of engineering principles:

*   **🛡️ Security by Default:** Built-in validation engines (like `SilentStrip`) to prevent schema enumeration, data dumps, and malicious payloads.
*   **🧩 Provider-Agnostic Cores:** Define your intent once, execute it anywhere.
*   **🔄 Automated CI/CD:** All packages are built, tested, and published via GitHub Actions using OIDC Trusted Publishing.

---

## 📂 Repository Structure

This repository is an **Umbrella Monorepo**. 

*   **`/src`**: Contains the source code for all PepperX libraries. Each library folder contains its own dedicated `README.md` with deep-dive technical documentation, C# examples, and API references.
*   **`/tests`**: Comprehensive `xUnit` test suites ensuring bulletproof reliability across all engines.
*   **`/samples`**: Runnable ASP.NET Core Minimal API projects demonstrating real-world integration.

---

## 🤝 Contributing & License

Contributions, issues, and feature requests are welcome! 

Unless otherwise specified, all packages in the PepperX ecosystem are licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

<br>

<p align="center">
  <sub>Engineered with ❤️ and C# by <a href="https://github.com/amirhosseinmp02">Amir Hossein</a></sub>
</p>