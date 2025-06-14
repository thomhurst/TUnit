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
dotnet run -- --treenode-filter "TestName"    # Run specific test

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

### Core Components

1. **TUnit.Core.SourceGenerator**: Source generators that discover tests at compile-time
   - Entry: Multiple `IIncrementalGenerator` implementations
   - Generates static test registrations avoiding runtime reflection

2. **TUnit.Core**: Core abstractions and attributes
   - Test attributes: `[Test]`, `[Arguments]`, `[Before]`, `[After]`
   - Interfaces: `IDataAttribute`, `ITestExecutor`, `IHookExecutor`
   - Context objects: `TestContext`, test state management

3. **TUnit.Engine**: Test execution engine
   - Entry: `TestingPlatformBuilderHook` integrates with Microsoft.Testing.Platform
   - Discovery: `TUnitTestDiscoverer` → `BaseTestsConstructor`
   - Execution: `TestsExecutor` → `SingleTestExecutor` → `TestInvoker`
   - Parallel execution management with dependency resolution

4. **TUnit.Assertions**: Fluent assertion library
   - Entry: `Assert.That()` returns builder objects
   - Builder pattern for fluent API
   - All assertions are async (`Task` return type)
   - Extensible via extension methods

### Key Architectural Patterns

- **Compile-Time Discovery**: Source generators create static test metadata during build
- **Parallel-First**: Tests run in parallel by default with smart scheduling
- **Context-Driven**: Rich context objects flow through execution pipeline
- **Extensible**: Custom executors, data sources, hooks, and assertions

### Dual Mode Support: Source Generation and Reflection

TUnit supports two modes of operation:
- **Source Generation Mode** (default): Uses compile-time source generators for optimal performance
- **Reflection Mode**: Falls back to runtime reflection when source generation is not available

**Important**: When implementing new features or fixing bugs, you MUST implement the functionality in BOTH modes to ensure consistent behavior for all users. The `BaseTestsConstructor` class chooses between `SourceGeneratedTestsConstructor` and `ReflectionTestsConstructor` based on availability.

### Extension Projects

- **TUnit.Playwright**: Browser testing integration
- **TUnit.Assertions.FSharp**: F# language support
- **TUnit.Analyzers**: Roslyn analyzers for compile-time validation
- **TUnit.Templates**: Project templates for `dotnet new`

## Development Guidelines

When working on TUnit:

1. **Dual Implementation**: All features must work in both source generation AND reflection modes
2. **Source Generators**: Changes to attributes or test discovery require updating source generators
3. **Testing**: Use `TUnit.UnitTests` for framework tests, `TUnit.TestProject` for integration tests
4. **Analyzers**: Add corresponding analyzer rules when adding new features
5. **Platform Integration**: Ensure compatibility with Microsoft.Testing.Platform capabilities
6. **Performance**: TUnit prioritizes performance - avoid runtime reflection where possible
7. **Async Support**: All public APIs should support async operations properly

## Key Files and Locations

- Solution file: `TUnit.sln`
- Global build properties: `Directory.Build.props`
- Package versions: `Directory.Packages.props`
- CI/CD: `.github/workflows/`
- Documentation source: `docs/`
- Pipeline automation: `TUnit.Pipeline/`