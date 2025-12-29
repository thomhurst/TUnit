# Project Structure

Overview of TUnit's codebase organization.

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
| `TUnit.PropertyTesting` | Property-based testing |
| `TUnit.Playwright` | Browser testing integration |

## Test Projects

| Project | Purpose |
|---------|---------|
| `TUnit.TestProject` | Integration tests - NEVER run without filters |
| `TUnit.Engine.Tests` | Engine unit tests |
| `TUnit.Assertions.Tests` | Assertion library tests |
| `TUnit.Core.SourceGenerator.Tests` | Snapshot tests for source generator |
| `TUnit.PublicAPI` | Snapshot tests for public API |

## Roslyn Version Projects

Multi-targeting for Roslyn API compatibility:
- `*.Roslyn414`, `*.Roslyn44`, `*.Roslyn47`

## Dual-Mode Architecture

Changes to core engine metadata collection must work in both modes:
- **Source-gen**: `TUnit.Core.SourceGenerator` → compile-time
- **Reflection**: `TUnit.Engine` → runtime

Both modes feed into a unified execution path after metadata collection.
