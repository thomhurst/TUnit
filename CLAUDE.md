# TUnit Development Guide for LLM Agents

## MANDATORY RULES - ALWAYS FOLLOW
1. **Dual-mode implementation required**: ALL changes must work identically in both source-generated and reflection modes
2. **Snapshot tests are critical**: After ANY change to source generator or public APIs, you MUST run and accept snapshots
3. **Never use VSTest**: This project uses Microsoft.Testing.Platform exclusively
4. **Performance first**: Optimize for speed - this framework is used by millions

## When You Make Changes

### If you modify source generator code:
1. Run: `dotnet test TUnit.Core.SourceGenerator.Tests`
2. If snapshots differ, rename ALL `*.received.txt` files to `*.verified.txt`
3. Commit the updated `.verified.txt` files

### If you modify public APIs:
1. Run: `dotnet test TUnit.PublicAPI`
2. If snapshots differ, rename ALL `*.received.txt` files to `*.verified.txt`
3. Commit the updated `.verified.txt` files

### If you add a feature:
1. Implement in BOTH `TUnit.Core.SourceGenerator` AND `TUnit.Engine` (reflection path)
2. Verify identical behavior in both modes
3. Add tests covering both execution paths
4. Consider if an analyzer rule would help prevent misuse
5. Test performance impact

### If you fix a bug:
1. Write a failing test first
2. Fix in BOTH execution modes (source-gen and reflection)
3. Verify no performance regression

## Project Structure
- `TUnit.Core`: Abstractions, interfaces, attributes
- `TUnit.Engine`: Test discovery and execution (reflection mode)
- `TUnit.Core.SourceGenerator`: Compile-time code generation (source-gen mode)
- `TUnit.Assertions`: Fluent assertion library
- `TUnit.Analyzers`: Roslyn analyzers for compile-time validation

## Code Style (REQUIRED)
```csharp
// Modern C# syntax - always use
List<string> list = [];  // Collection expressions
var result = GetValue();  // var for obvious types

// Always use braces
if (condition)
{
    DoSomething();
}

// Naming
public string PublicField;      // PascalCase
private string _privateField;   // _camelCase

// Async
async ValueTask DoWorkAsync(CancellationToken cancellationToken)  // ValueTask when possibly sync
```

## Performance Guidelines
- Minimize allocations in hot paths (discovery/execution)
- Use object pooling for frequent allocations
- Cache reflection results
- Benchmark critical paths before/after changes

## Common Commands
```bash
# Test everything
dotnet test

# Test source generator specifically
dotnet test TUnit.Core.SourceGenerator.Tests

# Test public API surface
dotnet test TUnit.PublicAPI

# Accept snapshots (Windows - use this after verifying diffs are correct)
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

# Run specific test by filter
dotnet test -- --treenode-filter "/Assembly/Namespace/ClassName/TestName"
```

## AOT/Trimming Compatibility
- Use `[UnconditionalSuppressMessage]` for known-safe reflection
- Test with trimming/AOT enabled projects
- Avoid dynamic code generation at runtime (use source generators instead)

## Threading and Safety
- All test execution must be thread-safe
- Use proper synchronization for shared state
- Dispose resources correctly (implement IDisposable/IAsyncDisposable)

## Critical Mistakes to Avoid
1. ❌ Implementing a feature only in source-gen mode (must do BOTH)
2. ❌ Breaking change to public API without major version bump
3. ❌ Forgetting to accept snapshots after intentional generator changes
4. ❌ Performance regression in discovery or execution
5. ❌ Using reflection in ways incompatible with AOT/trimming

## Verification Checklist
Before completing any task, verify:
- [ ] Works in both source-generated and reflection modes
- [ ] Snapshots accepted if generator/API changed
- [ ] Tests added and passing
- [ ] No performance regression
- [ ] AOT/trimming compatible
- [ ] Thread-safe if touching concurrent code

## Target Frameworks
- .NET Standard 2.0 (library compatibility)
- .NET 6, 8, 9+ (current support)

## Philosophy
TUnit aims to be: **fast, modern, reliable, and enjoyable to use**. Every change should advance these goals.