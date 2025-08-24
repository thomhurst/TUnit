# TUnit Project Index

## Project Type
- **Framework**: Modern .NET Testing Framework
- **Language**: C# (.NET 8.0, .NET 9.0)
- **Architecture**: Source Generation + Reflection dual-mode
- **Platform**: Microsoft.Testing.Platform based

## Solution Structure (TUnit.sln)

### Core Components

#### 1. TUnit.Core (Core Abstractions)
- **Purpose**: Core interfaces, attributes, and base types
- **Key Files**:
  - `TestContext.cs` - Test execution context
  - `AbstractExecutableTest.cs` - Base test abstraction
  - `Attributes/` - Test attributes (Test, Before, After, etc.)
  - `Interfaces/` - Core contracts
  - `Enums/` - Framework enumerations
  - `Extensions/` - Extension methods
  - `Services/` - Core services

#### 2. TUnit.Engine (Execution Engine)
- **Purpose**: Test discovery, execution, and orchestration
- **Key Components**:
  - `Building/` - Test builders and metadata collection
  - `Services/` - Test execution services (SingleTestExecutor, HookOrchestrator, etc.)
  - `CommandLineProviders/` - CLI command providers
  - `Capabilities/` - Framework capabilities (TRX reports, etc.)
  - `Helpers/` - Utility classes
  - `Logging/` - Logging infrastructure

#### 3. TUnit.Core.SourceGenerator (Compile-time Generation)
- **Purpose**: Source generation for compile-time test discovery
- **Key Components**:
  - `CodeGenerators/` - Various code generators
  - `Generators/` - AOT and method invocation generators
  - `Models/` - Data models for generation
  - `Analyzers/` - Code analyzers
  - `Builders/` - Test definition builders
- **Roslyn Versions**: Roslyn44, Roslyn47, Roslyn414 variants

#### 4. TUnit.Assertions (Fluent Assertions)
- **Purpose**: Fluent assertion library
- **Key Files**:
  - `Assert.cs` - Main assertion entry point
  - `AssertionData.cs` - Assertion data structures
  - `Extensions/` - Type-specific assertion extensions
  - `Exceptions/` - Assertion exceptions

#### 5. TUnit (Meta Package)
- **Purpose**: Main NuGet package that bundles all components
- **Dependencies**: References Core, Engine, Assertions, and Analyzers

### Analyzer Projects

#### 6. TUnit.Analyzers
- **Purpose**: Compile-time code analyzers for test code validation
- **Roslyn Versions**: Roslyn44, Roslyn47, Roslyn414

#### 7. TUnit.Analyzers.CodeFixers
- **Purpose**: Code fix providers for analyzer diagnostics

#### 8. TUnit.Assertions.Analyzers
- **Purpose**: Analyzers specific to assertion usage
- **Related**: TUnit.Assertions.Analyzers.CodeFixers

### Test Projects

#### 9. TUnit.TestProject
- **Purpose**: Main integration test project
- **Structure**:
  - Various test scenarios and examples
  - Object tracking tests
  - Parallel execution tests
  - Hook tests

#### 10. TUnit.Core.SourceGenerator.Tests
- **Purpose**: Source generator snapshot tests
- **Uses**: Verify library for snapshot testing

#### 11. Other Test Projects
- `TUnit.UnitTests` - Unit tests
- `TUnit.Engine.Tests` - Engine tests
- `TUnit.Assertions.Tests` - Assertion tests
- `TUnit.Analyzers.Tests` - Analyzer tests
- `TUnit.AOT.Tests` - AOT compatibility tests
- `TUnit.RpcTests` - RPC tests
- `TUnit.Performance.Tests` - Performance benchmarks
- `TUnit.IntegrationTests` - Integration tests
- `TUnit.PublicAPI` - Public API surface tests

### Extension Libraries

#### 12. TUnit.Playwright
- **Purpose**: Playwright integration for browser testing

#### 13. TUnit.Assertions.FSharp
- **Purpose**: F# support for assertions

### Templates

#### 14. TUnit.Templates
- **Purpose**: Project templates for dotnet new
- **Templates**:
  - Basic TUnit project
  - ASP.NET integration
  - Playwright integration
  - Aspire integration

### Example Projects
- `TUnit.Example` - Basic examples
- `TUnit.Example.Asp.Net` - ASP.NET examples
- `TUnit.Example.Asp.Net.TestProject` - ASP.NET test examples

### Supporting Projects
- `TUnit.Pipeline` - CI/CD pipeline support
- `TUnit.TestProject.Library` - Shared test library
- `TUnit.TestProject.FSharp` - F# test project
- `TUnit.TestProject.VB.NET` - VB.NET test project
- `Playground` - Experimental/development sandbox

### Build & Tools
- **Build System**: MSBuild with Directory.Build.props
- **Package Management**: Central Package Management (Directory.Packages.props)
- **Versioning**: GitVersion.yml
- **CI/CD**: GitHub Actions (.github/workflows/)

## Key Technologies
- **Source Generators**: Roslyn-based code generation
- **Testing Platform**: Microsoft.Testing.Platform
- **Snapshot Testing**: Verify library
- **Benchmarking**: BenchmarkDotNet
- **Documentation**: docs/ directory

## Development Patterns
1. **Dual Execution Modes**: Source-generated and reflection-based
2. **AOT Support**: Native AOT and trimming compatibility
3. **Parallel by Default**: Tests run in parallel unless specified
4. **Extensibility**: Hook system, custom executors, data sources
5. **Modern C#**: Using latest C# features and patterns

## File Organization
- Source files use file-scoped namespaces
- Tests organized by feature/component
- Analyzers follow Roslyn conventions
- Templates follow dotnet template structure

## Important Configuration Files
- `global.json` - SDK version (9.0.301)
- `Directory.Build.props` - Common MSBuild properties
- `Directory.Packages.props` - Central package versions
- `GitVersion.yml` - Version configuration
- `CLAUDE.md` - Development guidelines
- `.editorconfig` - Code style settings