# TUnit Development Guide for LLM Agents

> **Purpose**: This file contains essential instructions for AI assistants (Claude, Copilot, etc.) working on TUnit.
> **Audience**: LLM agents, AI coding assistants
> **Optimization**: Structured for rapid parsing, clear decision trees, explicit requirements

---

## 🚨 MANDATORY RULES - ALWAYS FOLLOW

### Rule 1: Dual-Mode Implementation (CRITICAL)

**REQUIREMENT**: ALL changes must work identically in both execution modes.

```
User Test Code
    │
    ├─► SOURCE-GENERATED MODE (TUnit.Core.SourceGenerator)
    │   └─► Compile-time code generation
    │
    └─► REFLECTION MODE (TUnit.Engine)
        └─► Runtime test discovery

    Both modes MUST produce identical behavior
```

**Implementation Checklist**:
- [ ] Feature implemented in `TUnit.Core.SourceGenerator` (source-gen path)
- [ ] Feature implemented in `TUnit.Engine` (reflection path)
- [ ] Tests written for both modes
- [ ] Verified identical behavior in both modes

**If you implement only ONE mode, the feature is INCOMPLETE and MUST NOT be committed.**

---

### Rule 2: Snapshot Testing (NON-NEGOTIABLE)

**TRIGGER CONDITIONS**:
1. ANY change to source generator output
2. ANY change to public APIs (TUnit.Core, TUnit.Engine, TUnit.Assertions)

**WORKFLOW**:
```bash
# Source Generator Changes:
dotnet test TUnit.Core.SourceGenerator.Tests
# Review .received.txt files
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done  # Linux/macOS
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"              # Windows

# Public API Changes:
dotnet test TUnit.PublicAPI
# Review .received.txt files
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done  # Linux/macOS
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"              # Windows

# Commit .verified.txt files (NEVER commit .received.txt)
git add *.verified.txt
git commit -m "Update snapshots: [reason]"
```

**REMEMBER**: Snapshots are the source of truth. Failing to update them breaks CI.

---

### Rule 3: No VSTest (ABSOLUTE)

- ✅ **USE**: `Microsoft.Testing.Platform`
- ❌ **NEVER**: `Microsoft.VisualStudio.TestPlatform` (VSTest - legacy, incompatible)

If you see VSTest references in new code, **STOP** and use Microsoft.Testing.Platform instead.

---

### Rule 4: Performance First

**Context**: TUnit processes millions of tests daily. Performance is not optional.

**Requirements**:
- Minimize allocations in hot paths (test discovery, execution)
- Cache reflection results
- Use `ValueTask` for potentially-sync operations
- Profile before/after for changes in critical paths
- Use object pooling for frequent allocations

**Hot Paths** (profile these):
1. Test discovery (source generation + reflection scanning)
2. Test execution (invocation, assertions, result collection)
3. Data generation (argument expansion, data sources)

---

### Rule 5: AOT/Trimming Compatibility

**Requirement**: All code must work with Native AOT and IL trimming.

**Guidelines**:
```csharp
// ✅ CORRECT: Annotate reflection usage
public void DiscoverTests(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    Type testClass)
{
    var methods = testClass.GetMethods();
}

// ✅ CORRECT: Suppress warnings when safe
[UnconditionalSuppressMessage("Trimming", "IL2070",
    Justification = "Test methods preserved by source generator")]
public void InvokeTest(MethodInfo method) { }
```

**Verification**:
```bash
cd TUnit.TestProject
dotnet publish -c Release -p:PublishAot=true --use-current-runtime
```

---

## 📋 Quick Reference Card

### ⚠️ CRITICAL: TUnit.TestProject Testing Rules

**DO NOT run `TUnit.TestProject` without filters!** Many tests are intentionally designed to fail to verify error handling.

```bash
# ❌ WRONG - Will show many "failures" (expected behavior)
cd TUnit.TestProject && dotnet run
cd TUnit.TestProject && dotnet test

# ✅ CORRECT - Always use targeted filters
cd TUnit.TestProject && dotnet run -- --treenode-filter "/*/*/SpecificClass/*"
cd TUnit.TestProject && dotnet run -- --treenode-filter "/*/*/*/*[Category!=Performance]"

# ✅ CORRECT - Test other projects normally
dotnet test TUnit.Engine.Tests
dotnet test TUnit.Assertions.Tests
dotnet test TUnit.Core.SourceGenerator.Tests
```

**Why?** TUnit.TestProject contains:
- Tests that verify failure scenarios (expected to fail)
- Tests for error messages and diagnostics
- Performance tests that should be excluded by default
- Integration tests covering edge cases

