# PipeFlow v3 — Wave 0.A Foundation Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a clean v3 repository scaffolding (build config, project layout, solution file, CI) and the foundational contracts of the core PipeFlow package (abstractions, `DataRow`, `PipelineContext`, exception hierarchy) with tests green on GitHub Actions.

**Architecture:** Big-bang v3 rewrite. v2 code is archived via a git tag then deleted. New layout uses SLNX solution format + Central Package Management + `Directory.Build.props` for shared settings. TDD for `DataRow` and exceptions (the v2 versions had structural bugs — `no IEquatable`, throwing getters). Interfaces are contract-only (implementations come in Wave 0.B).

**Tech Stack:** .NET 8 + .NET 10 (multi-target), C# latest, xUnit v3, FluentAssertions, GitHub Actions.

**Spec reference:** `docs/superpowers/specs/2026-04-15-pipeflow-v3-architecture-design.md` — especially Sections 5 (repo layout), 6.1-6.5 (abstractions, DataRow, exceptions), 9 (testing), 10.3 (CI).

**Exit criteria:**
- v2 code tagged + removed from working tree
- `dotnet build` succeeds for both target frameworks
- `dotnet test` passes all foundation tests
- GitHub Actions `build.yml` is green on main
- `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` in place and enforced
- `IPipeline<T>`, `IPipelineSource<T>`, `IPipelineSink<T>`, `IOrderedPipeline<T>`, `IQueryablePipeline<T>`, `IOrderedQueryablePipeline<T>` declared with XML doc comments
- `DataRow` implements structural equality (the v2 gap that broke `Distinct()`/`GroupBy`)
- Exception hierarchy in place

**NOT in this plan** (deferred to Wave 0.B+):
- `Pipeline<T>`/`ParallelPipeline<T>` implementations
- `PipeFlowBuilder`, DI extension, `PipeFlowOptions`
- CSV/JSON/HTTP sources or sinks
- Validation API
- Benchmark project
- Samples project
- Docs site workflow
- Release workflow

---

## File Structure

### Files DELETED (v2 cleanup)

```
PipeFlow/                 ← entire v2 library source
PipeFlow.Tests/           ← entire v2 test project
PipeFlow.Benchmarks/      ← v2 benchmarks (rewrite later)
Examples/                 ← v2 examples
PipeFlow.sln              ← replaced by PipeFlow.slnx
.editorconfig             ← replaced with comprehensive one
```

### Files CREATED (build infrastructure)

```
.gitignore                              ← updated .NET + IDE + tools
global.json                             ← pin SDK to net10
Directory.Build.props                   ← shared MSBuild: TFMs, nullable, LangVersion, analyzers, SourceLink, deterministic
Directory.Packages.props                ← Central Package Management
Version.props                           ← single source of version (3.0.0-alpha.1)
.editorconfig                           ← comprehensive C# style (4 space, rules, naming)
.config/dotnet-tools.json               ← csharpier, dotnet-outdated-tool
PipeFlow.slnx                           ← modern SLNX solution
README.md                               ← stubs pointing to migration guide (full rewrite later)
CHANGELOG.md                            ← v3.0.0-alpha.1 header only
```

### Files CREATED (src/PipeFlow core)

```
src/PipeFlow/PipeFlow.csproj
src/PipeFlow/Abstractions/IPipelineSource.cs
src/PipeFlow/Abstractions/IPipelineSink.cs
src/PipeFlow/DataRow.cs
src/PipeFlow/PipelineContext.cs
src/PipeFlow/IPipeline.cs
src/PipeFlow/IOrderedPipeline.cs
src/PipeFlow/IQueryablePipeline.cs
src/PipeFlow/IOrderedQueryablePipeline.cs
src/PipeFlow/Exceptions/PipeFlowException.cs
src/PipeFlow/Exceptions/PipeFlowSourceException.cs
src/PipeFlow/Exceptions/PipeFlowSinkException.cs
src/PipeFlow/Exceptions/PipeFlowConfigurationException.cs
src/PipeFlow/Exceptions/PipeFlowValidationException.cs
src/PipeFlow/Exceptions/ValidationError.cs
```

### Files CREATED (tests/PipeFlow.Tests)

```
tests/PipeFlow.Tests/PipeFlow.Tests.csproj
tests/PipeFlow.Tests/xunit.runner.json            ← parallel test config
tests/PipeFlow.Tests/GlobalUsings.cs              ← common usings
tests/PipeFlow.Tests/DataRowTests/ConstructionTests.cs
tests/PipeFlow.Tests/DataRowTests/IndexerTests.cs
tests/PipeFlow.Tests/DataRowTests/GetValueTests.cs
tests/PipeFlow.Tests/DataRowTests/EqualityTests.cs
tests/PipeFlow.Tests/DataRowTests/CloneTests.cs
tests/PipeFlow.Tests/ExceptionTests/ExceptionHierarchyTests.cs
tests/PipeFlow.Tests/ExceptionTests/ExceptionPropertiesTests.cs
tests/PipeFlow.Tests/Abstractions/InterfaceShapeTests.cs
```

### Files CREATED (CI)

```
.github/workflows/build.yml             ← new comprehensive build workflow
```

### Files KEPT AS-IS (will be rewritten in later plans)

```
LICENSE                                 ← unchanged
icon.png                                ← unchanged (used in package metadata)
CONTRIBUTING.md                         ← will update later in Wave 4
.github/workflows/release.yml           ← will rewrite in Wave 4
.github/workflows/dependency-check.yml  ← will improve in Wave 4
docs/superpowers/specs/2026-04-15-pipeflow-v3-architecture-design.md  ← the spec
```

---

## Chunk 1: Repository Setup & Build Configuration

### Task 1: Archive v2 state before touching anything

**Files:** none (git operation only)

- [ ] **Step 1.1: Verify current state is clean**

Run: `git status`
Expected: `working tree clean` (spec commit already on main)

- [ ] **Step 1.2: Tag current HEAD as v2.1.0-archived**

Run:
```bash
git tag -a v2.1.0-archived -m "Archive v2.1.0 state before v3 greenfield rewrite"
git tag --list 'v2*'
```
Expected output includes `v2.1.0-archived`.

- [ ] **Step 1.3: Push the tag to origin**

Run: `git push origin v2.1.0-archived`
Expected: `* [new tag] v2.1.0-archived -> v2.1.0-archived`

This preserves v2 code in git history accessible via `git checkout v2.1.0-archived`.

---

### Task 2: Delete v2 code

**Files:**
- Delete: `PipeFlow/` (entire directory)
- Delete: `PipeFlow.Tests/` (entire directory)
- Delete: `PipeFlow.Benchmarks/` (entire directory)
- Delete: `Examples/` (entire directory)
- Delete: `PipeFlow.sln`
- Delete: `.editorconfig` (will be replaced)

- [ ] **Step 2.1: Remove v2 directories and solution**

Run:
```bash
cd /home/nonantiy/Projects/PipeFlow
rm -rf PipeFlow/ PipeFlow.Tests/ PipeFlow.Benchmarks/ Examples/
rm -f PipeFlow.sln .editorconfig
```

- [ ] **Step 2.2: Verify only non-v2 files remain**

Run: `ls -la`
Expected: Only `.git/`, `.github/`, `.gitignore`, `CHANGELOG.md`, `CONTRIBUTING.md`, `LICENSE`, `README.md`, `docs/`, `icon.png` remain.

- [ ] **Step 2.3: Commit the deletion**

Run:
```bash
git add -A
git commit -m "chore: remove v2 code (archived as tag v2.1.0-archived)

Big-bang v3 rewrite starts with a clean slate. v2 code is preserved
in git history and accessible via the v2.1.0-archived tag for anyone
who needs to reference the old implementation.

See docs/superpowers/specs/2026-04-15-pipeflow-v3-architecture-design.md"
```

---

### Task 3: Add `global.json` to pin SDK

**Files:**
- Create: `global.json`

- [ ] **Step 3.1: Check currently installed SDKs**

Run: `dotnet --list-sdks`
Expected: Includes a 10.0.x entry (if not, install .NET 10 SDK first from dotnet.microsoft.com).

- [ ] **Step 3.2: Create global.json**

Write `global.json`:
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

Replace `10.0.100` with the first 10.0.x version from the list-sdks output.

- [ ] **Step 3.3: Verify SDK resolves**

Run: `dotnet --version`
Expected: Prints a 10.0.x version.

---

### Task 4: Write comprehensive `.gitignore`

**Files:**
- Create: `.gitignore`

- [ ] **Step 4.1: Write `.gitignore`**

Write `.gitignore`:
```gitignore
# Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/
artifacts/
TestResults/
coverage/

# Visual Studio / Rider / VS Code
.vs/
.vscode/
.idea/
*.user
*.suo
*.userosscache

# NuGet
*.nupkg
*.snupkg
**/packages/*
!**/packages/build/
.nuget/

# BenchmarkDotNet
BenchmarkDotNet.Artifacts/

# Build outputs
*.dll
*.exe
*.pdb

# Coverage
*.coverage
*.coveragexml
coverage.json

# macOS
.DS_Store

# Tools
.config/tools/

# Temp / test outputs
*.tmp
*.temp
~$*
test_data.csv
large_test_data.csv
employees.csv
employees.json
products.json
*.test.csv
*.test.json
```

- [ ] **Step 4.2: Verify .gitignore ignores bin/obj**

Run (after a later `dotnet build`): `git status` should not show `bin/` or `obj/`.

---

### Task 5: Create `Version.props` as single version source

**Files:**
- Create: `Version.props`

- [ ] **Step 5.1: Write Version.props**

