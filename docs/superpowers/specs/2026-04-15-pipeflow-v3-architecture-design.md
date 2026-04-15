# PipeFlow v3.0 — Architecture Design Specification

| Field | Value |
|-------|-------|
| **Status** | Draft — pending spec review |
| **Date** | 2026-04-15 |
| **Author** | Berkant (Nonanti) + Claude (brainstorming assistant) |
| **Target Release** | v3.0.0 GA |
| **Supersedes** | v2.1.0 (PipeFlowCore) |

---

## 1. Executive Summary

PipeFlow v3.0 is a **greenfield rewrite** of the v2.x codebase delivered as a **big-bang breaking release**. It addresses ~30 critical issues identified in the v2.1.0 code review (data-loss bugs, fake async, resource leaks, security holes, phantom features, test-coverage gaps) while repositioning PipeFlow as a **production-grade, DI-first, async-first .NET ETL library**.

The release renames the NuGet package from `PipeFlowCore` to `PipeFlow`, splits the monolithic package into **9 targeted packages**, and introduces first-class integration points with the standard .NET ecosystem (`Microsoft.Extensions.Logging`, `Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Options`).

## 2. Context & Motivation

### 2.1 What v2.x Got Right
- Source-agnostic fluent API (`PipeFlow.From.X.Filter(...).ToY(...)`)
- Broad source/sink coverage (CSV, JSON, Excel, SQL Server, PostgreSQL, MongoDB, REST API, AWS S3, Azure Blob, GCS)
- Performance focus (PostgreSQL binary COPY, SqlBulkCopy, streaming enumerables)
- MIT licensed OSS with CI + CHANGELOG + CONTRIBUTING discipline

### 2.2 What v2.x Got Wrong (Blocking Production)

**P0 — Data-correctness bugs:**
1. `Builder/PipelineDestinationExtensions.ToCsv` creates a new `CsvWriter` per row in a `ForEach`, truncating the file each call. Dataset size N → 1 row persisted.
2. `PipeFlowBuilder.FromXxx(path, configure => ...)` accepts options lambdas but never applies the configured options to the underlying reader.
3. `PostgreSqlWriter.WriteBatch` executes `ExecuteNonQuery()` per row inside the "batch" loop — not batching at all.
4. `SqlWriter.InsertBatch` builds parameterized inserts that will hit the 2100-parameter per-command limit on common batch sizes.

**P0 — Resource/lifecycle:**
5. `HttpClient` instantiated per `ApiReader`/`ApiWriter`, never disposed (socket exhaustion).
6. `MongoClient`/`AmazonS3Client` created per `Read()`/`Write()` call (no connection-pool reuse).
7. `S3Csv/AzureBlobCsv/GoogleCloudCsv` write to `Path.GetTempFileName()` and never clean up.
8. `ApiReader.Read()` uses `Task.Run(...).Wait()` — sync-over-async deadlock vector.

**P0 — Correctness of abstractions:**
9. `QueryablePipelineBuilder.Filter/OrderBy` accepts `Func<T,bool>`/`Func<T,TKey>` instead of `Expression<...>` — EF Core materializes the entire table client-side.
10. `Pipeline<T>.ExecuteAsync` is fake-async (`Task.Run(() => Execute())`).
11. `ParallelPipeline<T>.Filter` returns `Pipeline<T>` (not `ParallelPipeline<T>`) — parallelism dropped after first operation.
12. `DataRow` has no `IEquatable<DataRow>`/`GetHashCode` override — `pipeline.Distinct()` uses reference equality; silent no-op.

**P0 — Security:**
13. `SqlWriter`/`PostgreSqlWriter` interpolate `_tableName` and column identifiers directly into SQL — injection risk if any caller passes untrusted identifiers.

**P1 — Production essentials missing:**
14. No `ILogger` integration anywhere.
15. No `IHttpClientFactory` integration.
16. No `IOptions<T>` pattern — `PipeFlowConfig` referenced in README but does not exist.
17. No structured exception hierarchy — all errors wrapped as `Exception` losing type info.
18. `Pipeline.StreamAsync` awaits full task completion before yielding the first item (not true streaming).
19. Two classes literally named `PipeFlowBuilder` in different namespaces — ambiguous reference.
20. README examples for `DataValidator` use overloads (`Required("Id", "Email")`) that do not exist in the code.

**P2 — Test coverage:**
21. v2 Builder API (the "modern" API) has zero test coverage.
22. No integration tests with real DBs (no TestContainers).
23. `UnitTest1.cs` committed empty template-generated test.

### 2.3 Release Strategy

| Decision | Rationale |
|----------|-----------|
| **Big-bang v3.0** (no v2.2 patch series) | Dual-`PipeFlowBuilder` and dual-API problems cannot be cleanly fixed without breaking changes; incremental patches would accumulate debt. |
| **Rename `PipeFlowCore` → `PipeFlow`** | v2 package is deprecated in NuGet with `AlternatePackageId` pointing to v3. Shorter canonical name matches the library's identity. |
| **Production-ready scope (not minimal, not enterprise)** | Adds DI/ILogger/IHttpClientFactory/IOptions/structured exceptions. Excludes OpenTelemetry/Polly direct dependencies (users add their own via standard .NET patterns). |
| **Full split packages (9 total)** | One core + one package per heavy external dependency. Matches EF Core / AspNetCore / Serilog ecosystem conventions. Users pay only for what they use. |
| **Multi-target `net8.0`+`net10.0`** | `net8.0` = current LTS (supported to Nov 2026). `net10.0` = next LTS (supported to Nov 2028). `net9.0` skipped (STS, EOL May 2026). |
| **Async-first, no sync wrappers** | All I/O is `Task`/`IAsyncEnumerable`. Transformations (`Where`, `Select`) remain sync (CPU-bound, lazy). No sync entry points at all. |

## 3. Goals & Non-Goals

### 3.1 Goals
- Fix every P0/P1 issue identified in v2.1.0 review
- Provide a coherent, discoverable, DI-friendly public API
- Achieve ≥85% code coverage on the core package, ≥75% on each integration package
- Zero silent-failure paths (all errors surface as typed exceptions or structured results)
- First-class ASP.NET Core / Worker Service integration
- Production observability via `ILogger` + `ActivitySource`
- Complete test coverage including real-database integration tests via TestContainers
- Migration path documented for v2 users