**Rule**: Only run TUnit.TestProject with explicit `--treenode-filter` to target specific tests or classes.

---

### Most Common Commands

```bash
# Run all tests (excludes TUnit.TestProject integration tests)
dotnet test

# Test source generator + accept snapshots
dotnet test TUnit.Core.SourceGenerator.Tests
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Test public API + accept snapshots
dotnet test TUnit.PublicAPI
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Run specific test
dotnet test -- --treenode-filter "/Assembly/Namespace/ClassName/TestName"

# Exclude performance tests
dotnet test -- --treenode-filter "/*/*/*/*[Category!=Performance]"

# Build release
dotnet build -c Release

# Test AOT
dotnet publish -c Release -p:PublishAot=true
```

### Test Filter Syntax

```bash
# Single test
--treenode-filter "/TUnit.TestProject/Namespace/ClassName/TestMethodName"

# All tests in a class
--treenode-filter "/*/*/ClassName/*"

# Multiple patterns (OR)
--treenode-filter "Pattern1|Pattern2"

# Exclude by category
--treenode-filter "/*/*/*/*[Category!=Performance]"
```

---

## 🏗️ Project Structure

### Core Projects

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

### Test Projects

| Project | Purpose |
|---------|---------|
| `TUnit.TestProject` | Integration tests (dogfooding) |
| `TUnit.Engine.Tests` | Engine-specific tests |
| `TUnit.Assertions.Tests` | Assertion library tests |
| `TUnit.Core.SourceGenerator.Tests` | **Snapshot tests for source generator** |
| `TUnit.PublicAPI` | **Snapshot tests for public API** |

### Roslyn Version Projects

- `*.Roslyn414`, `*.Roslyn44`, `*.Roslyn47`: Multi-targeting for Roslyn API versions
- Ensures compatibility across Visual Studio and .NET SDK versions

---

## 💻 Code Style (REQUIRED)

### Modern C# Syntax (Mandatory)

```csharp
// ✅ Collection expressions (C# 12+)
List<string> items = [];
string[] array = ["a", "b", "c"];

// ❌ WRONG: Old syntax
List<string> items = new List<string>();

// ✅ var for obvious types
var testName = GetTestName();

// ✅ Always use braces
if (condition)
{
    DoSomething();
}

// ❌ WRONG: No braces
if (condition)
    DoSomething();

// ✅ File-scoped namespaces
namespace TUnit.Core.Features;

public class MyClass { }

// ✅ Pattern matching
if (obj is TestContext context)
{
    ProcessContext(context);
}

// ✅ Switch expressions
var result = status switch
{
    TestStatus.Passed => "✓",
    TestStatus.Failed => "✗",
    _ => "?"
};

// ✅ Raw string literals
string code = """
    public void Test() { }
    """;
```

### Naming Conventions

```csharp
// Public: PascalCase
public string TestName { get; }

// Private fields: _camelCase
private readonly IExecutor _executor;

// Local: camelCase
var testContext = new TestContext();

// Async: Async suffix
public async Task<TestResult> ExecuteTestAsync(CancellationToken ct) { }
```

### Async Patterns

```csharp
// ✅ ValueTask for potentially-sync operations
public ValueTask<TestResult> ExecuteAsync(CancellationToken ct)
{
    if (IsCached)
        return new ValueTask<TestResult>(cachedResult);

    return ExecuteAsyncCore(ct);
}

// ✅ Always accept CancellationToken
public async Task<T> RunAsync(CancellationToken cancellationToken) { }

// ❌ NEVER block on async
var result = ExecuteAsync().Result;              // DEADLOCK RISK
var result = ExecuteAsync().GetAwaiter().GetResult();  // DEADLOCK RISK
```

### Performance Patterns

```csharp
// ✅ Cache reflection results
private static readonly Dictionary<Type, MethodInfo[]> TestMethodCache = new();

// ✅ Object pooling
private static readonly ObjectPool<StringBuilder> StringBuilderPool =
    ObjectPool.Create<StringBuilder>();

// ✅ Span<T> to avoid allocations
public void ProcessTestName(ReadOnlySpan<char> name) { }

// ✅ ArrayPool for temporary buffers
var buffer = ArrayPool<byte>.Shared.Rent(size);
try { /* use buffer */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }
```

### Anti-Patterns (NEVER DO THIS)

```csharp
// ❌ Catching all exceptions without re-throw
try { } catch (Exception) { }  // Swallows errors

// ❌ LINQ in hot paths
var count = tests.Where(t => t.IsPassed).Count();

// ✅ Use loops instead
int count = 0;
foreach (var test in tests)
    if (test.IsPassed) count++;

// ❌ String concatenation in loops
string result = "";
foreach (var item in items) result += item;

// ✅ StringBuilder
var builder = new StringBuilder();
foreach (var item in items) builder.Append(item);
```