Write `Version.props`:
```xml
<Project>
  <PropertyGroup>
    <VersionPrefix>3.0.0</VersionPrefix>
    <VersionSuffix>alpha.1</VersionSuffix>
    <AssemblyVersion>3.0.0.0</AssemblyVersion>
    <FileVersion>3.0.0.0</FileVersion>
  </PropertyGroup>
</Project>
```

---

### Task 6: Write `Directory.Build.props` (shared MSBuild)

**Files:**
- Create: `Directory.Build.props`

- [ ] **Step 6.1: Write Directory.Build.props**

Write `Directory.Build.props`:
```xml
<Project>

  <Import Project="Version.props" />

  <PropertyGroup Label="Target frameworks">
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <PropertyGroup Label="Build quality">
    <AnalysisMode>All</AnalysisMode>
    <AnalysisLevel>latest</AnalysisLevel>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Missing XML doc (temporarily) -->
  </PropertyGroup>

  <PropertyGroup Label="Deterministic + SourceLink" Condition="'$(CI)' == 'true'">
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
  </PropertyGroup>

  <PropertyGroup Label="Package metadata" Condition="'$(IsPackable)' == 'true'">
    <Authors>Nonanti</Authors>
    <Company>Nonanti</Company>
    <Copyright>Copyright (c) 2024-2026 Berkant (Nonanti)</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/Nonanti/PipeFlow</PackageProjectUrl>
    <RepositoryUrl>https://github.com/Nonanti/PipeFlow</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
  </PropertyGroup>

  <ItemGroup Condition="'$(IsPackable)' == 'true'">
    <None Include="$(MSBuildThisFileDirectory)icon.png" Pack="true" PackagePath="\" Visible="false" />
    <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
  </ItemGroup>

</Project>
```

- [ ] **Step 6.2: Verify the file parses as XML**

Run: `xmllint --noout Directory.Build.props` (if xmllint available) or inspect manually.

Expected: No output (silent success) means well-formed XML.

---

### Task 7: Write `Directory.Packages.props` (Central Package Management)

**Files:**
- Create: `Directory.Packages.props`

- [ ] **Step 7.1: Write Directory.Packages.props**

Write `Directory.Packages.props`:
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="Core runtime">
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="9.0.0" />
    <PackageVersion Include="System.Linq.Async" Version="6.0.1" />
  </ItemGroup>

  <ItemGroup Label="Source link (privateAssets)">
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup Label="Test stack">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageVersion Include="xunit.v3" Version="1.0.0" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.0" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>

</Project>
```

**Note on versions:** Pin to current-stable versions as of 2026-04-15. Verify availability before committing:

```bash
dotnet package search xunit.v3 --exact-match
dotnet package search xunit.runner.visualstudio --exact-match
```

Expected: Each returns the latest version on nuget.org.

**If `xunit.v3` is NOT available yet**, fall back to xUnit 2.x. Replace the test-stack block in `Directory.Packages.props` with:

```xml
<ItemGroup Label="Test stack (xUnit 2.x fallback)">
  <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
  <PackageVersion Include="xunit" Version="2.9.2" />
  <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
  <PackageVersion Include="FluentAssertions" Version="7.0.0" />
  <PackageVersion Include="coverlet.collector" Version="6.0.2" />
</ItemGroup>
```

And in `tests/PipeFlow.Tests/PipeFlow.Tests.csproj` (Task 14.1) replace `<PackageReference Include="xunit.v3" />` with `<PackageReference Include="xunit" />`. Add a CHANGELOG note documenting the deviation and mark a revisit ticket for when xunit.v3 GA ships.

---

### Task 8: Write comprehensive `.editorconfig`

**Files:**
- Create: `.editorconfig`

- [ ] **Step 8.1: Write .editorconfig**

Write `.editorconfig`:
```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{yml,yaml,json}]
indent_size = 2