### 3.2 Non-Goals
- **Not** a direct dependency on OpenTelemetry, Polly, Serilog (users add these themselves)
- **Not** backward-compatible with v2 — breaking changes are intentional
- **Not** bundling a CLI or dashboard
- **Not** offering an analyzer / code-fix migration tool in v3.0.0 (deferred to v3.1)
- **Not** pursuing NativeAOT certification in v3.0 (core is trim-safe; integration packages may not be)

## 4. Decision Summary

| Area | Decision |
|------|----------|
| Package layout | 9 NuGet packages (1 core + 8 integrations) |
| Target frameworks | `net8.0;net10.0` multi-target |
| API model | Async-first, LINQ-style (`Where`/`Select`) |
| Entry points | Static facade + Builder + DI extension |
| Options | `Microsoft.Extensions.Options` pattern |
| Logging | `Microsoft.Extensions.Logging.Abstractions` + `LoggerMessage` source gen |
| HTTP | `IHttpClientFactory` mandatory |
| Async model | `IAsyncEnumerable<T>` first-class; `ValueTask` on hot paths |
| Parallelism | `System.Threading.Channels` with bounded backpressure |
| Test framework | xUnit v3 |
| Integration tests | TestContainers (DBs) + LocalStack/Azurite/fake-gcs-server (cloud) |
| Versioning | Lockstep SemVer; all 9 packages share the same version number |
| Build | CPM + SourceLink + snupkg + deterministic builds |
| Migration | Guide + CHANGELOG + NuGet deprecation; analyzer deferred to v3.1 |

---

## 5. Section A — Repository Layout & Build System

### 5.1 Directory Structure

```
PipeFlow/
├── src/
│   ├── PipeFlow/                        # Core (CSV + JSON + HTTP + abstractions)
│   ├── PipeFlow.Excel/                  # ClosedXML
│   ├── PipeFlow.SqlServer/              # Microsoft.Data.SqlClient
│   ├── PipeFlow.PostgreSql/             # Npgsql
│   ├── PipeFlow.MongoDb/                # MongoDB.Driver
│   ├── PipeFlow.EntityFrameworkCore/    # EF Core 10
│   ├── PipeFlow.Aws/                    # AWSSDK.S3
│   ├── PipeFlow.Azure/                  # Azure.Storage.Blobs
│   └── PipeFlow.GoogleCloud/            # Google.Cloud.Storage.V1
├── tests/
│   ├── PipeFlow.Tests/                  # Core unit tests
│   ├── PipeFlow.Excel.Tests/
│   ├── PipeFlow.SqlServer.Tests/        # Unit + TestContainers
│   ├── PipeFlow.PostgreSql.Tests/       # Unit + TestContainers
│   ├── PipeFlow.MongoDb.Tests/          # Unit + TestContainers
│   ├── PipeFlow.EntityFrameworkCore.Tests/
│   ├── PipeFlow.Aws.Tests/              # LocalStack
│   ├── PipeFlow.Azure.Tests/            # Azurite
│   ├── PipeFlow.GoogleCloud.Tests/      # fake-gcs-server
│   └── PipeFlow.IntegrationTests/       # Cross-package scenarios
├── bench/
│   └── PipeFlow.Benchmarks/             # BenchmarkDotNet
├── samples/
│   └── PipeFlow.Samples/                # Executable, CI-verified docs source
├── docs/
│   ├── specs/                           # Architecture + per-integration specs
│   ├── migration-v2-to-v3.md
│   └── architecture.md
├── .config/
│   └── dotnet-tools.json                # dotnet-format, csharpier, dotnet-outdated
├── .github/
│   └── workflows/                       # build, release, benchmarks, mutation, docs
├── Directory.Build.props                # Shared MSBuild settings
├── Directory.Packages.props             # Central Package Management
├── global.json                          # SDK pinning
├── PipeFlow.slnx                        # Modern SLNX solution
├── README.md
├── CHANGELOG.md
├── CONTRIBUTING.md
└── LICENSE
```

### 5.2 Build System Decisions

1. **SLNX solution format** — XML-based, default in .NET 10 SDK.
2. **Central Package Management (CPM)** — All versions in `Directory.Packages.props`; csproj files contain only `<PackageReference Include="..." />`.
3. **SourceLink + deterministic builds** — `Microsoft.SourceLink.GitHub` + `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>` in CI.
4. **Symbol packages (.snupkg)** — Every `dotnet pack` generates `.snupkg`; pushed to nuget.org alongside `.nupkg`.
5. **Nullable enforcement** — `<Nullable>enable</Nullable>` + `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; all CS86xx warnings actually fixed, not suppressed.
6. **Trim safety** — Core package marked `<IsTrimmable>true</IsTrimmable>`. Integration packages document trim-status per README.
7. **Lockstep versioning** — Single `Version.props` controls all 9 packages.
8. **Tools manifest** — `dotnet tool restore` provides `dotnet-format`, `CSharpier`, `dotnet-outdated-tool`.
9. **EditorConfig** — 4-space indentation (fixing v2's Builder/ 2-space inconsistency), StyleCop analyzers, naming rules enforced in CI.

### 5.3 NuGet Package Names

| Package | Description |
|---------|-------------|
| `PipeFlow` | Core: abstractions, Pipeline, DataRow, Builder, DI, CSV, JSON, HTTP |
| `PipeFlow.Excel` | ClosedXML source/sink |
| `PipeFlow.SqlServer` | Microsoft.Data.SqlClient source/sink + bulk copy |
| `PipeFlow.PostgreSql` | Npgsql source/sink + binary COPY |
| `PipeFlow.MongoDb` | MongoDB.Driver source/sink |
| `PipeFlow.EntityFrameworkCore` | `IQueryable<T>` source + `DbContext` sink |
| `PipeFlow.Aws` | AWS S3 source/sink |
| `PipeFlow.Azure` | Azure Blob source/sink |
| `PipeFlow.GoogleCloud` | GCS source/sink |

---

## 6. Section B — Core Abstractions

### 6.1 Source / Sink Interfaces

```csharp
namespace PipeFlow.Abstractions;

