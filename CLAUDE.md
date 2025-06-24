# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TUnit is a modern, source-generated testing framework for .NET that provides:
- Compile-time test discovery through source generators
- Parallel test execution by default
- Native AOT and trimming support
- Built on Microsoft.Testing.Platform
- Fluent assertion library
- Rich extensibility model

## Build and Test Commands

### Prerequisites
- .NET SDK 9.0.301 or later (see `global.json`)
- Targets .NET 8.0 and .NET 9.0

### Common Commands

```bash
# Build the solution
dotnet build                    # Debug build
dotnet build -c Release         # Release build

# Run all tests in a project (3 equivalent ways)
cd [TestProjectDirectory]
dotnet run -c Release
dotnet test -c Release
dotnet exec bin/Release/net8.0/TestProject.dll

# Run tests with options
dotnet run -- --list-tests                    # List all tests
dotnet run -- --fail-fast                     # Stop on first failure
dotnet run -- --maximum-parallel-tests 10     # Control parallelism
dotnet run -- --report-trx --coverage         # Generate reports
dotnet run -- --treenode-filter "TestName"    # Run specific test by exact name
dotnet run -- --treenode-filter/*/*/*PartialName*/*  # Filter tests by partial name pattern

# Build NuGet packages
dotnet pack -c Release

# Run the full pipeline
dotnet run --project TUnit.Pipeline/TUnit.Pipeline.csproj

# Clean build artifacts
./clean.ps1
```

### Documentation Site
```bash
cd docs
npm install      # First time only
npm start        # Run locally at localhost:3000
npm run build    # Build static site
```

## High-Level Architecture

**Note: TUnit now uses a simplified architecture. See [SIMPLIFIED_ARCHITECTURE.md](docs/SIMPLIFIED_ARCHITECTURE.md) for details.**

### Core Components (Simplified Architecture)

1. **TUnit.Core.SourceGenerator**: Source generators that discover tests at compile-time
   - Entry: `UnifiedTestMetadataGenerator` - generates test metadata and registration
   - Generates `TestMetadata` instances with AOT-friendly invokers
   - Creates compile-time test registry for zero-reflection discovery

2. **TUnit.Core**: Core abstractions and attributes
   - Test attributes: `[Test]`, `[Arguments]`, `[Before]`, `[After]`
   - **TestMetadata**: Unified compile-time test representation
   - **TestContext**: Simplified context for test execution
   - Interfaces for extensibility: `ITestMetadataSource`, `IDataAttribute`

3. **TUnit.Engine**: Simplified test execution engine
   - Entry: `SimplifiedTUnitTestFramework` integrates with Microsoft.Testing.Platform
   - Discovery: `TestDiscoveryService` → `TestFactory` → `ExecutableTest`
   - Execution: `UnifiedTestExecutor` with clear parallel/serial paths
   - Single test execution: `DefaultSingleTestExecutor`
   - Test Building: `TestMetadataSource` → `TestBuilder` → `TestDefinition`
   - Execution: `TestsExecutor` → `SingleTestExecutor` → `TestInvoker`
   - Parallel execution management with dependency resolution

4. **TUnit.Assertions**: Fluent assertion library
   - Entry: `Assert.That()` returns builder objects
   - Builder pattern for fluent API
   - All assertions are async (`Task` return type)
   - Extensible via extension methods

### Key Architectural Patterns

- **Clean Separation**: Source generators emit only data (TestMetadata), runtime handles all logic
- **Compile-Time Discovery**: Source generators create static test metadata during build
- **Runtime Expansion**: TestBuilder expands metadata into executable tests with data variations
- **Parallel-First**: Tests run in parallel by default with smart scheduling
- **Context-Driven**: Rich context objects flow through execution pipeline
- **Extensible**: Custom executors, data sources, hooks, and assertions

### Clean Architecture Approach

TUnit uses a clean architecture with clear separation of concerns:
- **Source Generation Phase**: Only emits TestMetadata data structures
- **Runtime Phase**: TestBuilder handles all complex logic including:
  - Data source enumeration and expansion
  - Tuple unwrapping for method arguments
  - Property injection with data sources
  - Test instance creation and lifecycle
  - Expression compilation for performance

### Extension Projects

- **TUnit.Playwright**: Browser testing integration
- **TUnit.Assertions.FSharp**: F# language support
- **TUnit.Analyzers**: Roslyn analyzers for compile-time validation
- **TUnit.Templates**: Project templates for `dotnet new`

## Development Guidelines

When working on TUnit:

1. **Clean Architecture**: Maintain separation between source generation (data only) and runtime (logic)
2. **Source Generators**: TestMetadataGenerator should only emit data structures, never execution logic
3. **Testing**: Use `TUnit.UnitTests` for framework tests, `TUnit.TestProject` for integration tests
4. **Analyzers**: Add corresponding analyzer rules when adding new features
5. **Platform Integration**: Ensure compatibility with Microsoft.Testing.Platform capabilities
6. **Performance**: TUnit prioritizes performance - use expression compilation and caching in TestBuilder
7. **Async Support**: All public APIs should support async operations properly
8. **Async Best Practices**:
   - Never use `GetAwaiter().GetResult()` or `.Result` on tasks - always use proper async/await to avoid deadlocks
   - When in a sync context that needs async functionality, refactor method signatures to be async rather than trying to execute async code synchronously
   - TUnit supports async all the way through the stack - embrace it!

## Key Files and Locations

- Solution file: `TUnit.sln`
- Global build properties: `Directory.Build.props`
- Package versions: `Directory.Packages.props`
- CI/CD: `.github/workflows/`
- Documentation source: `docs/`
- Pipeline automation: `TUnit.Pipeline/`

## Memories

- Remember to use the test filter syntax from the claude instructions, including slashes.
- Remember to not add redundant comments and instead write self descriptive code.