[*.{csproj,props,targets}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false

[*.cs]
# Sort using directives
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Language rules
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_predefined_type_for_member_access = true:warning

# Expression-bodied members
csharp_style_expression_bodied_methods = when_on_single_line:silent
csharp_style_expression_bodied_constructors = when_on_single_line:silent
csharp_style_expression_bodied_properties = when_on_single_line:suggestion

# Pattern matching
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion

# Modern features
csharp_style_prefer_primary_constructors = true:suggestion
csharp_style_prefer_pattern_matching = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion

# Naming
dotnet_naming_rule.interface_rule.severity = warning
dotnet_naming_rule.interface_rule.symbols = interface
dotnet_naming_rule.interface_rule.style = interface_style

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_style.interface_style.required_prefix = I
dotnet_naming_style.interface_style.capitalization = pascal_case

dotnet_naming_rule.private_field_rule.severity = warning
dotnet_naming_rule.private_field_rule.symbols = private_field
dotnet_naming_rule.private_field_rule.style = underscore_camel

dotnet_naming_symbols.private_field.applicable_kinds = field
dotnet_naming_symbols.private_field.applicable_accessibilities = private
dotnet_naming_style.underscore_camel.required_prefix = _
dotnet_naming_style.underscore_camel.capitalization = camel_case

# Code quality analyzers
dotnet_diagnostic.CA1014.severity = none  # CLSCompliant attribute (not needed)
dotnet_diagnostic.CA2007.severity = none  # ConfigureAwait (library code, handled case by case)
```

---

### Task 9: Create `.config/dotnet-tools.json` (tool manifest)

**Files:**
- Create: `.config/dotnet-tools.json`

- [ ] **Step 9.1: Initialize tool manifest and install tools**

Run:
```bash
dotnet new tool-manifest
dotnet tool install csharpier
dotnet tool install dotnet-outdated-tool
```

This creates `.config/dotnet-tools.json` with two tools registered. Note: `dotnet format` is built into the SDK since .NET 6 — no separate tool install needed; CI calls `dotnet format` directly.

- [ ] **Step 9.2: Verify tools restore**

Run: `dotnet tool restore`
Expected: `Tool 'csharpier' (version '...') was restored.` (and similar for dotnet-outdated-tool).

---

### Task 10: README + CHANGELOG stubs

**Files:**
- Overwrite: `README.md`
- Overwrite: `CHANGELOG.md`

- [ ] **Step 10.1: Overwrite README.md with stub**

Write `README.md`:
```markdown
# PipeFlow

> **⚠️ v3.0 in development — not yet on NuGet.** For the current stable release (v2.1.0, NuGet: `PipeFlowCore`), see the [v2.1.0-archived tag](https://github.com/Nonanti/PipeFlow/tree/v2.1.0-archived).

A modern, production-grade ETL pipeline library for .NET with DI integration, async-first streaming, and broad data source coverage.

## Status

v3.0 is being developed in-repo as a big-bang rewrite. Follow progress:

- Architecture spec: [`docs/superpowers/specs/2026-04-15-pipeflow-v3-architecture-design.md`](docs/superpowers/specs/2026-04-15-pipeflow-v3-architecture-design.md)
- Current wave: 0.A (Foundation)

## Quick Links (v3 targets, under construction)

- **Packages:** `PipeFlow` + 8 integrations (SqlServer, PostgreSql, MongoDb, EntityFrameworkCore, Excel, Aws, Azure, GoogleCloud)
- **Frameworks:** .NET 8, .NET 10
- **License:** [MIT](LICENSE)

## For v2 Users

v2 code lives at tag `v2.1.0-archived`. v3 will ship with a migration guide once stable (`docs/migration-v2-to-v3.md`).

## Author

**Berkant** — [GitHub](https://github.com/Nonanti)
```

- [ ] **Step 10.2: Overwrite CHANGELOG.md with v3 starting entry**

Write `CHANGELOG.md`:
```markdown
# Changelog

All notable changes to PipeFlow will be documented in this file.

## [Unreleased]

### Wave 0.A — Foundation

- Archive v2.1.0 as git tag `v2.1.0-archived`
- Reorganize repository into SLNX solution + Central Package Management + shared MSBuild props
- Multi-target `net8.0` and `net10.0`
- Introduce core abstractions: `IPipelineSource<T>`, `IPipelineSink<T>`, `IPipeline<T>`, `IOrderedPipeline<T>`, `IQueryablePipeline<T>`, `IOrderedQueryablePipeline<T>`
- Redesign `DataRow` with structural `IEquatable<DataRow>` + `GetHashCode` caching
- Structured exception hierarchy: `PipeFlowException`, `PipeFlowSourceException`, `PipeFlowSinkException`, `PipeFlowConfigurationException`, `PipeFlowValidationException`
- GitHub Actions `build.yml` rewritten with NuGet cache, xUnit v3, and coverage artifacts

## [2.1.0] — 2024-01-14

(see tag v2.1.0-archived)
```

---

### Task 11: Create `PipeFlow.slnx`

**Files:**
- Create: `PipeFlow.slnx`

- [ ] **Step 11.1: Write PipeFlow.slnx**

Write `PipeFlow.slnx`:
```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/PipeFlow/PipeFlow.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/PipeFlow.Tests/PipeFlow.Tests.csproj" />
  </Folder>
  <Folder Name="/docs/">
    <File Path="docs/superpowers/specs/2026-04-15-pipeflow-v3-architecture-design.md" />
    <File Path="docs/superpowers/plans/2026-04-15-v3-wave0a-foundation.md" />
  </Folder>
  <Folder Name="/_config/">
    <File Path=".editorconfig" />
    <File Path="Directory.Build.props" />
    <File Path="Directory.Packages.props" />
    <File Path="Version.props" />
    <File Path="global.json" />
    <File Path="README.md" />
    <File Path="CHANGELOG.md" />
  </Folder>
</Solution>
```

---

### Task 12: Commit build infrastructure

**Files:** none (git commit)

- [ ] **Step 12.1: Review staged changes**

Run: `git status && git diff --stat`

Expected new files:
- `.config/dotnet-tools.json`
- `.editorconfig`
- `.gitignore`
- `CHANGELOG.md`
- `Directory.Build.props`
- `Directory.Packages.props`
- `PipeFlow.slnx`
- `README.md`
- `Version.props`
- `global.json`

- [ ] **Step 12.2: Commit**

Run:
```bash
git add .
git commit -m "chore: scaffold v3 build infrastructure

- global.json pins SDK
- Directory.Build.props: multi-target net8.0+net10.0, nullable strict,
  warnings-as-errors, analyzers, SourceLink, symbol packages
- Directory.Packages.props: Central Package Management
- Version.props: single source of version (3.0.0-alpha.1)
- .editorconfig: comprehensive C# style rules
- .config/dotnet-tools.json: csharpier, dotnet-outdated (dotnet format is SDK-builtin)
- PipeFlow.slnx: modern SLNX solution format
- README + CHANGELOG stubs

No source projects yet — those come next."
```

---

## Chunk 2: Exception Hierarchy (TDD)

### Task 13: Create `src/PipeFlow/PipeFlow.csproj`

**Files:**
- Create: `src/PipeFlow/PipeFlow.csproj`

- [ ] **Step 13.1: Create directory and csproj**

Run:
```bash
mkdir -p src/PipeFlow
```

Write `src/PipeFlow/PipeFlow.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <PackageId>PipeFlow</PackageId>
    <Description>Production-grade ETL pipeline library for .NET with DI integration, async-first streaming, and broad data source coverage.</Description>
    <PackageTags>etl;pipeline;pipeflow;csv;json;data-processing;streaming;async;async-enumerable;dependency-injection</PackageTags>
    <IsPackable>true</IsPackable>
    <IsTrimmable>true</IsTrimmable>
    <IsAotCompatible>true</IsAotCompatible>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="System.Linq.Async" />
  </ItemGroup>

</Project>
```

- [ ] **Step 13.2: Verify restore**

Run: `dotnet restore src/PipeFlow/PipeFlow.csproj`
Expected: Restore succeeds.

- [ ] **Step 13.3: Verify build**

Run: `dotnet build src/PipeFlow/PipeFlow.csproj`
Expected: Build succeeds (empty assembly, no source yet).

---

### Task 14: Create tests project

**Files:**
- Create: `tests/PipeFlow.Tests/PipeFlow.Tests.csproj`
- Create: `tests/PipeFlow.Tests/GlobalUsings.cs`
- Create: `tests/PipeFlow.Tests/xunit.runner.json`

- [ ] **Step 14.1: Create directory and files**

Run:
```bash
mkdir -p tests/PipeFlow.Tests
```

Write `tests/PipeFlow.Tests/PipeFlow.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\PipeFlow\PipeFlow.csproj" />
  </ItemGroup>

</Project>
```

Write `tests/PipeFlow.Tests/GlobalUsings.cs`:
```csharp
global using Xunit;
global using FluentAssertions;
global using PipeFlow;
// PipeFlow.Abstractions added in Chunk 4 (Task 29) when those types first exist.
```

Write `tests/PipeFlow.Tests/xunit.runner.json`:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "longRunningTestSeconds": 10
}
```

- [ ] **Step 14.2: Verify test project builds**

Run: `dotnet build tests/PipeFlow.Tests/PipeFlow.Tests.csproj`
Expected: Build succeeds (test project empty, no tests yet).

- [ ] **Step 14.3: Commit scaffolding**

Run:
```bash
git add src tests
git commit -m "feat: scaffold src/PipeFlow + tests/PipeFlow.Tests projects

Empty csproj files. Abstractions, DataRow, exceptions come next.
Test project wired for xUnit v3 + FluentAssertions + coverlet."
```

---

### Task 15: Write failing tests for `PipeFlowException` (base)

**Files:**
- Create: `tests/PipeFlow.Tests/ExceptionTests/ExceptionHierarchyTests.cs`

- [ ] **Step 15.1: Write the failing test**

Create `tests/PipeFlow.Tests/ExceptionTests/ExceptionHierarchyTests.cs`:
```csharp
// Explicit import — PipeFlow.Exceptions is not in GlobalUsings because
// the exception types don't exist until Tasks 16-20 implement them.
using PipeFlow.Exceptions;

namespace PipeFlow.Tests.ExceptionTests;

public class ExceptionHierarchyTests
{
    [Fact]
    public void PipeFlowException_IsAbstract()
    {
        typeof(PipeFlowException).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void PipeFlowException_InheritsFromException()
    {
        typeof(PipeFlowException).BaseType.Should().Be(typeof(Exception));
    }

    [Fact]
    public void PipeFlowSourceException_InheritsFromPipeFlowException()
    {
        typeof(PipeFlowSourceException).BaseType.Should().Be(typeof(PipeFlowException));
    }

    [Fact]
    public void PipeFlowSinkException_InheritsFromPipeFlowException()
    {
        typeof(PipeFlowSinkException).BaseType.Should().Be(typeof(PipeFlowException));
    }

    [Fact]
    public void PipeFlowConfigurationException_InheritsFromPipeFlowException()
    {
        typeof(PipeFlowConfigurationException).BaseType.Should().Be(typeof(PipeFlowException));
    }

    [Fact]
    public void PipeFlowValidationException_InheritsFromPipeFlowException()
    {
        typeof(PipeFlowValidationException).BaseType.Should().Be(typeof(PipeFlowException));
    }

    [Fact]
    public void AllDerivedExceptions_AreSealed()
    {
        typeof(PipeFlowSourceException).IsSealed.Should().BeTrue();
        typeof(PipeFlowSinkException).IsSealed.Should().BeTrue();
        typeof(PipeFlowConfigurationException).IsSealed.Should().BeTrue();
        typeof(PipeFlowValidationException).IsSealed.Should().BeTrue();
    }
}
```

- [ ] **Step 15.2: Run test, expect failure**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~ExceptionHierarchyTests"`
Expected: Compilation error — `PipeFlowException`, `PipeFlowSourceException`, etc. do not exist yet.

---

### Task 16: Implement `PipeFlowException` (abstract base)

**Files:**
- Create: `src/PipeFlow/Exceptions/PipeFlowException.cs`

- [ ] **Step 16.1: Write implementation**

Create `src/PipeFlow/Exceptions/PipeFlowException.cs`:
```csharp
namespace PipeFlow.Exceptions;

/// <summary>
/// Base class for all exceptions thrown by PipeFlow. Catch this type to handle
/// any error originating from the library while letting unrelated exceptions propagate.
/// </summary>
public abstract class PipeFlowException : Exception
{
    protected PipeFlowException(string message) : base(message) { }

    protected PipeFlowException(string message, Exception? innerException)
        : base(message, innerException) { }
}
```

**Namespace note:** Spec §6.5 shows `namespace PipeFlow;` for the exception block. We subdivide into `PipeFlow.Exceptions` for file organization only — consumers can still `catch (PipeFlowException)` because the test GlobalUsings.cs includes `using PipeFlow.Exceptions;` (added implicitly via `using PipeFlow;` at call sites if the user wants — or explicitly imported). This sub-namespace decision is ergonomic, not architectural.

---

### Task 17: Implement `PipeFlowSourceException`

**Files:**
- Create: `src/PipeFlow/Exceptions/PipeFlowSourceException.cs`

- [ ] **Step 17.1: Write implementation**

Create `src/PipeFlow/Exceptions/PipeFlowSourceException.cs`:
```csharp
namespace PipeFlow.Exceptions;

/// <summary>
/// Thrown when a pipeline source fails to produce data (file not found,
/// connection failure, parse error, etc.). Inspect <see cref="SourceType"/>
/// and <see cref="Location"/> for structured error handling.
/// </summary>
public sealed class PipeFlowSourceException : PipeFlowException
{
    /// <summary>Kind of source that failed (e.g., "Csv", "SqlServer", "Http").</summary>
    public string SourceType { get; }

    /// <summary>Source-specific location (file path, URL, connection-string alias). May be null.</summary>
    public string? Location { get; }

    /// <summary>Row number where the failure occurred, if applicable.</summary>
    public long? RowNumber { get; }

    public PipeFlowSourceException(string sourceType, string message)
        : base(message)
    {
        SourceType = sourceType;
    }

    public PipeFlowSourceException(string sourceType, string? location, string message,
        Exception? innerException = null, long? rowNumber = null)
        : base(message, innerException)
    {
        SourceType = sourceType;
        Location = location;
        RowNumber = rowNumber;
    }
}
```

---

### Task 18: Implement `PipeFlowSinkException`

**Files:**
- Create: `src/PipeFlow/Exceptions/PipeFlowSinkException.cs`

- [ ] **Step 18.1: Write implementation**

Create `src/PipeFlow/Exceptions/PipeFlowSinkException.cs`:
```csharp
namespace PipeFlow.Exceptions;

/// <summary>
/// Thrown when a pipeline sink fails to consume data (write error, constraint violation,
/// connection failure, etc.).
/// </summary>
public sealed class PipeFlowSinkException : PipeFlowException
{
    /// <summary>Kind of sink that failed (e.g., "Csv", "SqlServer", "Http").</summary>
    public string SinkType { get; }

    /// <summary>Sink-specific location. May be null.</summary>
    public string? Location { get; }

    public PipeFlowSinkException(string sinkType, string message)
        : base(message)
    {
        SinkType = sinkType;
    }

    public PipeFlowSinkException(string sinkType, string? location, string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        SinkType = sinkType;
        Location = location;
    }
}
```

---

### Task 19: Implement `PipeFlowConfigurationException`

**Files:**
- Create: `src/PipeFlow/Exceptions/PipeFlowConfigurationException.cs`

- [ ] **Step 19.1: Write implementation**

Create `src/PipeFlow/Exceptions/PipeFlowConfigurationException.cs`:
```csharp
namespace PipeFlow.Exceptions;

/// <summary>
/// Thrown when PipeFlow detects invalid configuration: bad option values, unresolved
/// services, unsafe identifiers, etc.
/// </summary>
public sealed class PipeFlowConfigurationException : PipeFlowException
{
    /// <summary>Name of the option or configuration key that triggered the failure, if known.</summary>
    public string? OptionName { get; }

    public PipeFlowConfigurationException(string message) : base(message) { }

    public PipeFlowConfigurationException(string? optionName, string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        OptionName = optionName;
    }
}
```

---

### Task 20: Implement `ValidationError` + `PipeFlowValidationException`

**Files:**
- Create: `src/PipeFlow/Exceptions/ValidationError.cs`
- Create: `src/PipeFlow/Exceptions/PipeFlowValidationException.cs`

- [ ] **Step 20.1: Write ValidationError**

Create `src/PipeFlow/Exceptions/ValidationError.cs`:
```csharp
namespace PipeFlow.Exceptions;

/// <summary>
/// Describes a single validation failure. A row may produce multiple
/// <see cref="ValidationError"/> instances (one per rule that failed).
/// </summary>
public sealed record ValidationError(
    string ColumnName,
    string Message,
    object? AttemptedValue = null,
    long? RowNumber = null);
```

- [ ] **Step 20.2: Write PipeFlowValidationException**

Create `src/PipeFlow/Exceptions/PipeFlowValidationException.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;

namespace PipeFlow.Exceptions;

/// <summary>
/// Thrown when <see cref="ValidationErrorHandling.Throw"/> is chosen and one or more
/// rows fail validation. Inspect <see cref="Errors"/> for the full set.
/// </summary>
public sealed class PipeFlowValidationException : PipeFlowException
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public PipeFlowValidationException(IReadOnlyList<ValidationError> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildMessage(IReadOnlyList<ValidationError> errors)
    {
        if (errors.Count == 1)
            return $"Validation failed: {errors[0].ColumnName}: {errors[0].Message}";

        var first = errors.Take(3).Select(e => $"{e.ColumnName}: {e.Message}");
        var suffix = errors.Count > 3 ? $" (+ {errors.Count - 3} more)" : string.Empty;
        return $"Validation failed ({errors.Count} errors): {string.Join("; ", first)}{suffix}";
    }
}
```

---

### Task 21: Run exception tests, verify green

**Files:** none

- [ ] **Step 21.1: Build and test**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~ExceptionHierarchyTests"`
Expected: All 7 tests pass.

---

### Task 22: Add property/constructor tests for exceptions

**Files:**
- Create: `tests/PipeFlow.Tests/ExceptionTests/ExceptionPropertiesTests.cs`

- [ ] **Step 22.1: Write property tests**

Create `tests/PipeFlow.Tests/ExceptionTests/ExceptionPropertiesTests.cs`:
```csharp
using PipeFlow.Exceptions;

namespace PipeFlow.Tests.ExceptionTests;

public class ExceptionPropertiesTests
{
    [Fact]
    public void SourceException_Full_SetsAllProperties()
    {
        var inner = new InvalidOperationException("underlying");
        var ex = new PipeFlowSourceException("Csv", "input.csv", "parse failure", inner, rowNumber: 42);

        ex.SourceType.Should().Be("Csv");
        ex.Location.Should().Be("input.csv");
        ex.RowNumber.Should().Be(42);
        ex.Message.Should().Be("parse failure");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void SourceException_Short_SetsSourceTypeAndMessage()
    {
        var ex = new PipeFlowSourceException("Csv", "something went wrong");

        ex.SourceType.Should().Be("Csv");
        ex.Location.Should().BeNull();
        ex.RowNumber.Should().BeNull();
        ex.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void SinkException_SetsProperties()
    {
        var ex = new PipeFlowSinkException("SqlServer", "my-db", "constraint failed");

        ex.SinkType.Should().Be("SqlServer");
        ex.Location.Should().Be("my-db");
    }

    [Fact]
    public void ConfigurationException_WithOptionName_SetsProperty()
    {
        var ex = new PipeFlowConfigurationException("Delimiter", "invalid delimiter");

        ex.OptionName.Should().Be("Delimiter");
        ex.Message.Should().Be("invalid delimiter");
    }

    [Fact]
    public void ValidationException_SingleError_HasSingleErrorMessage()
    {
        var errors = new[] { new ValidationError("Email", "is required") };
        var ex = new PipeFlowValidationException(errors);

        ex.Errors.Should().ContainSingle();
        ex.Message.Should().Contain("Email");
        ex.Message.Should().Contain("is required");
    }

    [Fact]
    public void ValidationException_ManyErrors_MessageSummarizes()
    {
        var errors = Enumerable.Range(0, 10)
            .Select(i => new ValidationError($"Col{i}", "fail"))
            .ToArray();
        var ex = new PipeFlowValidationException(errors);

        ex.Errors.Should().HaveCount(10);
        ex.Message.Should().Contain("10 errors");
        ex.Message.Should().Contain("+ 7 more");
    }
}
```

- [ ] **Step 22.2: Run all exception tests**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~ExceptionTests"`
Expected: All tests pass (13 total).

- [ ] **Step 22.3: Commit exception work**

Run:
```bash
git add src tests
git commit -m "feat: exception hierarchy for PipeFlow

- PipeFlowException: abstract base for all library exceptions
- PipeFlowSourceException: source read failures (with SourceType/Location/RowNumber)
- PipeFlowSinkException: sink write failures (with SinkType/Location)
- PipeFlowConfigurationException: invalid options/configuration (with OptionName)
- PipeFlowValidationException: validation failures (with IReadOnlyList<ValidationError>)
- ValidationError: structured per-row-per-column error record

13 tests; all green. Closes v2 issue: errors wrapped as raw Exception losing type info."
```

---

## Chunk 3: `DataRow` (TDD)

### Task 23: Write failing construction & indexer tests

**Files:**
- Create: `tests/PipeFlow.Tests/DataRowTests/ConstructionTests.cs`
- Create: `tests/PipeFlow.Tests/DataRowTests/IndexerTests.cs`

- [ ] **Step 23.1: Write construction tests**

Create `tests/PipeFlow.Tests/DataRowTests/ConstructionTests.cs`:
```csharp
namespace PipeFlow.Tests.DataRowTests;

public class ConstructionTests
{
    [Fact]
    public void DefaultConstructor_CreatesEmptyRow()
    {
        var row = new DataRow();

        row.ColumnCount.Should().Be(0);
        row.Columns.Should().BeEmpty();
    }

    [Fact]
    public void CapacityConstructor_InitialCapacityDoesNotAddColumns()
    {
        var row = new DataRow(capacity: 32);

        row.ColumnCount.Should().Be(0);
    }

    [Fact]
    public void KeyValuePairConstructor_PopulatesFromSource()
    {
        var source = new Dictionary<string, object?>
        {
            ["Name"] = "Alice",
            ["Age"] = 30,
            ["Active"] = true
        };

        var row = new DataRow(source);

        row.ColumnCount.Should().Be(3);
        row["Name"].Should().Be("Alice");
        row["Age"].Should().Be(30);
        row["Active"].Should().Be(true);
    }

    [Fact]
    public void KeyValuePairConstructor_PreservesInsertionOrder()
    {
        var source = new[]
        {
            new KeyValuePair<string, object?>("First", 1),
            new KeyValuePair<string, object?>("Second", 2),
            new KeyValuePair<string, object?>("Third", 3),
        };

        var row = new DataRow(source);

        row.Columns.Should().Equal("First", "Second", "Third");
    }
}
```

- [ ] **Step 23.2: Write indexer tests**

Create `tests/PipeFlow.Tests/DataRowTests/IndexerTests.cs`:
```csharp
namespace PipeFlow.Tests.DataRowTests;

public class IndexerTests
{
    [Fact]
    public void Indexer_Get_MissingColumn_ReturnsNull()
    {
        // v2 threw KeyNotFoundException; v3 returns null
        var row = new DataRow();

        row["NonExistent"].Should().BeNull();
    }

    [Fact]
    public void Indexer_Set_AppendsNewColumnToOrder()
    {
        var row = new DataRow();

        row["First"] = 1;
        row["Second"] = 2;

        row.Columns.Should().Equal("First", "Second");
    }

    [Fact]
    public void Indexer_Set_ExistingColumn_UpdatesValueWithoutReordering()
    {
        var row = new DataRow { ["A"] = 1, ["B"] = 2 };

        row["A"] = 10;

        row.Columns.Should().Equal("A", "B");
        row["A"].Should().Be(10);
    }

    [Fact]
    public void Indexer_IsCaseInsensitive()
    {
        var row = new DataRow { ["Name"] = "Alice" };

        row["name"].Should().Be("Alice");
        row["NAME"].Should().Be("Alice");
        row["NaMe"].Should().Be("Alice");
    }

    [Fact]
    public void IntegerIndexer_Get_ReturnsValueAtColumnIndex()
    {
        var row = new DataRow { ["A"] = 1, ["B"] = 2, ["C"] = 3 };

        row[0].Should().Be(1);
        row[1].Should().Be(2);
        row[2].Should().Be(3);
    }

    [Fact]
    public void IntegerIndexer_Get_OutOfRange_Throws()
    {
        var row = new DataRow { ["A"] = 1 };

        var act = () => _ = row[5];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IntegerIndexer_Set_OutOfRange_Throws()
    {
        var row = new DataRow { ["A"] = 1 };

        var act = () => row[5] = 99;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ContainsColumn_IsCaseInsensitive()
    {
        var row = new DataRow { ["Name"] = "Alice" };

        row.ContainsColumn("name").Should().BeTrue();
        row.ContainsColumn("NAME").Should().BeTrue();
        row.ContainsColumn("Missing").Should().BeFalse();
    }

    [Fact]
    public void Remove_ExistingColumn_ReturnsTrue_AndRemovesFromOrder()
    {
        var row = new DataRow { ["A"] = 1, ["B"] = 2, ["C"] = 3 };

        var removed = row.Remove("B");

        removed.Should().BeTrue();
        row.ColumnCount.Should().Be(2);
        row.Columns.Should().Equal("A", "C");
        row.ContainsColumn("B").Should().BeFalse();
    }

    [Fact]
    public void Remove_MissingColumn_ReturnsFalse()
    {
        var row = new DataRow { ["A"] = 1 };

        row.Remove("Missing").Should().BeFalse();
    }

    [Fact]
    public void Remove_IsCaseInsensitive()
    {
        var row = new DataRow { ["Name"] = "Alice" };

        row.Remove("name").Should().BeTrue();
        row.ContainsColumn("Name").Should().BeFalse();
    }
}
```

- [ ] **Step 23.3: Run tests, expect failure**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests.ConstructionTests|FullyQualifiedName~DataRowTests.IndexerTests"`
Expected: Compilation error — `DataRow` does not exist yet.

---

### Task 24: Implement minimal `DataRow` (no equality yet)

**Files:**
- Create: `src/PipeFlow/DataRow.cs`

- [ ] **Step 24.1: Write minimal implementation**

Create `src/PipeFlow/DataRow.cs`:
```csharp
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace PipeFlow;

/// <summary>
/// A case-insensitive, order-preserving bag of named column values. Used as the
/// generic record type for source-agnostic pipeline operations.
/// </summary>
/// <remarks>
/// v3 changes versus v2:
/// <list type="bullet">
///   <item>Getter returns <c>null</c> on missing column (v2 threw).</item>
///   <item>Structural <see cref="IEquatable{T}"/>; <c>Distinct()</c>/<c>GroupBy</c> work.</item>
///   <item>Type conversion uses <see cref="CultureInfo.InvariantCulture"/>.</item>
/// </list>
/// </remarks>
public sealed class DataRow : IReadOnlyDictionary<string, object?>, IEquatable<DataRow>
{
    private readonly Dictionary<string, object?> _data;
    private readonly List<string> _columnOrder;
    private int? _cachedHashCode;

    public DataRow() : this(capacity: 16) { }

    public DataRow(int capacity)
    {
        _data = new Dictionary<string, object?>(capacity, StringComparer.OrdinalIgnoreCase);
        _columnOrder = new List<string>(capacity);
    }

    public DataRow(IEnumerable<KeyValuePair<string, object?>> source) : this()
    {
        ArgumentNullException.ThrowIfNull(source);
        foreach (var kvp in source)
            this[kvp.Key] = kvp.Value;
    }

    public object? this[string columnName]
    {
        get => _data.TryGetValue(columnName, out var value) ? value : null;
        set
        {
            if (!_data.ContainsKey(columnName))
                _columnOrder.Add(columnName);
            _data[columnName] = value;
            _cachedHashCode = null;
        }
    }

    public object? this[int columnIndex]
    {
        get
        {
            if ((uint)columnIndex >= (uint)_columnOrder.Count)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));
            return _data[_columnOrder[columnIndex]];
        }
        set
        {
            if ((uint)columnIndex >= (uint)_columnOrder.Count)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));
            _data[_columnOrder[columnIndex]] = value;
            _cachedHashCode = null;
        }
    }

    public int ColumnCount => _columnOrder.Count;
    public IEnumerable<string> Columns => _columnOrder;

    public bool ContainsColumn(string columnName) => _data.ContainsKey(columnName);

    public bool Remove(string columnName)
    {
        if (!_data.Remove(columnName))
            return false;
        _columnOrder.RemoveAll(c => c.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        _cachedHashCode = null;
        return true;
    }

    public DataRow Clone()
    {
        var clone = new DataRow(_columnOrder.Count);
        foreach (var col in _columnOrder)
            clone[col] = _data[col];
        return clone;
    }

    // IReadOnlyDictionary<string, object?> members
    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => _columnOrder;
    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values
    {
        get
        {
            foreach (var col in _columnOrder)
                yield return _data[col];
        }
    }
    int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => _data.Count;
    bool IReadOnlyDictionary<string, object?>.ContainsKey(string key) => _data.ContainsKey(key);
    bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
        => _data.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        foreach (var col in _columnOrder)
            yield return new KeyValuePair<string, object?>(col, _data[col]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Equality — implemented in Task 26
    public bool Equals(DataRow? other) => throw new NotImplementedException();
    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}
```

**Note:** `Equals`/`GetHashCode` deliberately throw here — Task 26 implements them red→green.

- [ ] **Step 24.2: Run construction & indexer tests**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests.ConstructionTests|FullyQualifiedName~DataRowTests.IndexerTests"`
Expected: All tests pass (construction 4 + indexer 9 = 13 tests green).

---

### Task 25: Write failing `GetValue<T>` tests, then implement

**Files:**
- Create: `tests/PipeFlow.Tests/DataRowTests/GetValueTests.cs`
- Modify: `src/PipeFlow/DataRow.cs`

- [ ] **Step 25.1: Write tests**

Create `tests/PipeFlow.Tests/DataRowTests/GetValueTests.cs`:
```csharp
using System.Globalization;

namespace PipeFlow.Tests.DataRowTests;

public class GetValueTests
{
    [Fact]
    public void GetValue_DirectMatch_ReturnsTypedValue()
    {
        var row = new DataRow { ["Count"] = 42 };

        row.GetValue<int>("Count").Should().Be(42);
    }

    [Fact]
    public void GetValue_StringToInt_ConvertsUsingInvariantCulture()
    {
        var row = new DataRow { ["N"] = "1234" };

        row.GetValue<int>("N").Should().Be(1234);
    }

    [Fact]
    public void GetValue_DecimalFormat_UsesInvariantCulture()
    {
        // Using a period as decimal separator — must work regardless of machine culture
        var row = new DataRow { ["Price"] = "19.99" };

        row.GetValue<decimal>("Price").Should().Be(19.99m);
    }

    [Fact]
    public void GetValue_MissingColumn_ReturnsDefault()
    {
        var row = new DataRow();

        row.GetValue<int>("NonExistent").Should().Be(0);
        row.GetValue<string?>("NonExistent").Should().BeNull();
    }

    [Fact]
    public void GetValue_NullValue_ReturnsDefault()
    {
        var row = new DataRow { ["X"] = null };

        row.GetValue<int>("X").Should().Be(0);
        row.GetValue<string?>("X").Should().BeNull();
    }

    [Fact]
    public void TryGetValue_ValidConversion_ReturnsTrueAndValue()
    {
        var row = new DataRow { ["Age"] = "30" };

        row.TryGetValue<int>("Age", out var age).Should().BeTrue();
        age.Should().Be(30);
    }

    [Fact]
    public void TryGetValue_InvalidConversion_ReturnsFalse()
    {
        var row = new DataRow { ["Age"] = "not a number" };

        row.TryGetValue<int>("Age", out var age).Should().BeFalse();
        age.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_MissingColumn_ReturnsFalse()
    {
        var row = new DataRow();

        row.TryGetValue<int>("Missing", out var _).Should().BeFalse();
    }

    [Fact]
    public void GetValue_NullableInt_MissingReturnsNull()
    {
        // Regression: Convert.ChangeType doesn't handle Nullable<> directly;
        // implementation must unwrap via Nullable.GetUnderlyingType.
        var row = new DataRow();

        row.GetValue<int?>("Missing").Should().BeNull();
    }

    [Fact]
    public void GetValue_NullableInt_ConvertsString()
    {
        var row = new DataRow { ["Age"] = "42" };

        row.GetValue<int?>("Age").Should().Be(42);
    }

    [Fact]
    public void GetValue_NullableDateTime_ConvertsIsoString()
    {
        var row = new DataRow { ["When"] = "2026-01-15" };

        var when = row.GetValue<DateTime?>("When");

        when.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public void TryGetValue_NullableInt_SucceedsOnStringValue()
    {
        var row = new DataRow { ["N"] = "100" };

        row.TryGetValue<int?>("N", out var n).Should().BeTrue();
        n.Should().Be(100);
    }
}
```

- [ ] **Step 25.2: Run tests, expect failure (methods don't exist)**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests.GetValueTests"`
Expected: Compilation error — `GetValue<T>`/`TryGetValue<T>` not defined.

- [ ] **Step 25.3: Add implementations to DataRow**

Edit `src/PipeFlow/DataRow.cs` — add these methods before the IReadOnlyDictionary region:

```csharp
public T? GetValue<T>(string columnName)
{
    if (!_data.TryGetValue(columnName, out var value) || value is null)
        return default;

    if (value is T typed)
        return typed;

    // Unwrap Nullable<T> — Convert.ChangeType doesn't handle Nullable<> directly.
    var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

    try
    {
        return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
    catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
    {
        return default;
    }
}

public bool TryGetValue<T>(string columnName, out T? value)
{
    value = default;

    if (!_data.TryGetValue(columnName, out var raw) || raw is null)
        return false;

    if (raw is T typed)
    {
        value = typed;
        return true;
    }

    var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

    try
    {
        value = (T)Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
        return true;
    }
    catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
    {
        return false;
    }
}
```

**Important notes:**
- `GetValue<T>` on missing/null returns `default(T)` without throwing — this is the v3 contract per spec §6.4.
- `TryGetValue<T>` distinguishes "missing or null" (false) from "successfully converted" (true).
- **`Nullable<T>` handling:** `Convert.ChangeType(value, typeof(int?), ...)` throws `InvalidCastException` — `Convert.ChangeType` can't produce `Nullable<>` directly. We unwrap via `Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)` so callers can safely do `row.GetValue<int?>("Age")` and `row.GetValue<DateTime?>("When")`. The box-to-T cast at the return site then works because C# auto-wraps to `Nullable<T>` when assigning a non-null underlying value.

- [ ] **Step 25.4: Run GetValue tests, verify green**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests.GetValueTests"`
Expected: All 12 tests pass (8 original + 4 Nullable<T> cases).

---

### Task 26: Write failing equality tests, then implement

**Files:**
- Create: `tests/PipeFlow.Tests/DataRowTests/EqualityTests.cs`
- Modify: `src/PipeFlow/DataRow.cs`

- [ ] **Step 26.1: Write equality tests**

Create `tests/PipeFlow.Tests/DataRowTests/EqualityTests.cs`:
```csharp
namespace PipeFlow.Tests.DataRowTests;

public class EqualityTests
{
    [Fact]
    public void Equals_IdenticalContent_ReturnsTrue()
    {
        var a = new DataRow { ["Name"] = "Alice", ["Age"] = 30 };
        var b = new DataRow { ["Name"] = "Alice", ["Age"] = 30 };

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentOrderSameContent_ReturnsTrue()
    {
        var a = new DataRow { ["Name"] = "Alice", ["Age"] = 30 };
        var b = new DataRow { ["Age"] = 30, ["Name"] = "Alice" };

        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentCaseKeys_ReturnsTrue()
    {
        var a = new DataRow { ["Name"] = "Alice" };
        var b = new DataRow { ["NAME"] = "Alice" };

        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new DataRow { ["Name"] = "Alice" };
        var b = new DataRow { ["Name"] = "Bob" };

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentColumnSets_ReturnsFalse()
    {
        var a = new DataRow { ["Name"] = "Alice" };
        var b = new DataRow { ["Name"] = "Alice", ["Age"] = 30 };

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new DataRow { ["X"] = 1 };

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var a = new DataRow { ["X"] = 1 };
        object other = "not a DataRow";

        a.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_IsCached()
    {
        var a = new DataRow { ["X"] = 1, ["Y"] = 2 };
        var first = a.GetHashCode();
        var second = a.GetHashCode();

        first.Should().Be(second);
    }

    [Fact]
    public void GetHashCode_ChangesAfterMutation()
    {
        var a = new DataRow { ["X"] = 1 };
        var before = a.GetHashCode();

        a["X"] = 2;
        var after = a.GetHashCode();

        after.Should().NotBe(before);  // hash invalidation works
    }

    [Fact]
    public void HashSet_OfDataRows_DeduplicatesStructurally()
    {
        // v2 bug: Distinct broken because DataRow lacked IEquatable
        var set = new HashSet<DataRow>
        {
            new DataRow { ["N"] = 1 },
            new DataRow { ["N"] = 1 },   // duplicate
            new DataRow { ["N"] = 2 },
        };

        set.Should().HaveCount(2);
    }
}
```

- [ ] **Step 26.2: Run tests, expect failure (Equals throws NotImplementedException)**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests.EqualityTests"`
Expected: Tests fail with NotImplementedException.

- [ ] **Step 26.3: Replace equality methods in DataRow**

Edit `src/PipeFlow/DataRow.cs` — replace the three `NotImplementedException`-throwing equality methods with:

```csharp
public bool Equals(DataRow? other)
{
    if (other is null)
        return false;
    if (ReferenceEquals(this, other))
        return true;
    if (_data.Count != other._data.Count)
        return false;

    foreach (var kvp in _data)
    {
        if (!other._data.TryGetValue(kvp.Key, out var otherValue))
            return false;
        if (!Equals(kvp.Value, otherValue))
            return false;
    }
    return true;
}

public override bool Equals(object? obj) => obj is DataRow other && Equals(other);

public override int GetHashCode()
{
    if (_cachedHashCode is int cached)
        return cached;

    var hash = new HashCode();
    // Order-independent: sort keys by OrdinalIgnoreCase so identical content
    // with different insertion order produces equal hashes.
    foreach (var key in _columnOrder
        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
    {
        hash.Add(key, StringComparer.OrdinalIgnoreCase);
        hash.Add(_data[key]);
    }

    var result = hash.ToHashCode();
    _cachedHashCode = result;
    return result;
}

public static bool operator ==(DataRow? left, DataRow? right)
    => left is null ? right is null : left.Equals(right);

public static bool operator !=(DataRow? left, DataRow? right) => !(left == right);
```

- [ ] **Step 26.4: Run equality tests, verify green**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests.EqualityTests"`
Expected: All 10 tests pass.

---

### Task 27: Write clone tests, verify implementation

**Files:**
- Create: `tests/PipeFlow.Tests/DataRowTests/CloneTests.cs`

- [ ] **Step 27.1: Write tests**

Create `tests/PipeFlow.Tests/DataRowTests/CloneTests.cs`:
```csharp
namespace PipeFlow.Tests.DataRowTests;

public class CloneTests
{
    [Fact]
    public void Clone_ProducesEqualButDistinctInstance()
    {
        var original = new DataRow { ["A"] = 1, ["B"] = "two" };

        var cloned = original.Clone();

        cloned.Should().NotBeSameAs(original);
        cloned.Should().Be(original);
    }

    [Fact]
    public void Clone_IsIndependent_MutatingCloneDoesNotAffectOriginal()
    {
        var original = new DataRow { ["A"] = 1 };

        var cloned = original.Clone();
        cloned["A"] = 99;
        cloned["B"] = "new";

        original["A"].Should().Be(1);
        original.ContainsColumn("B").Should().BeFalse();
    }

    [Fact]
    public void Clone_PreservesColumnOrder()
    {
        var original = new DataRow { ["Z"] = 1, ["A"] = 2, ["M"] = 3 };

        var cloned = original.Clone();

        cloned.Columns.Should().Equal("Z", "A", "M");
    }
}
```

- [ ] **Step 27.2: Run tests, verify green**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests.CloneTests"`
Expected: All 3 tests pass.

---

### Task 28: Run all DataRow tests, commit

**Files:** none

- [ ] **Step 28.1: Run full DataRow suite**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~DataRowTests"`
Expected: All DataRow tests pass (4 construction + 11 indexer + 12 getvalue + 10 equality + 3 clone = 40 tests).

- [ ] **Step 28.2: Commit**

Run:
```bash
git add src tests
git commit -m "feat: DataRow with structural equality and null-friendly getters

- Case-insensitive keys with ordered column list
- Null return on missing column (v2 threw KeyNotFoundException)
- GetValue<T>/TryGetValue<T>: InvariantCulture-aware Convert.ChangeType
- IEquatable<DataRow>: structural, order-independent
- GetHashCode: cached, invalidated on mutation
- Clone: defensive deep copy

40 tests; all green. Closes v2 issues #12 (no IEquatable/GetHashCode broke
Distinct/GroupBy) and #16 (culture-sensitive type conversion). Nullable<T>
conversion (int?/DateTime?) works correctly via Nullable.GetUnderlyingType
unwrap before Convert.ChangeType."
```

---

## Chunk 4: Abstractions (Interfaces + PipelineContext)

### Task 29: `IPipelineSource<T>` and `IPipelineSink<T>`

**Files:**
- Create: `src/PipeFlow/Abstractions/IPipelineSource.cs`
- Create: `src/PipeFlow/Abstractions/IPipelineSink.cs`

- [ ] **Step 29.1: Write IPipelineSource**

Create `src/PipeFlow/Abstractions/IPipelineSource.cs`:
```csharp
namespace PipeFlow.Abstractions;

/// <summary>
/// A source of items for a pipeline. Implementations own the lifecycle of any
/// I/O resources (files, connections) via the enumerator returned from
/// <see cref="ReadAsync"/>; opening/closing happens as the consumer enumerates.
/// </summary>
/// <typeparam name="T">Row/record type produced by the source.</typeparam>
public interface IPipelineSource<out T>
{
    /// <summary>
    /// Produce items for the pipeline. The returned sequence is typically read once.
    /// </summary>
    IAsyncEnumerable<T> ReadAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 29.2: Write IPipelineSink**

Create `src/PipeFlow/Abstractions/IPipelineSink.cs`:
```csharp
namespace PipeFlow.Abstractions;

/// <summary>
/// A consumer for a pipeline's output. Implementations enumerate the source
/// stream exactly once and are responsible for their own flushing/disposal.
/// </summary>
/// <typeparam name="T">Row/record type consumed by the sink.</typeparam>
public interface IPipelineSink<in T>
{
    /// <summary>
    /// Consume items from the pipeline. Must drain <paramref name="source"/> to completion
    /// (respecting <paramref name="cancellationToken"/>) before the returned task completes.
    /// </summary>
    Task WriteAsync(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default);
}
```

---

### Task 30: `PipelineContext`

**Files:**
- Create: `src/PipeFlow/PipelineContext.cs`

- [ ] **Step 30.1: Write PipelineContext**

Create `src/PipeFlow/PipelineContext.cs`:
```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PipeFlow;

/// <summary>
/// Cross-cutting state carried by every pipeline operation. Includes logger,
/// options, HTTP factory, service provider, cancellation, and parallelism hint.
/// Immutable — operations that need to change the context produce a new one.
/// </summary>
/// <remarks>
/// Spec §6.3 declares <see cref="HttpClientFactory"/> and <see cref="Services"/> as positional
/// nullable parameters without defaults; this implementation adds <c>= null</c> defaults so
/// <see cref="Empty"/> and the static <c>PipeFlow.From</c> facade can construct a context with
/// only the required logger/options. Types are unchanged (<c>IHttpClientFactory?</c>, <c>IServiceProvider?</c>);
/// callers that supplied all six positional args continue to compile.
/// </remarks>
public readonly record struct PipelineContext(
    ILogger Logger,
    PipeFlowOptions Options,
    IHttpClientFactory? HttpClientFactory = null,
    IServiceProvider? Services = null,
    CancellationToken CancellationToken = default,
    int? MaxDegreeOfParallelism = null)
{
    /// <summary>
    /// A minimal context with <see cref="NullLogger.Instance"/> and default options. Suitable
    /// for tests and the static <c>PipeFlow.From</c> facade. Not in spec §6.3 but a pragmatic
    /// factory for zero-config entry points.
    /// </summary>
    public static PipelineContext Empty { get; } = new(NullLogger.Instance, new PipeFlowOptions());
}
```

**Note:** `PipeFlowOptions` is referenced but not yet defined. Add a placeholder to make compilation succeed:

- [ ] **Step 30.2: Write placeholder PipeFlowOptions**

Create `src/PipeFlow/PipeFlowOptions.cs`:
```csharp
namespace PipeFlow;

/// <summary>
/// Library-wide options. Full surface lands in Wave 0.B when the builder and DI
/// extensions come online; this stub exists only so <see cref="PipelineContext"/>
/// compiles.
/// </summary>
public sealed class PipeFlowOptions
{
    // Empty for now — full surface defined in spec §7.6, implemented in Wave 0.B.
}
```

---

### Task 31: Pipeline interfaces (contracts only)

**Files:**
- Create: `src/PipeFlow/IPipeline.cs`
- Create: `src/PipeFlow/IOrderedPipeline.cs`
- Create: `src/PipeFlow/IQueryablePipeline.cs`
- Create: `src/PipeFlow/IOrderedQueryablePipeline.cs`

- [ ] **Step 31.1: Write IPipeline<T>**

Create `src/PipeFlow/IPipeline.cs`:
```csharp
using PipeFlow.Abstractions;

namespace PipeFlow;

/// <summary>
/// A lazy, composable pipeline over <typeparamref name="T"/>. All I/O is async-first;
/// transformations are sync LINQ-style. Enumeration is driven by a terminal operation.
/// </summary>
/// <typeparam name="T">Row/record type flowing through the pipeline.</typeparam>
public interface IPipeline<T>
{
    /// <summary>Cross-cutting state (logger, options, CT, services).</summary>
    PipelineContext Context { get; }

    // Composition — lazy

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

    IPipeline<IGrouping<TKey, T>> GroupBy<TKey>(
        Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null);

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
}
```

- [ ] **Step 31.2: Write IOrderedPipeline<T>**

Create `src/PipeFlow/IOrderedPipeline.cs`:
```csharp
namespace PipeFlow;

/// <summary>
/// Represents a pipeline whose items have been sorted. Enables <c>ThenBy</c>/<c>ThenByDescending</c>.
/// </summary>
public interface IOrderedPipeline<T> : IPipeline<T>
{
    IOrderedPipeline<T> ThenBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null);
    IOrderedPipeline<T> ThenByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null);
}
```

- [ ] **Step 31.3: Write IQueryablePipeline<T>**

Create `src/PipeFlow/IQueryablePipeline.cs`:
```csharp
using System.Linq.Expressions;

namespace PipeFlow;

/// <summary>
/// Specialized pipeline for <see cref="IQueryable{T}"/>-backed sources (Entity Framework Core).
/// Accepts <see cref="Expression{TDelegate}"/> overloads of <c>Where</c>/<c>Select</c>/<c>OrderBy</c>
/// so operations translate to SQL instead of client-side filtering. Enumeration is deferred
/// until a terminal call.
/// </summary>
/// <remarks>
/// Calling <see cref="IPipeline{T}.Chunk"/> on a queryable pipeline forces client-side
/// materialization (chunk has no SQL translation) and returns a non-queryable
/// <see cref="IPipeline{T}"/>.
/// </remarks>
public interface IQueryablePipeline<T> : IPipeline<T>
{
    IQueryablePipeline<T> Where(Expression<Func<T, bool>> predicate);
    IQueryablePipeline<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    IOrderedQueryablePipeline<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
    IOrderedQueryablePipeline<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>Enable keyset-style server-side paging with the given page size.</summary>
    IQueryablePipeline<T> WithPaging(int pageSize);

    /// <summary>Entity Framework no-tracking hint for read-only queries.</summary>
    IQueryablePipeline<T> AsNoTracking();
}
```

- [ ] **Step 31.4: Write IOrderedQueryablePipeline<T>**

Create `src/PipeFlow/IOrderedQueryablePipeline.cs`:
```csharp
using System.Linq.Expressions;

namespace PipeFlow;

/// <summary>
/// A queryable pipeline that has been ordered and can accept additional
/// <c>ThenBy</c>/<c>ThenByDescending</c> clauses.
/// </summary>
public interface IOrderedQueryablePipeline<T> : IQueryablePipeline<T>, IOrderedPipeline<T>
{
    IOrderedQueryablePipeline<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);
    IOrderedQueryablePipeline<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
}
```

---

### Task 32: Interface shape smoke tests

**Files:**
- Create: `tests/PipeFlow.Tests/Abstractions/InterfaceShapeTests.cs`

- [ ] **Step 32.1: Write tests**

Create `tests/PipeFlow.Tests/Abstractions/InterfaceShapeTests.cs`:
```csharp
namespace PipeFlow.Tests.Abstractions;

public class InterfaceShapeTests
{
    [Fact]
    public void IPipeline_IsGeneric()
    {
        typeof(IPipeline<>).IsGenericTypeDefinition.Should().BeTrue();
    }

    [Fact]
    public void IPipelineSource_IsCovariant()
    {
        // out T variance
        var sourceParam = typeof(IPipelineSource<>).GetGenericArguments()[0];
        sourceParam.GenericParameterAttributes
            .HasFlag(System.Reflection.GenericParameterAttributes.Covariant)
            .Should().BeTrue();
    }

    [Fact]
    public void IPipelineSink_IsContravariant()
    {
        // in T variance
        var sinkParam = typeof(IPipelineSink<>).GetGenericArguments()[0];
        sinkParam.GenericParameterAttributes
            .HasFlag(System.Reflection.GenericParameterAttributes.Contravariant)
            .Should().BeTrue();
    }

    [Fact]
    public void IOrderedPipeline_ExtendsIPipeline()
    {
        typeof(IOrderedPipeline<>).GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipeline<>));
    }

    [Fact]
    public void IQueryablePipeline_ExtendsIPipeline()
    {
        typeof(IQueryablePipeline<>).GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipeline<>));
    }

    [Fact]
    public void IOrderedQueryablePipeline_ExtendsBothQueryableAndOrdered()
    {
        var interfaces = typeof(IOrderedQueryablePipeline<>).GetInterfaces();
        interfaces.Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryablePipeline<>));
        interfaces.Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOrderedPipeline<>));
    }

    [Fact]
    public void PipelineContext_IsReadOnlyStruct()
    {
        typeof(PipelineContext).IsValueType.Should().BeTrue();
        typeof(PipelineContext).GetCustomAttributes(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), inherit: false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void PipelineContext_Empty_IsValid()
    {
        var empty = PipelineContext.Empty;

        empty.Logger.Should().NotBeNull();
        empty.Options.Should().NotBeNull();
        empty.HttpClientFactory.Should().BeNull();
        empty.Services.Should().BeNull();
        empty.CancellationToken.Should().Be(CancellationToken.None);
    }
}
```

- [ ] **Step 32.2: Run tests**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj --filter "FullyQualifiedName~Abstractions.InterfaceShapeTests"`
Expected: All 8 tests pass.