public interface IPipelineSource<out T>
{
    IAsyncEnumerable<T> ReadAsync(CancellationToken cancellationToken = default);
}

public interface IPipelineSink<in T>
{
    Task WriteAsync(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default);
}
```

**Design notes:**
- Minimal surface — each integration package implements one or both of these.
- Source is read-once semantically (enumerating the returned `IAsyncEnumerable` drives the read).
- Sink owns flush/dispose lifecycle; it receives the whole pipeline stream and writes it.

### 6.2 `IPipeline<T>`

```csharp
namespace PipeFlow;

public interface IPipeline<T>
{
    // Composition (lazy)
    IPipeline<T> Where(Func<T, bool> predicate);
    IPipeline<T> Where(Func<T, int, bool> predicate);
    IPipeline<TResult> Select<TResult>(Func<T, TResult> selector);
    IPipeline<TResult> Select<TResult>(Func<T, int, TResult> selector);
    IPipeline<TResult> SelectMany<TResult>(Func<T, IEnumerable<TResult>> selector);
    IPipeline<TResult> SelectManyAsync<TResult>(
        Func<T, CancellationToken, IAsyncEnumerable<TResult>> selector);

    IPipeline<T> Take(int count);
    IPipeline<T> Skip(int count);
    IPipeline<T> TakeWhile(Func<T, bool> predicate);
    IPipeline<T> SkipWhile(Func<T, bool> predicate);

    IPipeline<T> Distinct(IEqualityComparer<T>? comparer = null);
    IOrderedPipeline<T> OrderBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null);
    IOrderedPipeline<T> OrderByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null);

    IPipeline<IGrouping<TKey, T>> GroupBy<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null);
    IPipeline<IReadOnlyList<T>> Chunk(int size);

    // Concurrency & binding
    IPipeline<T> AsParallel(int? maxDegreeOfParallelism = null);
    IPipeline<T> WithCancellation(CancellationToken cancellationToken);

    // Terminals
    IAsyncEnumerable<T> StreamAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> ToListAsync(CancellationToken ct = default);
    Task<T[]> ToArrayAsync(CancellationToken ct = default);
    Task<Dictionary<TKey, T>> ToDictionaryAsync<TKey>(Func<T, TKey> keySelector, CancellationToken ct = default)
        where TKey : notnull;
    Task<T> FirstAsync(CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(CancellationToken ct = default);
    Task<T> SingleAsync(CancellationToken ct = default);
    Task<T?> SingleOrDefaultAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<long> LongCountAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(Func<T, bool> predicate, CancellationToken ct = default);
    Task<bool> AllAsync(Func<T, bool> predicate, CancellationToken ct = default);
    Task ForEachAsync(Func<T, CancellationToken, ValueTask> action, CancellationToken ct = default);

    Task WriteToAsync(IPipelineSink<T> sink, CancellationToken ct = default);

    PipelineContext Context { get; }
}

public interface IOrderedPipeline<T> : IPipeline<T>
{
    IOrderedPipeline<T> ThenBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null);
    IOrderedPipeline<T> ThenByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null);
}

/// <summary>
/// Specialized pipeline for IQueryable-backed sources (EF Core).
/// Defers enumeration until terminal so Where/OrderBy/Take/Skip translate to SQL.
/// </summary>
public interface IQueryablePipeline<T> : IPipeline<T>
{
    IQueryablePipeline<T> Where(Expression<Func<T, bool>> predicate);
    IQueryablePipeline<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    IOrderedQueryablePipeline<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
    IOrderedQueryablePipeline<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
    IQueryablePipeline<T> WithPaging(int pageSize);
    IQueryablePipeline<T> AsNoTracking();  // EF Core-specific
}

public interface IOrderedQueryablePipeline<T> : IQueryablePipeline<T>, IOrderedPipeline<T>
{
    IOrderedQueryablePipeline<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
    IOrderedQueryablePipeline<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
}
```

**Note on `Chunk` for queryable pipelines:** `IQueryablePipeline<T>.Chunk(size)` forces a client-side materialization boundary — `Chunk` has no SQL translation. Calling `Chunk` downgrades the pipeline to `IPipeline<IReadOnlyList<T>>` (non-queryable). This is documented behavior and appears in the logger warning channel.

**Key decisions:**
- LINQ names (`Where`, `Select`) are the only names — no `Filter`/`Map` aliases.
- All terminal operations are async-only; no sync `ToList()`/`Count()`.
- `Chunk` returns `IPipeline<IReadOnlyList<T>>` — batching is composable, not terminal.
- `ForEachAsync` takes `Func<T, CT, ValueTask>` — zero-allocation for sync callbacks.
- `WithCancellation` binds a CT to the pipeline so terminal calls don't need to re-pass it.
- `WriteToAsync(IPipelineSink<T>)` is the single canonical write terminal; all `.ToXxxAsync(...)` convenience methods internally construct a sink and call this.
- `IQueryablePipeline<T>` accepts `Expression<...>` overloads so EF Core sources translate to SQL. This closes v2 issue #9 (the root cause of EF Core client-side materialization). `PipeFlow.EntityFrameworkCore` sources return `IQueryablePipeline<T>`; calls to `Where(Func<T,bool>)` (non-expression) on such a pipeline either overload-resolve to the `Expression` variant (most lambda expressions), or fall back to the base `IPipeline<T>` and log a warning that server-side translation was lost.

### 6.3 `PipelineContext`

Public `readonly record struct` — passed through every operation in the pipeline. Carries cross-cutting services.

```csharp
namespace PipeFlow;

public readonly record struct PipelineContext(
    ILogger Logger,
    PipeFlowOptions Options,
    IHttpClientFactory? HttpClientFactory,
    IServiceProvider? Services,
    CancellationToken CancellationToken = default,
    int? MaxDegreeOfParallelism = null);
```

### 6.4 `DataRow`

Redesigned from v2. Key changes: structural equality, null-friendly getter, caching.

```csharp
namespace PipeFlow;

public sealed class DataRow : IEquatable<DataRow>, IReadOnlyDictionary<string, object?>
{
    // Construction
    public DataRow();
    public DataRow(int capacity);
    public DataRow(IEnumerable<KeyValuePair<string, object?>> source);

