# Project Structure

---

## Repository Layout

| Directory | Contents |
|-----------|----------|
| `src/` | Product libraries, analyzers, source generators, integrations, templates, and packaged tools |
| `tests/` | Unit, integration, snapshot, fixture, and test-application projects |
| `benchmarks/` | Performance benchmarks and profiling workloads |
| `examples/` | Sample applications and test projects |
| `tools/` | CI pipeline and developer utilities |
| `eng/` | Shared MSBuild imports, signing key, and engineering assets |
| `scripts/` | Local test and profiling scripts |

Project names below are relative to their category directory. Production projects live under `src/`, test projects under `tests/`, and benchmark projects under `benchmarks/` unless noted.

## Core Projects

| Project | Purpose |
|---------|---------|
| `TUnit.Core` | Public API, attributes, interfaces |
| `TUnit.Engine` | Test discovery & execution (reflection mode) |
| `TUnit.Core.SourceGenerator` | Compile-time test generation (source-gen mode) |
| `TUnit.Assertions` | Fluent assertion library |
| `TUnit.Assertions.SourceGenerator` | `[GenerateAssertion]` implementation |
| `TUnit.Analyzers` | Roslyn analyzers & code fixes |
| `TUnit.Analyzers.CodeFixers` | Code fix providers for analyzer diagnostics |
| `TUnit.Assertions.Analyzers` | Assertion-specific analyzers |
| `TUnit.Assertions.Analyzers.CodeFixers` | Code fixes for assertion analyzers |
| `TUnit.Assertions.FSharp` | F# assertion extensions |
| `TUnit.AspNetCore` | ASP.NET Core integration |
| `TUnit.AspNetCore.Analyzers` | ASP.NET Core-specific analyzers |
| `TUnit.Aspire` | Aspire integration |
| `TUnit.Logging.Microsoft` | Microsoft.Extensions.Logging integration (no ASP.NET Core dependency) |
| `TUnit.PropertyTesting` | Property-based testing |
| `TUnit.FsCheck` | F#-based property testing integration |
| `TUnit.Playwright` | Browser testing integration |
| `tools/TUnit.Pipeline` | CI/CD pipeline orchestration |
| `TUnit.Templates` | `dotnet new` project templates |

## Test Projects

| Project | Purpose |
|---------|---------|
| `TUnit.TestProject` | Integration tests - NEVER run without filters |
| `TUnit.TestProject.FSharp` | F# integration tests |
| `TUnit.TestProject.VB.NET` | VB.NET integration tests |
| `TUnit.TestProject.Library` | Shared library for test projects |
| `TUnit.Engine.Tests` | Engine unit tests |
| `TUnit.Core.Tests` | Core library unit tests |
| `TUnit.UnitTests` | General unit tests |
| `TUnit.Assertions.Tests` | Assertion library tests |
| `TUnit.Assertions.SourceGenerator.Tests` | Assertion source generator tests |
| `TUnit.Assertions.Analyzers.Tests` | Assertion analyzer tests |
| `TUnit.Assertions.Analyzers.CodeFixers.Tests` | Assertion code fixer tests |
| `TUnit.Analyzers.Tests` | Analyzer unit tests |
| `TUnit.AspNetCore.Analyzers.Tests` | ASP.NET Core analyzer tests |
| `TUnit.Core.SourceGenerator.Tests` | Snapshot tests for source generator |
| `TUnit.SourceGenerator.IncrementalTests` | Incremental source generator tests |
| `TUnit.IntegrationTests` | End-to-end integration tests |
| `TUnit.RpcTests` | RPC protocol tests |
| `TUnit.PublicAPI` | Snapshot tests for public API |
| `TUnit.Templates.Tests` | Template instantiation tests |

## Mocking Framework

| Project | Purpose |
|---------|---------|
| `TUnit.Mocks` | Source-generated mocking framework (core runtime: `MockEngine<T>`, `Mock<T>`, setup/verification) |
| `TUnit.Mocks.SourceGenerator` | Roslyn source generator that emits mock implementations and extension methods |
| `TUnit.Mocks.SourceGenerator.Roslyn414/44/47` | Roslyn version variants (link source from base generator) |
| `TUnit.Mocks.Analyzers` | Analyzers for mock usage correctness |
| `TUnit.Mocks.Assertions` | TUnit assertion integration for mocks |
| `TUnit.Mocks.Http` | HTTP mocking (`MockHttpHandler`, `MockHttpClient`) |
| `TUnit.Mocks.Logging` | `ILogger` mocking (`MockLogger`, `MockLogger<T>`) |
| `TUnit.Mocks.Tests` | Runtime mock tests (672 tests) |
| `TUnit.Mocks.SourceGenerator.Tests` | Snapshot tests for generated code |
| `TUnit.Mocks.Analyzers.Tests` | Analyzer tests |
| `TUnit.Mocks.Http.Tests` | HTTP mock tests |
| `TUnit.Mocks.Logging.Tests` | Logging mock tests |
| `TUnit.Mocks.Benchmarks` | BenchmarkDotNet performance comparisons vs Moq/NSubstitute/FakeItEasy |

## Performance & Benchmarking

| Project | Purpose |
|---------|---------|
| `TUnit.Performance.Tests` | BenchmarkDotNet performance tests |
| `TUnit.SourceGenerator.Benchmarks` | Source generator performance benchmarks |
| `TUnit.PerformanceBenchmarks` | Large-scale performance validation |

## Roslyn Version Projects

Multi-targeting for Roslyn API compatibility:

- `*.Roslyn414`, `*.Roslyn44`, `*.Roslyn47`

## Dual-Mode Architecture

Changes to core engine metadata collection must work in both modes:

- **Source-gen**: `TUnit.Core.SourceGenerator` → compile-time
- **Reflection**: `TUnit.Engine` → runtime

Both modes feed into a unified execution path after metadata collection.
