# Project Structure

---

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
| `TUnit.Aspire` | .NET Aspire integration |
| `TUnit.Logging.Microsoft` | Microsoft.Extensions.Logging integration (no ASP.NET Core dependency) |
| `TUnit.PropertyTesting` | Property-based testing |
| `TUnit.FsCheck` | F#-based property testing integration |
| `TUnit.Playwright` | Browser testing integration |
| `TUnit.Pipeline` | CI/CD pipeline orchestration |
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