    // Indexers
    public object? this[string columnName] { get; set; }  // null on missing (no throw)
    public object? this[int columnIndex] { get; set; }

    // Typed accessors
    public T? GetValue<T>(string columnName);             // InvariantCulture conversion
    public bool TryGetValue<T>(string columnName, out T? value);

    // Inspection
    public IEnumerable<string> Columns { get; }
    public int ColumnCount { get; }
    public bool ContainsColumn(string columnName);
    public bool Remove(string columnName);

    // Transformation
    public DataRow Clone();                                // defensive deep copy

    // Object mapping
    public static DataRow FromObject<T>(T source) where T : notnull;
    public T ToObject<T>() where T : new();

    // Equality (structural)
    public bool Equals(DataRow? other);
    public override int GetHashCode();                     // cached, invalidated on mutation
    public override bool Equals(object? obj);
    public static bool operator ==(DataRow? left, DataRow? right);
    public static bool operator !=(DataRow? left, DataRow? right);

    // IReadOnlyDictionary<string, object?> members elided
}
```

**Behavior changes vs v2:**
| v2 | v3 |
|-----|-----|
| `row["missing"]` throws `KeyNotFoundException` | Returns `null` |
| Type conversion uses current culture | Uses `CultureInfo.InvariantCulture` unless configured |
| No `Equals`/`GetHashCode` override | Structural equality; `Distinct()`/`GroupBy` work correctly |
| `IDataRow` interface | Removed (YAGNI — no external implementations) |

### 6.5 Exception Hierarchy

```csharp
namespace PipeFlow;

public abstract class PipeFlowException : Exception { ... }

public sealed class PipeFlowSourceException : PipeFlowException
{
    public string SourceType { get; }
    public string? Location { get; }
    public long? RowNumber { get; }
}

public sealed class PipeFlowSinkException : PipeFlowException
{
    public string SinkType { get; }
    public string? Location { get; }
}

public sealed class PipeFlowConfigurationException : PipeFlowException
{
    public string? OptionName { get; }
}

public sealed class PipeFlowValidationException : PipeFlowException
{
    public IReadOnlyList<ValidationError> Errors { get; }
}
```

All caught/wrapped exceptions preserve `InnerException` with original stack trace.

### 6.6 Database Sink Safety Rules (Mandatory)

All DB sinks across integration packages MUST follow these rules. They close v2 bugs #4 (SQL 2100-parameter-per-command limit) and #13 (identifier SQL injection):

1. **Bulk APIs only.** DB sinks use the provider's native bulk-copy path (`SqlBulkCopy` for SQL Server, `BeginBinaryImport` for PostgreSQL, `InsertMany`/`BulkWrite` for MongoDB). Row-per-parameter `INSERT ... VALUES (@p0, @p1, ...)` with dynamically sized batches is **not permitted** — it hits parameter-count limits on every mainstream DB provider at practical batch sizes.
2. **Identifier validation.** Table names, column names, and schema names received through public APIs MUST be validated before being interpolated into SQL. The core default is `^[A-Za-z_][A-Za-z0-9_]*$`; per-provider sub-specs may relax this to match their native rules (e.g., SQL Server allows `schema.table` with dots; Postgres allows unicode identifiers; names with spaces/special chars require explicit quoting opt-in). Non-matching identifiers throw `PipeFlowConfigurationException` with `OptionName` set. Each integration MUST declare its exact identifier regex in its sub-spec.
3. **Quoting.** Validated identifiers are provider-quoted (`[name]` for SQL Server, `"name"` for PostgreSQL) before interpolation. Raw concatenation is not permitted.
4. **Parameterized values.** Column values are always passed as parameters, never interpolated — enforced by bulk-copy APIs, but also true for any fallback path.

These rules are architecturally enforced: the `IPipelineSink<T>` contract does not expose a "raw SQL execute" method, and per-integration sub-specs inherit these constraints from this section.

---

## 7. Section C — Builder API & DI

### 7.1 Three Entry Points

```csharp
// Entry 1 — static facade (scripts, CLI)
await PipeFlow.From.Csv("input.csv")
    .Where(row => row.GetValue<string>("Status") == "Active")
    .ToJsonAsync("output.json");

// Entry 2 — builder (one-off configuration)
var pipeFlow = PipeFlow.CreateBuilder()
    .UseLogger(myLogger)
    .UseHttpClientFactory(myFactory)
    .Configure(opt => opt.DefaultBatchSize = 5000)
    .Build();

await pipeFlow.From.SqlServer(connStr, "SELECT * FROM Orders")
    .ToCsvAsync("orders.csv");

// Entry 3 — DI (ASP.NET Core, Worker Service)
services.AddPipeFlow(opt => opt.DefaultBatchSize = 5000)
    .AddSqlServer()
    .AddMongoDb();

public class ReportService(IPipeFlow pipeFlow, ILogger<ReportService> log)
{
    public async Task ProcessAsync(CancellationToken ct)
    {
        await pipeFlow.From.SqlServer(connStr, "...")
            .Where(row => row.GetValue<decimal>("Total") > 1000)
            .ToExcelAsync("report.xlsx", ct);
    }
}
```

All three paths resolve to the same `DefaultPipeFlow` implementation; only construction differs.

### 7.2 `IPipeFlow` & `ISourceBuilder`

```csharp
public interface IPipeFlow
{
    ISourceBuilder From { get; }
    PipelineContext Context { get; }
}

public interface ISourceBuilder
{
    // Core sources (shipped with PipeFlow package)
    IPipeline<DataRow> Csv(string filePath, Action<CsvReaderOptions>? configure = null);
    IPipeline<DataRow> Json(string filePath, Action<JsonReaderOptions>? configure = null);
    IPipeline<T> Json<T>(string filePath, Action<JsonReaderOptions>? configure = null);
    IPipeline<DataRow> Http(Uri url, Action<HttpReaderOptions>? configure = null);
    IPipeline<T> Http<T>(Uri url, Action<HttpReaderOptions>? configure = null);
    IPipeline<T> Collection<T>(IEnumerable<T> items);
    IPipeline<T> AsyncCollection<T>(IAsyncEnumerable<T> items);
    IPipeline<T> FromSource<T>(IPipelineSource<T> source);

