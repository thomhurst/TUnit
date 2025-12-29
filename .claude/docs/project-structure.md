# Project Structure

## Core Projects

| Project | Purpose | Key Responsibility |
|---------|---------|-------------------|
| `TUnit.Core` | Abstractions, attributes, interfaces | Public API surface |
| `TUnit.Engine` | Test discovery & execution | **Reflection mode** |
| `TUnit.Core.SourceGenerator` | Compile-time test generation | **Source-gen mode** |
| `TUnit.Assertions` | Fluent assertion library | Separate from core |
| `TUnit.Assertions.SourceGenerator` | Custom assertion generation | Extensibility |
| `TUnit.Analyzers` | Roslyn analyzers & code fixes | Compile-time safety |
| `TUnit.PropertyTesting` | Property-based testing | New feature |
| `TUnit.Playwright` | Browser testing integration | Playwright wrapper |

## Test Projects

| Project | Purpose |
|---------|---------|
| `TUnit.TestProject` | Integration tests (dogfooding) - NEVER run without filters |
| `TUnit.Engine.Tests` | Engine-specific tests |
| `TUnit.Assertions.Tests` | Assertion library tests |
| `TUnit.Core.SourceGenerator.Tests` | **Snapshot tests for source generator** |
| `TUnit.PublicAPI` | **Snapshot tests for public API** |

## Roslyn Version Projects

Multi-targeting for Roslyn API versions ensures compatibility across Visual Studio and .NET SDK versions:

- `*.Roslyn414` - Roslyn 4.1.4
- `*.Roslyn44` - Roslyn 4.4
- `*.Roslyn47` - Roslyn 4.7

## Key Directories

```
TUnit/
├── TUnit.Core/                    # Public API, attributes, interfaces
├── TUnit.Engine/                  # Reflection-based test discovery
├── TUnit.Core.SourceGenerator/    # Source-gen test discovery
├── TUnit.Assertions/              # Assertion library
├── TUnit.Analyzers/               # Roslyn analyzers
├── TUnit.TestProject/             # Integration tests (use filters!)
├── TUnit.Engine.Tests/            # Unit tests for engine
├── TUnit.Assertions.Tests/        # Unit tests for assertions
├── TUnit.Core.SourceGenerator.Tests/  # Snapshot tests
├── TUnit.PublicAPI/               # API snapshot tests
└── docs/                          # Documentation source
```

## Dual-Mode Architecture

```
User Test Code
    │
    ├─► SOURCE-GENERATED MODE
    │   TUnit.Core.SourceGenerator
    │   └─► Compile-time code generation
    │
    └─► REFLECTION MODE
        TUnit.Engine
        └─► Runtime test discovery

Both modes feed into unified execution path after metadata collection.
```