---

### Task 33: Commit abstractions

**Files:** none

- [ ] **Step 33.1: Run full test suite**

Run: `dotnet test tests/PipeFlow.Tests/PipeFlow.Tests.csproj`
Expected: All tests pass (7 hierarchy + 6 properties + 40 DataRow + 8 interface shape = 61 tests).

- [ ] **Step 33.2: Commit**

Run:
```bash
git add src tests
git commit -m "feat: core pipeline abstractions and PipelineContext

- IPipelineSource<out T> / IPipelineSink<in T>: variance-correct source/sink
- IPipeline<T>: full contract (Where/Select/Chunk/AsParallel/WithCancellation/terminals)
- IOrderedPipeline<T>: ThenBy/ThenByDescending after OrderBy
- IQueryablePipeline<T>: Expression<...> overloads for EF Core translation
- IOrderedQueryablePipeline<T>: combined queryable+ordered with Expression ThenBy
- PipelineContext: public readonly record struct carrying Logger/Options/HTTP/CT/services
- PipeFlowOptions: placeholder stub (full in Wave 0.B)

Contract-only — no implementation of IPipeline<T> yet (that is Pipeline<T> in Wave 0.B).
Closes v2 issue #9 (EF Core Expression-vs-Func) via IQueryablePipeline<T>."
```