    // Integration packages extend via extension methods on ISourceBuilder
    PipelineContext GetContext();  // accessible to extension methods
}
```

### 7.3 Integration Extension Pattern

Each integration package exposes:

1. **Source extension** on `ISourceBuilder`:
```csharp
// PipeFlow.SqlServer
public static class SqlServerSourceBuilderExtensions
{
    public static IPipeline<DataRow> SqlServer(
        this ISourceBuilder source, string connectionString, string query,
        Action<SqlServerReaderOptions>? configure = null) { ... }
}
```

2. **Sink extension** on `IPipeline<T>`:
```csharp
public static class PipelineSqlServerSinkExtensions
{
    public static Task ToSqlServerAsync(
        this IPipeline<DataRow> pipeline, string connectionString, string tableName,
        Action<SqlServerWriterOptions>? configure = null, CancellationToken ct = default) { ... }
}
```

3. **DI extension** on `IPipeFlowServicesBuilder` (from §7.5):
```csharp
public static class SqlServerPipeFlowServicesBuilderExtensions
{
    public static IPipeFlowServicesBuilder AddSqlServer(
        this IPipeFlowServicesBuilder builder,
        Action<SqlServerOptions>? configure = null) { ... }
}
```

### 7.4 `IPipeFlowBuilder` (Fluent, Non-DI)

```csharp
public interface IPipeFlowBuilder
{
    IPipeFlowBuilder UseLogger(ILogger logger);
    IPipeFlowBuilder UseLoggerFactory(ILoggerFactory factory);
    IPipeFlowBuilder UseHttpClientFactory(IHttpClientFactory factory);
    IPipeFlowBuilder UseServiceProvider(IServiceProvider services);
    IPipeFlowBuilder Configure(Action<PipeFlowOptions> configure);
    IPipeFlowBuilder ConfigureDefaults<TOptions>(Action<TOptions> configure) where TOptions : class, new();
    IPipeFlow Build();
}
```

### 7.5 DI `IPipeFlowServicesBuilder` (ServiceCollection-Backed)

Named differently from the fluent `IPipeFlowBuilder` in §7.4 to guarantee zero ambiguous references, even if a user imports both `using PipeFlow;` and `using PipeFlow.DependencyInjection;`. This closes v2 issue #19 (two classes literally named `PipeFlowBuilder`).

```csharp
namespace PipeFlow.DependencyInjection;

public interface IPipeFlowServicesBuilder
{
    IServiceCollection Services { get; }
}

public static class PipeFlowServiceCollectionExtensions
{
    public static IPipeFlowServicesBuilder AddPipeFlow(
        this IServiceCollection services,
        Action<PipeFlowOptions>? configure = null);
}
```

Integration DI extensions (`AddSqlServer`, `AddMongoDb`, etc.) are extension methods on `IPipeFlowServicesBuilder`.

### 7.6 Options Pattern

```csharp
namespace PipeFlow;

public sealed class PipeFlowOptions
{
    public int DefaultBatchSize { get; set; } = 1000;
    public int DefaultBufferSize { get; set; } = 65536;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool LogPipelineExecution { get; set; } = true;
    public CultureInfo DefaultCulture { get; set; } = CultureInfo.InvariantCulture;
    public DefaultOptions Defaults { get; } = new();
}

public sealed class DefaultOptions
{
    public CsvReaderOptions Csv { get; } = new();
    public JsonReaderOptions Json { get; } = new();
    public HttpReaderOptions Http { get; } = new();

    // Integration packages register per-integration option defaults in a keyed bag:
    //   opt.Set<SqlServerReaderOptions>(o => o.CommandTimeout = ...)
    //   opt.Get<SqlServerReaderOptions>()
    // This keeps DefaultOptions closed to extension while letting integrations
    // register any options type they need without partial classes.
    public T Get<T>() where T : class, new();
    public void Set<T>(Action<T> configure) where T : class, new();
}

public sealed class CsvReaderOptions
{
    public char Delimiter { get; set; } = ',';
    public bool HasHeaders { get; set; } = true;
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public int BufferSize { get; set; } = 65536;
    public bool TrimValues { get; set; } = true;
    public bool AutoConvertTypes { get; set; } = false;  // ⚠ changed from v2 default (was true)
    public CultureInfo? Culture { get; set; }
    public CsvReaderOptions Clone();
}
```

**`AutoConvertTypes = false`** default is a breaking behavioral change vs v2. CSV values are strings by default; users call `row.GetValue<int>("Age")` to convert explicitly. This eliminates the "0123" → 123 class of bugs.

### 7.7 Static Facade (`PipeFlow.From`)

```csharp
public static class PipeFlow
{
    // Default instance: NullLogger-only, no HttpClientFactory, no DI
    private static readonly IPipeFlow Default = CreateBuilder().Build();

    public static ISourceBuilder From => Default.From;
    public static IPipeFlowBuilder CreateBuilder() => new PipeFlowBuilder();
}
```

No setter on `PipeFlow.Default`. Users who want logging/DI use `CreateBuilder()` or `services.AddPipeFlow()`.

### 7.8 Validation

```csharp
await pipeFlow.From.Csv("users.csv")
    .Validate(v => v
        .Column("Email").Required().Email()
        .Column("Age").Range(0, 120)
        .Column("PostalCode").Regex(@"^\d{5}$")
        .Row(row => row["StartDate"] < row["EndDate"], "StartDate must be before EndDate"))
    .OnValidationError(ValidationErrorHandling.LogAndSkip)
    .ToSqlServerAsync(connStr, "Users");