---

## 🔄 Development Workflows

### Adding a New Feature

**Decision Tree**:
```
Is this a new feature?
  ├─► YES
  │   ├─► Does it require dual-mode implementation?
  │   │   ├─► YES: Implement in BOTH source-gen AND reflection
  │   │   └─► NO: Still verify both modes aren't affected
  │   │
  │   ├─► Does it change public API?
  │   │   └─► YES: Run TUnit.PublicAPI tests + accept snapshots
  │   │
  │   ├─► Does it change source generator output?
  │   │   └─► YES: Run TUnit.Core.SourceGenerator.Tests + accept snapshots
  │   │
  │   ├─► Does it touch hot paths?
  │   │   └─► YES: Profile before/after, benchmark
  │   │
  │   └─► Does it use reflection?
  │       └─► YES: Test with AOT (dotnet publish -p:PublishAot=true)
  │
  └─► NO: (Continue to bug fix workflow)
```

**Step-by-Step**:
1. **Write tests FIRST** (TDD)
2. Implement in `TUnit.Core` (if new abstractions needed)
3. Implement in `TUnit.Core.SourceGenerator` (source-gen path)
4. Implement in `TUnit.Engine` (reflection path)
5. Add analyzer rule (if misuse is possible)
6. Run all tests: `dotnet test`
7. Accept snapshots if needed (see Rule 2)
8. Benchmark if touching hot paths
9. Test AOT if using reflection

### Fixing a Bug

**Step-by-Step**:
1. **Write failing test** that reproduces the bug
2. Identify affected execution mode(s)
3. Fix in source generator (if affected)
4. Fix in reflection engine (if affected)
5. Verify both modes pass the test
6. Run full test suite: `dotnet test`
7. Accept snapshots if applicable
8. Check for performance regression (if in hot path)

---

## 🎯 Common Patterns

### Implementing Dual-Mode Feature

```csharp
// 1. Define abstraction in TUnit.Core
[AttributeUsage(AttributeTargets.Method)]
public class BeforeAllTestsAttribute : Attribute { }

// 2. Implement in TUnit.Core.SourceGenerator
// Generated code:
// await MyTestClass.GlobalSetup();

// 3. Implement in TUnit.Engine (reflection)
public class ReflectionTestDiscoverer
{
    private async Task DiscoverHooksAsync(Type testClass)
    {
        var hookMethods = testClass.GetMethods()
            .Where(m => m.GetCustomAttribute<BeforeAllTestsAttribute>() != null);

        foreach (var method in hookMethods)
            RegisterHook(method);
    }
}

// 4. Test BOTH modes
[Test]
[Arguments(ExecutionMode.SourceGenerated)]
[Arguments(ExecutionMode.Reflection)]
public async Task BeforeAllTestsHook_ExecutesOnce(ExecutionMode mode) { }
```

### Adding Analyzer Rule

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestMethodMustBePublicAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TUNIT0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Test method must be public",
        messageFormat: "Test method '{0}' must be public",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.GetAttributes().Any(a => a.AttributeClass?.Name == "TestAttribute"))
        {
            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule, method.Locations[0], method.Name));
            }
        }
    }
}
```

### Adding Assertion

```csharp
public static class NumericAssertions
{
    public static InvokableValueAssertionBuilder<TActual> IsPositive<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(
            new DelegateAssertion<TActual, TActual>(
                (value, _, _) =>
                {
                    if (value.CompareTo(default!) <= 0)
                        return AssertionResult.Failed($"Expected positive value but was {value}");
                    return AssertionResult.Passed;
                },
                (actual, expected) => $"{actual} is positive"));
    }
}

// Usage:
await Assert.That(value).IsPositive();
```

---

## 🐛 Troubleshooting

### Snapshot Tests Failing

**Symptom**: `TUnit.Core.SourceGenerator.Tests` or `TUnit.PublicAPI` failing

**Solution**:
```bash
# 1. Review .received.txt files
cd TUnit.Core.SourceGenerator.Tests  # or TUnit.PublicAPI
ls *.received.txt

# 2. If changes are intentional:
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# 3. Commit .verified.txt files
git add *.verified.txt
git commit -m "Update snapshots: [reason]"

# 4. NEVER commit .received.txt
git status  # Verify no .received.txt staged
```

### Tests Pass Locally, Fail in CI

**Common Causes**:
1. Forgot to commit `.verified.txt` files
2. Line ending differences (CRLF vs LF)
3. Race conditions in parallel tests
4. Missing dependencies

**Solution**:
```bash
# Check for uncommitted snapshots
git status | grep verified.txt