---

## Chunk 5: CI + Green Build

### Task 34: Write new `build.yml`

**Files:**
- Overwrite: `.github/workflows/build.yml`

- [ ] **Step 34.1: Overwrite build workflow**

Write `.github/workflows/build.yml`:
```yaml
name: Build & Test

on:
  push:
    branches: [main]
    paths-ignore:
      - '**.md'
      - 'docs/**'
      - 'LICENSE'
  pull_request:
    branches: [main]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  build:
    name: Build on ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    runs-on: ${{ matrix.os }}
    timeout-minutes: 30

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            10.0.x
          cache: true
          cache-dependency-path: '**/*.csproj'

      - name: Tool restore
        run: dotnet tool restore

      - name: Restore
        run: dotnet restore

      - name: Verify formatting
        run: dotnet format --verify-no-changes --severity warn
        continue-on-error: true  # warn-level for first pass; tighten later

      - name: Build
        run: dotnet build -c Release --no-restore

      - name: Test
        run: dotnet test -c Release --no-build --logger "trx;LogFileName=test-results.trx" --results-directory TestResults/

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results-${{ matrix.os }}
          path: TestResults/**/*.trx
          retention-days: 7

      - name: Coverage
        if: matrix.os == 'ubuntu-latest'
        run: dotnet test -c Release --no-build --collect:"XPlat Code Coverage" --results-directory coverage/

      # NOTE: CODECOV_TOKEN secret is OPTIONAL. On a fresh repo before Codecov
      # is wired up, this step emits a warning but does not fail the build
      # (fail_ci_if_error: false). Once you add the secret via
      # Settings > Secrets and variables > Actions, the warning goes away.
      - name: Upload coverage
        if: matrix.os == 'ubuntu-latest'
        uses: codecov/codecov-action@v4
        with:
          files: coverage/**/coverage.cobertura.xml
          fail_ci_if_error: false
          verbose: true
        env:
          CODECOV_TOKEN: ${{ secrets.CODECOV_TOKEN }}
```