```

`ValidationErrorHandling` enum: `Skip`, `LogAndSkip`, `Throw`, `Collect`. The v2 `Fix` value is removed (never implemented).

---

## 8. Section D — Async, Streaming & Resource Management

### 8.1 True `IAsyncEnumerable<T>` Streaming

Every source yields items via native `IAsyncEnumerable<T>` with `[EnumeratorCancellation]`. Example:

```csharp
internal sealed class CsvSource : IPipelineSource<DataRow>
{
    public async IAsyncEnumerable<DataRow> ReadAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read,
            FileShare.Read, _options.BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, _options.Encoding, false, _options.BufferSize);

        string[]? headers = null;
        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
        {
            if (headers is null && _options.HasHeaders)
            {
                headers = ParseCsvLine(line);
                continue;
            }
            yield return BuildRow(line, headers);
        }
    }
}
```

**Key points:**
- `await using var stream` — async disposal.
- `FileOptions.Asynchronous` — true OS-level async I/O.
- `ReadLineAsync(ct)` — net8+ overload accepts cancellation.
- `ConfigureAwait(false)` at every await in library code.

Pipeline composition (`Where`, `Select`, etc.) maps `IAsyncEnumerable<T>` to `IAsyncEnumerable<T>` without materializing; terminal operations (`ToListAsync`, `CountAsync`) drive enumeration.

### 8.2 Cancellation Propagation

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await pipeFlow.From.Csv("big.csv")
    .WithCancellation(cts.Token)
    .Where(row => ...)
    .ToSqlServerAsync(connStr, "Table");
```

`WithCancellation` stores the token in `PipelineContext.CancellationToken`. Terminal operations link the context token with any per-call CT:

```csharp
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    terminalCt, context.CancellationToken);
await foreach (var item in source.WithCancellation(linkedCts.Token)) { ... }
```

### 8.3 Resource Management

#### 8.3.1 `HttpClient` via `IHttpClientFactory`

```csharp
internal sealed class HttpSource<T> : IPipelineSource<T>
{
    private readonly IHttpClientFactory _factory;

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = _factory.CreateClient("PipeFlow.Http");  // named client
        // use client; DO NOT dispose
    }
}
```

- DI scenario: `IHttpClientFactory` resolved from `services.AddHttpClient()` (added automatically by `AddPipeFlow`).
- Builder scenario: internal `ServiceCollection` registers `AddHttpClient()`; exposed as `IHttpClientFactory` to all sources/sinks.
- Users adding resilience: `services.AddHttpClient("PipeFlow.Http").AddStandardResilienceHandler()` — PipeFlow picks up Polly v8 policies transparently.

#### 8.3.2 Client Caching for MongoDB/AWS/Azure/GCS

```csharp
// PipeFlow.MongoDb
internal static class MongoClientCache
{
    private static readonly ConcurrentDictionary<string, IMongoClient> _cache = new();

    public static IMongoClient Get(string connectionString) =>
        _cache.GetOrAdd(connectionString, cs => new MongoClient(cs));
}
```

- Client instances (MongoDB, AWS S3, Azure Blob, GCS) are thread-safe and designed for reuse — caching by connection-string is safe.
- DI scenario: `services.AddMongoDb()` registers `IMongoClientFactory` as singleton; cache lives inside factory.
- Builder/static scenario: static `ConcurrentDictionary` cache at package-assembly level.

#### 8.3.3 Zero Temp Files

`S3Csv`/`AzureBlobCsv`/`GoogleCloudCsv` no longer download to temp files. Source stream flows directly into CSV parser:

```csharp
public static class AwsSourceBuilderExtensions
{
    public static IPipeline<DataRow> S3Csv(
        this ISourceBuilder source, string bucket, string key,
        Action<CsvReaderOptions>? csvConfig = null,
        Action<S3Options>? s3Config = null)
    {
        var s3Source = new S3ObjectSource(bucket, key, s3Config, source.GetContext());
        var csvOverStream = new CsvSourceFromStream(s3Source.OpenReadStreamAsync, csvConfig, source.GetContext());
        return source.FromSource(csvOverStream);
    }
}
```

Memory footprint bounded by `CsvReaderOptions.BufferSize` (default 64 KB), regardless of blob size.

### 8.4 Parallelism via `System.Threading.Channels`

```csharp
public IPipeline<T> AsParallel(int? mdop = null)
    => new ParallelPipeline<T>(_source, mdop ?? Environment.ProcessorCount, _context);

internal sealed class ParallelPipeline<T> : IPipeline<T>
{
    public async IAsyncEnumerable<TResult> Select<TResult>(
        Func<T, TResult> selector,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateBounded<TResult>(new BoundedChannelOptions(_mdop * 2)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait   // backpressure
        });

        var workers = Enumerable.Range(0, _mdop).Select(_ => Task.Run(async () =>
        {
            await foreach (var item in _source.WithCancellation(ct))
                await channel.Writer.WriteAsync(selector(item), ct);
        })).ToArray();

        _ = Task.WhenAll(workers).ContinueWith(_ => channel.Writer.Complete());

        await foreach (var item in channel.Reader.ReadAllAsync(ct))
            yield return item;
    }
}
```

- `BoundedChannelFullMode.Wait` — producer waits when consumer can't keep up (bounded memory).
- Parallelism preserved across chain (each operation uses channel under the hood).
- Exceptions in workers surface through `Task.WhenAll` and complete the channel faulted.

### 8.5 Structured Logging

`LoggerMessage` source generator for zero-allocation logging:

```csharp
internal static partial class PipeFlowLogging
{
    [LoggerMessage(EventId = 100, Level = LogLevel.Information,
        Message = "Pipeline started: source={SourceType}, location={Location}")]
    public static partial void PipelineStarted(ILogger logger, string sourceType, string? location);

    [LoggerMessage(EventId = 101, Level = LogLevel.Information,
        Message = "Pipeline completed: rows={RowCount}, duration={Duration}")]
    public static partial void PipelineCompleted(ILogger logger, long rowCount, TimeSpan duration);

    [LoggerMessage(EventId = 200, Level = LogLevel.Error,
        Message = "Pipeline failed at row {RowNumber}: {Reason}")]
    public static partial void PipelineFailed(ILogger logger, long rowNumber, string reason, Exception ex);
}
```

Default `ILogger` is `NullLogger.Instance` — zero allocation if user hasn't configured logging.

### 8.6 `ActivitySource` for OpenTelemetry Bridge

```csharp
internal static class PipeFlowActivities
{
    public static readonly ActivitySource Source = new("PipeFlow", "3.0.0");
}

// In each source/sink:
using var activity = PipeFlowActivities.Source.StartActivity("Csv.Read");
activity?.SetTag("csv.path", _path);
```

Users opt in to OTel tracing:
```csharp
services.AddOpenTelemetry().WithTracing(t => t.AddSource("PipeFlow"));
```

