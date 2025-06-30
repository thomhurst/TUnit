# TUnit Project Overview

## Purpose
TUnit is a modern, source-generated testing framework (with a reflection-based fallback) for .NET that provides:
- Compile-time test discovery through source generators
- Parallel test execution by default
- Native AOT and trimming support
- Built on Microsoft.Testing.Platform
- Fluent assertion library
- Rich extensibility model

## Tech Stack
- **Language**: C#
- **Target Frameworks**: .NET 8.0 and .NET 9.0
- **SDK Requirement**: .NET SDK 9.0.301 or later (see global.json)
- **Architecture**: Source generation + runtime execution engine
- **Testing Platform**: Microsoft.Testing.Platform integration

## Core Components
1. **TUnit.Core**: Core abstractions and attributes
2. **TUnit.Core.SourceGenerator**: Source generators for compile-time test discovery
3. **TUnit.Engine**: Test execution engine with simplified architecture
4. **TUnit.Assertions**: Fluent assertion library with async support
5. **Extension Projects**: Playwright, F#, Analyzers, Templates

## Key Features
- **Clean Architecture**: Separation between source generation (data) and runtime (logic)
- **Parallel-First**: Tests run in parallel by default with smart scheduling
- **Async Support**: All public APIs support async operations
- **Extensible**: Custom executors, data sources, hooks, and assertions
- **Dual Mode**: TUnit supports Source Generated mode and Reflection Mode, and both should maintain feature parity, with the exception of the source generated code path MUST support Native AOT and trimming
