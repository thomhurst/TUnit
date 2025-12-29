# Troubleshooting

## Snapshot Tests Failing

**Symptom**: `TUnit.Core.SourceGenerator.Tests` or `TUnit.PublicAPI` failing

**Solution**:

```bash
# 1. Review .received.txt files
cd TUnit.Core.SourceGenerator.Tests  # or TUnit.PublicAPI
ls *.received.txt

# 2. If changes are intentional, accept snapshots:
# Linux/macOS:
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Windows:
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

# 3. Commit .verified.txt files
git add *.verified.txt
git commit -m "Update snapshots: [reason]"

# 4. Verify no .received.txt files staged
git status
```

IMPORTANT: NEVER commit `.received.txt` files.

---

## Tests Pass Locally, Fail in CI

**Common Causes**:
1. Forgot to commit `.verified.txt` files
2. Line ending differences (CRLF vs LF)
3. Race conditions in parallel tests
4. Missing dependencies

**Solutions**:

```bash
# Check for uncommitted snapshots
git status | grep verified.txt

# Check line endings
git config core.autocrlf

# Run with same parallelization as CI
dotnet test --parallel
```

---

## Dual-Mode Behavior Differs

**Symptom**: Test passes in one mode but fails in the other

**Diagnostic**:

```bash
# Check generated code
# Look in: obj/Debug/net9.0/generated/TUnit.Core.SourceGenerator/

# Set breakpoint in TUnit.Engine for reflection path
```

**Common Issues**:
- Attribute not checked in reflection path
- Different data expansion logic
- Missing hook invocation in one mode

**Solution**: Implement missing logic in the other execution mode.

---

## AOT Compilation Fails

**Symptom**: `dotnet publish -p:PublishAot=true` fails

**Common Causes**:
1. Dynamic code generation (not AOT-compatible)
2. Reflection without proper annotations
3. Missing `[DynamicallyAccessedMembers]`

**Solution**:

```csharp
// Add proper annotations
public void DiscoverTests(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    Type testClass)
{
    var methods = testClass.GetMethods();
}

// Or suppress when safe
[UnconditionalSuppressMessage("Trimming", "IL2070",
    Justification = "Test methods preserved by source generator")]
public void InvokeTest(MethodInfo method) { }
```

---

## Performance Regression

**Diagnostic**:

```bash
# Benchmark
cd TUnit.Performance.Tests
dotnet run -c Release --framework net9.0

# Profile
dotnet trace collect -- dotnet test
```

**Common Causes**:
- Missing reflection cache
- Unnecessary allocations in hot paths
- Blocking on async (`.Result`, `.GetAwaiter().GetResult()`)
- LINQ in tight loops without profiling

**Solutions**:
- Add caching for reflection results
- Use object pooling for frequent allocations
- Use `ValueTask` for potentially-sync operations
- Profile to identify actual bottlenecks

---

## TUnit.TestProject Shows Many Failures

**This is expected behavior.** Many tests are designed to fail to verify error handling.

**Solution**: Always run with filters:

```bash
dotnet run -- --treenode-filter "/*/*/SpecificClass/*"
```

See `workflows.md` for filter syntax.