# Check line endings
git config core.autocrlf

# Run with same parallelization as CI
dotnet test --parallel
```

### Dual-Mode Behavior Differs

**Diagnostic**:
```bash
# Check generated code
# obj/Debug/net9.0/generated/TUnit.Core.SourceGenerator/

# Set breakpoint in TUnit.Engine for reflection path

# Common issues:
# - Attribute not checked in reflection
# - Different data expansion logic
# - Missing hook invocation
```

**Solution**: Implement missing logic in other execution mode

### AOT Compilation Fails

**Common Causes**:
1. Dynamic code generation (not AOT-compatible)
2. Reflection without proper annotations
3. Missing `[DynamicallyAccessedMembers]`

**Solution**: Add proper annotations (see Rule 5 above)

### Performance Regression

**Diagnostic**:
```bash
# Benchmark
cd TUnit.Performance.Tests
dotnet run -c Release --framework net9.0

# Profile
dotnet trace collect -- dotnet test
```

**Common Causes**:
- LINQ in hot path → use loops
- Missing reflection cache
- Unnecessary allocations → use object pooling
- Blocking on async

---

## ✅ Pre-Commit Checklist

**Before committing, verify ALL items**:

```
┌─────────────────────────────────────────────────────────┐
│ □ All tests pass: dotnet test                          │
│                                                         │
│ □ If source generator changed:                         │
│   • Ran TUnit.Core.SourceGenerator.Tests               │
│   • Reviewed .received.txt files                       │
│   • Accepted snapshots (.verified.txt)                 │
│   • Committed .verified.txt files                      │
│                                                         │
│ □ If public API changed:                               │
│   • Ran TUnit.PublicAPI tests                          │
│   • Reviewed .received.txt files                       │
│   • Accepted snapshots (.verified.txt)                 │
│   • Committed .verified.txt files                      │
│                                                         │
│ □ If dual-mode feature:                                │
│   • Implemented in BOTH source-gen AND reflection      │
│   • Tested both modes explicitly                       │
│   • Verified identical behavior                        │
│                                                         │
│ □ If performance-critical:                             │
│   • Profiled before/after                              │
│   • No performance regression                          │
│   • Minimized allocations                              │
│                                                         │
│ □ If touching reflection:                              │
│   • Tested AOT: dotnet publish -p:PublishAot=true      │
│   • Added DynamicallyAccessedMembers annotations       │
│                                                         │
│ □ Code follows style guide                             │
│ □ No breaking changes (or major version bump)          │
└─────────────────────────────────────────────────────────┘
```

---

## 🎓 Philosophy

**TUnit Core Principles**:

1. **Fast**: Performance is not optional. Millions of tests depend on it.
2. **Modern**: Latest .NET features, AOT support, C# 12+ syntax.
3. **Reliable**: Dual-mode parity, comprehensive tests, API stability.
4. **Enjoyable**: Great error messages, intuitive API, minimal boilerplate.

**Decision Framework**:

When considering any change, ask:

> "Does this make TUnit faster, more modern, more reliable, or more enjoyable to use?"

If the answer is **NO** → reconsider the change.

---

## 📚 Additional Resources

- **Documentation**: https://tunit.dev
- **Contributing**: `.github/CONTRIBUTING.md`
- **Issues**: https://github.com/thomhurst/TUnit/issues
- **Discussions**: https://github.com/thomhurst/TUnit/discussions
- **Detailed Guide**: `.github/copilot-instructions.md`

---

## 🤖 LLM-Specific Notes

**For AI Assistants**:

1. **Always check Rule 1-5 first** before making changes
2. **Use Quick Reference Card** for common commands
3. **Follow Decision Trees** in Development Workflows section
4. **Consult Common Patterns** for implementation templates
5. **Run Pre-Commit Checklist** before suggesting changes to commit

**High-Level Decision Process**:
```
User Request
    │
    ├─► Identify category: feature, bug, refactor
    │
    ├─► Check if dual-mode implementation needed
    │
    ├─► Check if snapshots need updating
    │
    ├─► Implement with required style/patterns
    │
    ├─► Run tests + accept snapshots
    │
    └─► Verify pre-commit checklist
```

**Common Mistakes to Avoid**:
1. ❌ Implementing only one execution mode
2. ❌ Forgetting to update snapshots
3. ❌ Using old C# syntax
4. ❌ Adding allocations in hot paths
5. ❌ Using VSTest APIs
6. ❌ Blocking on async code
7. ❌ Committing .received.txt files

---

**Last Updated**: 2025-01-28
**Version**: 2.0 (LLM-optimized)
