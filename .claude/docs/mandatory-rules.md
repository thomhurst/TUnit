# Mandatory Rules

Detailed explanations of the critical rules from CLAUDE.md.

---

## Rule 1: Dual-Mode Implementation

This rule applies only to **core engine metadata collection code**.

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

### Checklist

- Feature implemented in `TUnit.Core.SourceGenerator`
- Feature implemented in `TUnit.Engine`
- Tests verify both modes behave identically

---

## Rule 2: Snapshot Testing

Run snapshot tests when changing:
- Source generator output → `dotnet test TUnit.Core.SourceGenerator.Tests`
- Public APIs → `dotnet test TUnit.PublicAPI`

See CLAUDE.md for the quick fix workflow to accept snapshots.

---

## Rule 3: No VSTest

- USE: `Microsoft.Testing.Platform`
- NEVER: `Microsoft.VisualStudio.TestPlatform` (VSTest)

VSTest is legacy and incompatible with TUnit's architecture.

---

## Rule 4: Performance First

TUnit processes millions of tests. Performance is not optional.

### Requirements

- Minimize allocations in hot paths (test discovery, execution)
- Cache reflection results
- Use `ValueTask` for potentially-sync operations
- Use object pooling for frequent allocations
- Profile before/after for changes in critical paths

### Hot Paths

1. Test discovery (source generation + reflection scanning)
2. Test execution (invocation, assertions, result collection)
3. Data generation (argument expansion, data sources)

---

## Rule 5: AOT/Trimming Compatibility

All code must work with Native AOT and IL trimming.

### Annotations

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
