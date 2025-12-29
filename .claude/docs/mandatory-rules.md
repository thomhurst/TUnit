# Mandatory Rules - Full Details

## Rule 1: Dual-Mode Implementation

IMPORTANT: This rule applies only to changes in **core engine metadata collection code**.

TUnit supports two execution modes that MUST produce identical behavior:

- **Source-Generated Mode** (`TUnit.Core.SourceGenerator`) - Compile-time code generation
- **Reflection Mode** (`TUnit.Engine`) - Runtime test discovery

### When This Applies

- Adding/modifying test discovery logic
- Changing how test metadata is collected
- Adding new attributes that affect test execution
- Modifying hook discovery or invocation

### When This Does NOT Apply

- Changes after metadata collection (unified code path)
- Assertion library changes
- Analyzer changes
- Documentation changes

### Implementation Checklist

- Feature implemented in `TUnit.Core.SourceGenerator` (source-gen path)
- Feature implemented in `TUnit.Engine` (reflection path)
- Tests verify both modes behave identically

---

## Rule 2: Snapshot Testing

### Trigger Conditions

Run snapshot tests when changing:
- Source generator output (`TUnit.Core.SourceGenerator`)
- Public APIs (`TUnit.Core`, `TUnit.Engine`, `TUnit.Assertions`)

### Workflow

```bash
# Source Generator Changes:
dotnet test TUnit.Core.SourceGenerator.Tests

# Public API Changes:
dotnet test TUnit.PublicAPI

# Review .received.txt files for intentional changes

# Accept snapshots:
# Linux/macOS:
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Windows:
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

# Commit .verified.txt files
git add *.verified.txt
git commit -m "Update snapshots: [reason]"
```

IMPORTANT: NEVER commit `.received.txt` files. Only `.verified.txt` files should be tracked.

---

## Rule 3: No VSTest

- USE: `Microsoft.Testing.Platform`
- NEVER: `Microsoft.VisualStudio.TestPlatform` (VSTest)

VSTest is legacy and incompatible with TUnit's architecture. If you encounter VSTest references in new code, stop and refactor to use Microsoft.Testing.Platform.

---

## Rule 4: Performance First

TUnit processes millions of tests. Performance is not optional.

### Requirements

- Minimize allocations in hot paths (test discovery, execution)
- Cache reflection results
- Use `ValueTask` for potentially-sync operations
- Use object pooling for frequent allocations
- Profile before/after for changes in critical paths

### Hot Paths (Always Profile)

1. Test discovery (source generation + reflection scanning)
2. Test execution (invocation, assertions, result collection)
3. Data generation (argument expansion, data sources)

### Performance Patterns

```csharp
// Cache reflection results
private static readonly ConcurrentDictionary<Type, MethodInfo[]> TestMethodCache = new();

// Object pooling
private static readonly ObjectPool<StringBuilder> StringBuilderPool =
    ObjectPool.Create<StringBuilder>();

// Span<T> to avoid allocations
public void ProcessTestName(ReadOnlySpan<char> name) { }

// ArrayPool for temporary buffers
var buffer = ArrayPool<byte>.Shared.Rent(size);
try { /* use buffer */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }
```

---

## Rule 5: AOT/Trimming Compatibility

All code must work with Native AOT and IL trimming.

### Guidelines

```csharp
// Annotate reflection usage
public void DiscoverTests(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    Type testClass)
{
    var methods = testClass.GetMethods();
}

// Suppress warnings when safe
[UnconditionalSuppressMessage("Trimming", "IL2070",
    Justification = "Test methods preserved by source generator")]
public void InvokeTest(MethodInfo method) { }
```

### Verification

```bash
cd TUnit.TestProject
dotnet publish -c Release -p:PublishAot=true --use-current-runtime
```

If AOT compilation fails, common causes are:
- Dynamic code generation (not AOT-compatible)
- Reflection without proper annotations
- Missing `[DynamicallyAccessedMembers]` attributes