PipeFlow has zero OTel package dependency; uses only `System.Diagnostics` BCL types.

---

## 9. Section E — Testing Strategy

### 9.1 Test Pyramid

```
  Benchmark (BenchmarkDotNet + baseline regression)
  Integration (TestContainers + LocalStack/Azurite/fake-gcs-server)
  Unit (xUnit v3 + FluentAssertions + Bogus)
```

### 9.2 Tooling

| Tool | Purpose |
|------|---------|
| xUnit v3 | Test framework — native runner, parallel by default |
| FluentAssertions | Readable assertions |
| Bogus | Realistic fake data generation |
| TestContainers.NET | SQL Server / PostgreSQL / MongoDB integration |
| LocalStack (via TestContainers) | AWS S3 integration |
| Azurite (via TestContainers) | Azure Blob integration |
| fake-gcs-server (via TestContainers) | GCS integration |
| Verify.xUnit | Snapshot testing for CSV/JSON output |
| FsCheck | Property-based testing (scoped to CSV/JSON roundtrip) |
| Coverlet | Code coverage collection |
| Stryker.NET | Mutation testing (nightly, core only) |
| BenchmarkDotNet | Performance regression detection |

### 9.3 Test Organization

`tests/` contains one project per `src/` project plus a cross-cutting `PipeFlow.IntegrationTests` project. Each integration test project includes:
- Unit tests (`Unit/` subfolder)
- Integration tests (`Integration/` subfolder) gated on `[Trait("Category", "Integration")]`
- TestContainers fixture (class-level, reused across tests in the collection)

### 9.4 Test-First (TDD) Scope

