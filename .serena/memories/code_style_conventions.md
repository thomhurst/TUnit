# TUnit Code Style and Conventions

## General Guidelines
- **Self-Descriptive Code**: Write self-descriptive code instead of adding redundant comments
- **Clean Architecture**: Maintain separation between source generation (data only) and runtime (logic)
- **Async Best Practices**: Never use `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` - always use proper async/await

## Async Guidelines (Critical)
- **Never Block on Async**: Avoid `GetAwaiter().GetResult()`, `.Result`, or `.Wait()` to prevent deadlocks
- **Async All the Way**: When in sync context needing async, refactor method signatures to be async
- **Embrace Async**: TUnit supports async throughout the stack - use it properly
- **Proper Cancellation**: Use CancellationToken properly for long-running operations

## Architecture Patterns
- **Source Generators**: Should only emit data structures (TestMetadata), never execution logic
- **Runtime Phase**: TestBuilder handles complex logic (data expansion, tuple unwrapping, etc.)
- **Performance Focus**: Use expression compilation and caching in TestBuilder
- **Extension Points**: Support custom executors, data sources, hooks, and assertions

## Project Structure
- Core logic in `TUnit.Core`
- Source generation in `TUnit.Core.SourceGenerator` 
- Test execution in `TUnit.Engine`
- Assertions in `TUnit.Assertions`
- Tests in `TUnit.UnitTests` (framework) and `TUnit.TestProject` (integration)

## Testing Conventions
- Use TUnit for framework testing
- Add analyzer rules when adding new features
- Ensure Microsoft.Testing.Platform compatibility