# Testing QueryForge

```bash
dotnet test QueryForge.slnx -c Release
```

That runs everything that needs no setup — the core, in-memory, SQLite and EF Core-over-SQLite
suites. The four server-backed engines are **opt-in**, so an ordinary build skips them instantly
rather than waiting out a connection timeout per engine.

## What runs where

| Project | What it covers |
| :--- | :--- |
| `PepperX.QueryForge.Tests` | Core models, builder, validation, and the shared query engine in isolation |
| `PepperX.QueryForge.Conformance` | **Not a test project.** The shared suites every provider must satisfy |
| `PepperX.QueryForge.Dapper.Tests` | Compiled SQL per dialect, plus both suites executed against SQLite, PostgreSQL, MySQL and SQL Server |
| `PepperX.QueryForge.EFCore.Tests` | Both suites executed through EF Core against SQLite, PostgreSQL, MySQL and SQL Server |
| `PepperX.QueryForge.InMemory.Tests` | Both suites executed against a plain collection |

## The two shared suites

Everything meaningful lives in `PepperX.QueryForge.Conformance` and is inherited by each provider, so a
behaviour change shows up on every provider at once instead of drifting apart quietly.

**`QueryForgeConformanceTests`** — 90 tests taking each input apart: `Criteria` (all eleven operators,
nulls, unusable conditions, all four logic modes, multiple groups), `Paging` (windows, page counts,
past-the-end, non-positive values), `SelectColumns`, `SortColumns` (multi-level, null placement,
numeric-not-lexical) and `GroupByColumns` (one to three levels, null keys, group paging), for flat and
grouped results.

**`SalesScenarioTests`** — 35 tests using the library the way an application does. A seeded order book
of 36 orders across two years, nine countries, four statuses and three sales reps, then whole requests:
a sales dashboard, a drill-down grid, an export, a search box, a client JSON payload posted verbatim.
Every expected value was derived from the seed data independently of the engine, so a wrong answer
fails rather than being re-asserted.

Adding a provider means writing one subclass of each and supplying a `RunAsync`.

## Running against real database servers

An engine runs when you either point it at a server explicitly, or ask for the built-in local
defaults:

```bash
# Use the repository's local defaults for anything not configured explicitly
QUERYFORGE_DB_TESTS=1 dotnet test QueryForge.slnx -c Release
```

Both providers read the same variables, so one set of connection strings configures the Dapper and
EF Core suites together:

```bash
export QUERYFORGE_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=queryforge"
export QUERYFORGE_MYSQL="Server=localhost;Port=3306;Uid=root;Pwd=root;Database=queryforge"
export QUERYFORGE_MSSQL="Server=localhost,1433;User Id=sa;Password=Your_password123;Database=queryforge;TrustServerCertificate=true"

dotnet test QueryForge.slnx -c Release
```

Or start throwaway ones with Docker:

```bash
docker run -d --name qf-pg    -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16
docker run -d --name qf-mysql -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=queryforge -p 3306:3306 mysql:8
docker run -d --name qf-mssql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Your_password123' -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest

export QUERYFORGE_POSTGRES="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres"
export QUERYFORGE_MYSQL="Server=localhost;Port=3306;Uid=root;Pwd=root;Database=queryforge"
export QUERYFORGE_MSSQL="Server=localhost,1433;User Id=sa;Password=Your_password123;Database=master;TrustServerCertificate=true"
```

SQL Server also installs natively on Debian and Ubuntu from Microsoft's repo, if you would rather not
use a container:

```bash
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor \
  | sudo tee /usr/share/keyrings/microsoft-prod.gpg > /dev/null
echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] \
  https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" \
  | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list
sudo apt-get update && sudo apt-get install -y mssql-server
sudo /opt/mssql/bin/mssql-conf setup
```

The tests create and drop their own tables (`qf_widget`, `qf_order`, `qf_ef_widget`, `qf_ef_order`),
so an empty database is all they need. To confirm a server was actually used rather than skipped,
watch the skip count fall — each engine contributes 126 tests per provider.

### Making a skip a failure

An unreachable engine skips its suites, which is right on a developer's machine and wrong in CI: a
database that never started would leave a green run that tested nothing. Setting
`QUERYFORGE_REQUIRE_DB=1` turns "configured but unreachable" into a failure, so a run cannot claim to
have covered an engine it never touched:

```bash
QUERYFORGE_REQUIRE_DB=1 dotnet test QueryForge.slnx -c Release
```

It only judges engines you configured — an engine with no connection string is still simply absent.

SQL Server and Oracle fixtures are in place for both providers and light up as soon as
`QUERYFORGE_MSSQL` / `QUERYFORGE_ORACLE` point at an instance:

```bash
docker run -d --name qf-oracle -e ORACLE_PASSWORD=queryforge -p 1521:1521 gvenzl/oracle-free:23-slim

export QUERYFORGE_ORACLE="User Id=system;Password=queryforge;Data Source=localhost:1521/FREEPDB1"
```

Oracle needs no local default connection string, because there is no ubiquitous throwaway instance the
way there is for the others — it runs only when pointed at one explicitly.

## Why real servers matter

Running against live PostgreSQL and MySQL found two defects that SQL-text assertions could not, and
auditing the generated SQL against Oracle's grammar found a third:

- **Loosely-typed values.** A JSON client sending `"value": "30"` against an integer column produced
  `operator does not exist: integer > text` on PostgreSQL. SQLite and MySQL coerce silently, so the
  bug was invisible until a strict engine saw it. Filter values are now coerced to the column's real
  type, discovered from the result set.
- **Null ordering.** PostgreSQL and Oracle sort nulls last ascending; SQL Server, MySQL and SQLite
  sort them first. The same query returned rows in a different order depending on the database. The
  Dapper dialects now pin this explicitly, and the EF Core provider orders by an explicit null-rank
  key first — it defers ORDER BY to the database, so it had exactly the same problem.
- **Derived table aliases.** The group-count statement was emitted as `FROM (...) AS qf_groups`.
  Oracle rejects `AS` in front of a table alias, so every grouped query would have failed there. The
  bare alias is valid on all five engines and is what the compiler emits now.

## Continuous integration

`.github/workflows/publish-nuget.yml` has two test jobs:

- **Build and test** runs on every push and pull request. It sets none of the `QUERYFORGE_*`
  variables, so the server-backed suites skip themselves and the job needs no services.
- **Test against real databases** spins up PostgreSQL, MySQL, SQL Server and Oracle as service
  containers and runs the full matrix with `QUERYFORGE_REQUIRE_DB=1`. It is triggered manually
  (`workflow_dispatch`) and automatically on a `v*.*.*` release tag, so a version cannot be published
  without having been exercised against every engine the Dapper provider claims to support.

Because a release tag is a bad place to discover a broken engine, run the job by hand
(**Actions → Build, Test, and Publish NuGet → Run workflow**) once on the commit you intend to tag.
It is the same job on the same containers, so a green dispatch means the tag will get the same answer.

The gate has already paid for itself twice. Its first run found that the EF Core fixture dropped its
tables with an undelimited name: Oracle folds that to upper case while EF Core creates a quoted
lower-case table, so the drop matched nothing and `CreateTables` then failed with ORA-00955 — 125
tests, the whole Oracle EF Core matrix. The same run showed the SQL Server container had no health
check, meaning a slow start would have skipped its suites and handed the gate a green run that never
touched SQL Server. Both are fixed, and `QUERYFORGE_REQUIRE_DB` now makes the second class of problem
impossible to miss.