TDD is mandatory for:
- `CsvSource` parser (RFC 4180 compliance, quote-escape, multiline, BOM, CRLF/LF, delimiters)
- `DataRow` equality & hashing
- `Pipeline<T>` composition (Where/Select/Chunk chains)
- `ParallelPipeline` Channel mechanics (backpressure, cancellation, exception propagation)
- Options binding (v2's single biggest bug — ensure configuration lambdas actually apply)

All other code can be test-after.

### 9.5 Coverage Targets

| Package | Line | Branch |
|---------|------|--------|
| `PipeFlow` (core) | 85% | 75% |
| `PipeFlow.*` (integrations) | 75% | 65% |
| `PipeFlow.EntityFrameworkCore` | 70% | 60% |
| `PipeFlow.Aws/Azure/GoogleCloud` | 70% | 55% |

Enforcement via `coverlet.msbuild` thresholds; CI fails on regression.

### 9.6 Mutation Testing

Nightly Stryker.NET run against `PipeFlow` (core) only. Threshold: 80% mutation score (break <60%).

### 9.7 Samples as Living Documentation

`samples/PipeFlow.Samples/` contains 9 executable programs (one per scenario). CI compiles and (where feasible) runs each sample. README code snippets are generated from sample source with `<!-- snippet: ... -->` tags — stale documentation becomes mechanically impossible.

---

## 10. Section F — Migration, CI/CD & Release

### 10.1 Migration Guide (`docs/migration-v2-to-v3.md`)

Covers:
1. Package rename (`PipeFlowCore` → `PipeFlow` + integration packages).
2. API surface change table (28 rows).
3. Breaking changes CHANGELOG.
4. Example conversions for each common v2 pattern.
5. Pointer to future analyzer/code-fix (v3.1).

### 10.2 NuGet Deprecation

`PipeFlowCore 2.1.0` is marked deprecated on NuGet with:
- `<PackageDeprecationReason>Legacy</PackageDeprecationReason>`
- `<AlternatePackageId>PipeFlow</AlternatePackageId>`
- Deprecation message linking to migration guide.

Existing installs continue to work; `dotnet list package --deprecated` surfaces the warning.

### 10.3 CI/CD Workflows

Five workflows under `.github/workflows/`:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `build.yml` | push, PR | Matrix build (ubuntu/windows/macos × net8/net10), unit + integration tests, coverage upload |
| `benchmarks.yml` | PR (src/** paths) | BenchmarkDotNet regression detection (10% slowdown warns, 50% fails) |
| `mutation.yml` | nightly cron | Stryker.NET on core package |
| `release.yml` | tag `v*.*.*` | Build, pack all 9 packages, push to NuGet, create GitHub release |
| `dependency-check.yml` | weekly cron | Outdated + vulnerable package scan with issue dedup |
| `docs.yml` | push to main | DocFX build, deploy to GitHub Pages |

Build workflow improvements over v2:
- NuGet cache via `actions/setup-dotnet`'s `cache: true`
- Test results uploaded as TRX artifacts
- `dotnet format --verify-no-changes` enforces style
- TestContainers integration tests run only on Linux runners
- Codecov upload with fail-on-error
- Cancel-in-progress concurrency group

### 10.4 Release Pipeline

Tag-based (`v3.0.0`, `v3.0.0-beta.1`, etc.). `release.yml`:
1. Compute version from tag.
2. Build + test.
3. Pack all 9 packages (with symbols).
4. Sign packages (if cert available) via `dotnet nuget sign` + timestamper.
5. Push each package to nuget.org with `--skip-duplicate`.
6. Create GitHub Release with auto-generated notes + attached `.nupkg`/`.snupkg` artifacts.
7. Generate SBOM (`anchore/sbom-action`) attached to release.

### 10.5 Versioning Strategy

Lockstep SemVer across all 9 packages:
- `3.0.0-alpha.1`: Core + 3 DB packages (Wave 0 + 1)
- `3.0.0-alpha.2`: + Excel + EF Core (Wave 2)
- `3.0.0-beta.1`: + 3 cloud packages (Wave 3)
- `3.0.0-beta.2`: Documentation complete, polish
- `3.0.0-rc.1`: Feature freeze, bug fixes only
- `3.0.0`: GA

### 10.6 Documentation Site

`docs/` directory builds with DocFX, deploys to `https://nonanti.github.io/PipeFlow/` via `docs.yml` workflow. Site includes:
- Getting Started
- Concepts (Pipeline, Source, Sink, DataRow)
- Per-source and per-sink guides
- DI Integration
- Logging & Telemetry
- Testing Your Pipelines
- API Reference (auto-generated from XML doc comments)
- Migration v2 → v3
- Benchmarks

---

## 11. Implementation Roadmap (Hybrid Wave Strategy)

### Wave 0 — Foundation (Sequential, ~1.5 weeks)
- Repository scaffolding (`src/`, `tests/`, `bench/`, `samples/`, `docs/`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, tools manifest)
- `PipeFlow` core package:
  - Abstractions (`IPipelineSource<T>`, `IPipelineSink<T>`, `IPipeline<T>`, `IOrderedPipeline<T>`)
  - `Pipeline<T>`, `ParallelPipeline<T>` implementation (Channel-based)
  - `DataRow` with equality/hashing
  - `PipelineContext`, exception hierarchy
  - Builder (`PipeFlow.CreateBuilder()`, static `PipeFlow.From`)
  - DI extension (`AddPipeFlow`)
  - Options (`PipeFlowOptions`, `CsvReaderOptions`, etc.)
  - CSV source/sink (async, RFC 4180, true streaming)
  - JSON source/sink (System.Text.Json with `Utf8JsonReader` for large files)
  - HTTP source (via `IHttpClientFactory`)
  - Logging (`LoggerMessage` source gen)
  - `ActivitySource` tracing hooks
  - Validation (`DataValidator`, fluent API with column + row rules)
- `PipeFlow.Tests` — 85%+ coverage
- CI workflows (build, benchmarks, mutation)

**Exit criteria:** Core package passes all unit tests, ≥85% line / ≥75% branch coverage, benchmarks baseline established, `IQueryablePipeline<T>` abstraction reviewed (even though implementation lands in Wave 2).

### Wave 1 — Database Integrations (Parallel, ~1 week)
Three parallel agents:
- `PipeFlow.SqlServer` + tests (TestContainers MSSQL)
- `PipeFlow.PostgreSql` + tests (TestContainers Postgres)
- `PipeFlow.MongoDb` + tests (TestContainers MongoDB)

**Exit criteria:** Each integration passes unit + integration tests, 75%+ coverage. Published as `3.0.0-alpha.1`.

### Wave 2 — File Formats & EF Core (Parallel, ~1 week)
Two parallel agents:
- `PipeFlow.Excel` + tests
- `PipeFlow.EntityFrameworkCore` + tests (InMemory + SQLite providers)

**Exit criteria:** Both integrations pass tests. Published as `3.0.0-alpha.2`.

### Wave 3 — Cloud Storage (Parallel, ~1 week)
Three parallel agents:
- `PipeFlow.Aws` + tests (LocalStack)
- `PipeFlow.Azure` + tests (Azurite)
- `PipeFlow.GoogleCloud` + tests (fake-gcs-server)

**Exit criteria:** Each integration passes tests. Published as `3.0.0-beta.1`.

### Wave 4 — Polish & Release (Sequential, ~3 days)
- Cross-cutting integration test suite
- Benchmark suite expansion
- Migration guide
- DocFX site
- Samples completion
- README rewrite
- `3.0.0-beta.2` → `3.0.0-rc.1` → `3.0.0` GA

**Exit criteria:** All release checklist items green. NuGet `PipeFlowCore 2.1.0` deprecated.

### Agent Assignments per Wave

| Role | Agent Type | Responsibility |
|------|-----------|----------------|
| Architect | `feature-dev:code-architect` | Per-integration spec design |
| Implementer | `general-purpose` in git worktree | Feature implementation against spec |
| Reviewer | `superpowers:code-reviewer` | Pre-merge review of implementation |
| Verifier | `superpowers:verification-before-completion` | End-of-wave verification |

Each integration runs in an isolated git worktree (`superpowers:using-git-worktrees`) with its own branch, merged to `main` when review + verification pass.

---

## 12. Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| v2 users resist breaking changes | Migration guide + analyzer in v3.1 + 6-month parallel support of v2.1.0 on NuGet |
| TestContainers flakiness in CI | Reuse containers across tests via `[CollectionFixture]`; fallback to mocked integration when infra unavailable |
| net10 not yet stable when coding | Multi-target `net8.0` as primary; net10 TFM added once SDK stabilizes |
| 9-package maintenance burden | Central versioning (lockstep), shared build props, single release pipeline — administrative overhead ~minutes per release |
| Agent-generated code quality variance | Code review gate on every PR; `feature-dev:code-reviewer` + human review before merge |

## 13. Success Criteria for v3.0.0 GA

- [ ] All 9 packages published to nuget.org at version `3.0.0`
- [ ] `PipeFlowCore 2.1.0` deprecated on NuGet with `AlternatePackageId=PipeFlow`
- [ ] All unit tests pass on ubuntu/windows/macos × net8/net10
- [ ] All integration tests pass on Linux (TestContainers)
- [ ] Coverage: core ≥85%, integrations ≥75%
- [ ] Benchmark suite shows ≥2× throughput improvement over v2 for CSV → SQL Server scenario
- [ ] Migration guide published and reviewed
- [ ] DocFX site live at `https://nonanti.github.io/PipeFlow/`
- [ ] All samples compile and run in CI
- [ ] Zero P0 issues from v2 review remaining

---

## 14. Open Questions

None at spec finalization. Any open questions will be addressed in per-integration sub-specs during Wave 0-3.

## 15. Revision History

| Version | Date | Notes |
|---------|------|-------|
| 0.1 | 2026-04-15 | Initial draft from brainstorming session |
| 0.2 | 2026-04-15 | Spec-reviewer pass 1: added §6.6 DB sink safety (bulk-only + identifier validation), added `IQueryablePipeline<T>` for EF Core Expression-based translation, renamed DI builder to `IPipeFlowServicesBuilder`, added full `DataRow` method signatures, replaced "extension properties" with `Get<T>`/`Set<T>` keyed options bag, added branch coverage to Wave 0 exit criteria |
| 0.3 | 2026-04-15 | Spec-reviewer pass 2: fixed §7.3 to use `IPipeFlowServicesBuilder` (was still referencing old `IPipeFlowBuilder` in DI example), added explicit `IOrderedQueryablePipeline<T>` declaration, clarified §6.6 rule 2 (provider-specific identifier regex rules), documented `Chunk` behavior on queryable pipelines |