---

### Task 35: Deactivate v2 workflows temporarily

**Files:**
- Modify: `.github/workflows/release.yml`
- Modify: `.github/workflows/dependency-check.yml`

The v2 release workflow references `PipeFlow/PipeFlow.csproj` which no longer exists. Disable these until Wave 4 rewrites them.

- [ ] **Step 35.1: Disable release.yml**

Prepend the existing content of `.github/workflows/release.yml` with a trigger condition that prevents it from firing, and leave the rest as-is for reference:

Edit `.github/workflows/release.yml` — change the `on:` block at the top to:
```yaml
name: Release (DISABLED — will be rewritten in Wave 4)

on:
  workflow_dispatch:  # manual only; no automatic triggers
```

Keep the rest of the file untouched so Wave 4 can re-enable / rewrite it.

- [ ] **Step 35.2: Disable dependency-check.yml similarly**

Edit `.github/workflows/dependency-check.yml` — change the top:
```yaml
name: Dependency Check (DISABLED — will be rewritten in Wave 4)

on:
  workflow_dispatch:
```

---

### Task 36: Final verification — green build locally

**Files:** none

- [ ] **Step 36.1: Clean and full rebuild**

Run:
```bash
dotnet clean
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
```

Expected: Build succeeds; all tests pass; no analyzer warnings above warning-as-error threshold.

