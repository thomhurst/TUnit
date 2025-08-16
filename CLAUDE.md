# TUnit Development Guide

## Critical Rules
1. **Behavioral parity** between source-generated and reflection modes is mandatory
2. **Run snapshot tests** when changing:
   - Source generator output: `dotnet test TUnit.Core.SourceGenerator.Tests`  
   - Public APIs: `dotnet test TUnit.PublicAPI`
3. **Accept snapshots**: Rename `.received.txt` → `.verified.txt` and commit
4. **Use Microsoft.Testing.Platform**, never VSTest
5. **Performance is paramount** - this framework may be used by millions

## Architecture

### Dual Execution Modes
- **Source Generated**: Compile-time generation for performance
- **Reflection**: Runtime for dynamic scenarios
- Both must produce identical behavior

### Core Components
- **TUnit.Core**: Abstractions and interfaces
- **TUnit.Engine**: Test discovery and execution
- **TUnit.Core.SourceGenerator**: Compile-time generation
- **TUnit.Assertions**: Fluent assertions
- **TUnit.Analyzers**: Compile-time validation

## Coding Standards

### Modern C#
- Collection initializers: `List<string> list = []`
- Pattern matching, records, file-scoped namespaces
- `var` for obvious types
- `ValueTask` for potentially synchronous operations

### Formatting
```csharp
if (condition)
{
    DoSomething();  // Always use braces
}
```
- PascalCase public, _camelCase private fields
- Expression-bodied members for simple logic
- Meaningful names over comments

### Performance
- Minimize allocations in hot paths
- Object pooling for frequent allocations
- Cache reflection results
- Profile discovery and execution paths

## Development Workflow

### Adding Features
1. Implement in both execution modes
2. Add analyzer rules if applicable
3. Write comprehensive tests
4. Verify performance impact
5. Update documentation

### Fixing Bugs
1. Write failing test
2. Fix in all affected modes
3. Verify no performance regression

### Before Submitting
- [ ] Both modes tested
- [ ] Source generator snapshots accepted (if changed)
- [ ] Public API snapshots accepted (if changed)
- [ ] Performance considered
- [ ] No breaking changes

## Quick Commands
```bash
# Run all tests
dotnet test

# Source generator tests
dotnet test TUnit.Core.SourceGenerator.Tests

# Public API tests
dotnet test TUnit.PublicAPI

# Accept snapshots (Windows)
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

# Accept snapshots (Linux/macOS)
for file in *.received.txt; do mv "$file" "${file%.received.txt}.verified.txt"; done

# Run specific test
dotnet test -- --treenode-filter "/Assembly/Namespace/ClassName/TestName"
```

## Key Patterns
- **Error Handling**: Specific exceptions with context
- **Async**: `CancellationToken` support throughout
- **Reflection**: AOT-friendly with `[UnconditionalSuppressMessage]`
- **Threading**: Ensure concurrent test safety
- **Disposal**: Proper resource cleanup

## Testing Categories
- Unit tests for components
- Integration for cross-component
- Performance benchmarks for critical paths
- Analyzer tests for compile-time rules
- Snapshot tests for generator and API surface

## Compatibility
- .NET Standard 2.0, .NET 6, 8, 9+
- AOT and trimming support
- Various project configurations

## Common Pitfalls
1. Mode inconsistency between source-gen and reflection
2. Performance regressions in discovery/execution
3. AOT/trimming issues with reflection
4. Thread safety in concurrent execution
5. Resource leaks from improper disposal
6. Forgetting to accept intentional snapshot changes

## Remember
Every change must maintain TUnit's goals: **fast, modern, reliable, and enjoyable to use**.