- [ ] **Step 36.2: Format check**

Run: `dotnet format --verify-no-changes`
Expected: Exit 0. Fix any formatting diffs reported.

- [ ] **Step 36.3: Coverage check (local, optional)**

Run:
```bash
dotnet test -c Release --collect:"XPlat Code Coverage" --results-directory coverage/
```
Expected: Coverage reports generated. (No hard threshold in Wave 0.A — 85% target applies once Pipeline<T> lands in Wave 0.B.)

---

### Task 37: Commit CI + push

**Files:** none

- [ ] **Step 37.1: Commit CI changes**

Run:
```bash
git add .github/workflows/
git commit -m "ci: new build workflow; disable v2 release + dependency-check

- build.yml rewrite: ubuntu/windows/macos x net8/net10 matrix with NuGet cache
- Test result TRX artifacts + coverage upload (Codecov, non-blocking)
- Concurrency group cancels superseded runs
- release.yml and dependency-check.yml disabled (workflow_dispatch only)
  — will be rewritten in Wave 4 with NuGet signing + split-package publishing"
```

- [ ] **Step 37.2: Push to origin**

Run: `git push origin main`

- [ ] **Step 37.3: Verify GitHub Actions green**

Wait for `build.yml` to complete on GitHub. Expected: all three OS matrix jobs green.

If red: diagnose from the failing job's logs. Common issues:
- `xunit.v3 1.0.0` not yet on NuGet → fall back to `xunit` 2.x; update `Directory.Packages.props` and test project.
- `dotnet format --verify-no-changes` diffs → run `dotnet format` locally, commit, push.
- `net10.0` SDK not available on runner → GitHub Actions images update ~monthly; `actions/setup-dotnet@v4` with `10.0.x` should work, but if not, temporarily drop to `net9.0` and re-target once runners update.

---

### Task 38: Final Wave 0.A commit — mark complete

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 38.1: Update CHANGELOG**

Edit `CHANGELOG.md` — under `## [Unreleased]`, replace the Wave 0.A section with a finalized entry:

```markdown
## [Unreleased]

### Wave 0.A — Foundation ✅ Complete

- [x] Archive v2.1.0 as git tag `v2.1.0-archived`
- [x] Delete v2 code from working tree
- [x] Repository restructure: SLNX + Central Package Management + `Directory.Build.props`
- [x] Multi-target `net8.0` and `net10.0`
- [x] Core abstractions: `IPipelineSource<T>`, `IPipelineSink<T>`, `IPipeline<T>`, `IOrderedPipeline<T>`, `IQueryablePipeline<T>`, `IOrderedQueryablePipeline<T>`
- [x] `DataRow` with `IEquatable<DataRow>`, cached `GetHashCode`, null-friendly indexer, `InvariantCulture` type conversion
- [x] Structured exception hierarchy
- [x] `PipelineContext` (public readonly record struct) with Logger/Options/HTTP/CT/Services
- [x] GitHub Actions `build.yml`: ubuntu/windows/macos × net8/net10 matrix with NuGet cache
- [x] v2 `release.yml` and `dependency-check.yml` disabled pending Wave 4 rewrite

**Next: Wave 0.B — Pipeline Implementation** (Pipeline<T>, ParallelPipeline<T>, Builder, DI, Options)
```

- [ ] **Step 38.2: Commit CHANGELOG**

Run:
```bash
git add CHANGELOG.md
git commit -m "docs: mark Wave 0.A complete in CHANGELOG"
git push
```

---

## Success Criteria

- [ ] `git tag --list` includes `v2.1.0-archived`
- [ ] v2 code removed from working tree (no `PipeFlow/`, `PipeFlow.Tests/`, `PipeFlow.Benchmarks/`, `Examples/`)
- [ ] New structure in place: `src/PipeFlow/`, `tests/PipeFlow.Tests/`, build config at root
- [ ] `dotnet build -c Release` succeeds on both `net8.0` and `net10.0` TFMs
- [ ] `dotnet test -c Release` all pass (61 tests)
- [ ] `dotnet format --verify-no-changes` exits 0
- [ ] GitHub Actions `build.yml` green on main branch
- [ ] CHANGELOG marks Wave 0.A complete

---

## Review Notes

- Spec §6.4 defines `FromObject<T>`/`ToObject<T>` mapping methods on `DataRow` — **not implemented in this plan**. These land in Wave 0.B alongside Pipeline<T>, where reflection-based mapping is needed for user POCO types. Flagged for that plan.
- Spec §6.2 defines `ForEachAsync(Func<T, CT, ValueTask>)` on `IPipeline<T>`. The interface is declared but no implementation exists until Wave 0.B.
- Spec §9.4 calls out TDD as mandatory for `DataRow` equality, `CsvSource` parser, `Pipeline<T>` composition, `ParallelPipeline` Channel, and options binding. This plan covers `DataRow` equality (Task 26). The remaining four are in Wave 0.B / Wave 0.C.

---

## Revision History

| Version | Date | Notes |
|---------|------|-------|
| 0.1 | 2026-04-15 | Initial plan for Wave 0.A Foundation |
| 0.2 | 2026-04-15 | Plan-reviewer fixes: corrected `</package>` XML typo in Directory.Build.props; dropped deprecated `dotnet-format` tool install; replaced invalid `dotnet nuget list` with `dotnet package search` + concrete xUnit 2.x fallback; added Nullable<T> unwrap in `GetValue<T>` / `TryGetValue<T>`; added tests for integer-indexer-set OOB, case-insensitive Remove, Nullable<int>/Nullable<DateTime> conversions; removed `PipeFlow.Abstractions` global using from Chunk 2 (added in Chunk 4); added rationale comments for `PipeFlow.Exceptions` sub-namespace and `PipelineContext` default params; removed unused `System.Linq.Expressions` import; added `timeout-minutes: 30` to CI; documented optional Codecov token; flipped Success Criteria checkboxes to `[ ]` (aspirational, not pre-filled). Test count updated 55 → 61. |
| 0.3 | 2026-04-15 | Pass-2 review cosmetic fixes: updated stale test counts ("8 tests" → "12 tests" in Step 25.4; "34 tests" → "40 tests" in Step 28.2 commit body); removed stale `dotnet-format` references in file-structure comment (line 57) and Task 12 commit body. |
