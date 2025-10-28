# TUnit Development Guide for AI Assistants

## Table of Contents
- [Critical Rules - READ FIRST](#critical-rules---read-first)
- [Quick Reference](#quick-reference)
- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [Development Workflow](#development-workflow)
- [Code Style Standards](#code-style-standards)
- [Testing Guidelines](#testing-guidelines)
- [Performance Requirements](#performance-requirements)
- [Common Patterns](#common-patterns)
- [Troubleshooting](#troubleshooting)

---

## Critical Rules - READ FIRST

### The Five Commandments

1. **DUAL-MODE IMPLEMENTATION IS MANDATORY**
   - Every feature MUST work identically in both execution modes:
     - **Source-Generated Mode**: Compile-time code generation via `TUnit.Core.SourceGenerator`
     - **Reflection Mode**: Runtime discovery via `TUnit.Engine`
   - Test both modes explicitly. Never assume parity without verification.
   - If you only implement in one mode, the feature is incomplete and MUST NOT be merged.

2. **SNAPSHOT TESTS ARE NON-NEGOTIABLE**
   - After ANY change to source generator output:
     ```bash
     dotnet test TUnit.Core.SourceGenerator.Tests
     # Review .received.txt files, then:
     for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done  # Linux/macOS
     for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"  # Windows
     ```
   - After ANY public API change (TUnit.Core, TUnit.Engine, TUnit.Assertions):
     ```bash
     dotnet test TUnit.PublicAPI
     # Review and accept snapshots as above
     ```
   - Commit ALL `.verified.txt` files. These are the source of truth.

3. **NEVER USE VSTest APIs**
   - This project uses **Microsoft.Testing.Platform** exclusively
   - VSTest is legacy and incompatible with TUnit's architecture
   - If you see `Microsoft.VisualStudio.TestPlatform`, it's wrong

4. **PERFORMANCE IS A FEATURE**
   - TUnit is used by millions of tests daily
   - Every allocation in discovery/execution hot paths matters
   - Profile before and after for any changes in critical paths
   - Use `ValueTask`, object pooling, and cached reflection

5. **AOT/TRIMMING COMPATIBILITY IS REQUIRED**
   - All code must work with Native AOT and IL trimming
   - Use `[DynamicallyAccessedMembers]` and `[UnconditionalSuppressMessage]` appropriately
   - Test changes with AOT-compiled projects when touching reflection

---

## Quick Reference

### Most Common Commands
```bash
# Run all tests
dotnet test

# Test source generator + accept snapshots
dotnet test TUnit.Core.SourceGenerator.Tests
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Test public API + accept snapshots
dotnet test TUnit.PublicAPI
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Run specific test by tree node filter
dotnet test -- --treenode-filter "/Assembly/Namespace/ClassName/TestName"

# Run tests excluding performance tests
dotnet test -- --treenode-filter "/*/*/*/*[Category!=Performance]"

# Build in release mode
dotnet build -c Release

# Test AOT compilation
dotnet publish -c Release -p:PublishAot=true
```

### Snapshot Workflow Quick Ref
```
┌─────────────────────────────────────────────────────────┐
│ 1. Make change to source generator or public API       │
│ 2. Run relevant test: dotnet test [Project]            │
│ 3. If snapshots differ, review .received.txt files     │
│ 4. If changes are correct, rename to .verified.txt     │
│ 5. Commit .verified.txt files                          │
│ 6. NEVER commit .received.txt files                    │
└─────────────────────────────────────────────────────────┘
```

### Test Filter Syntax
```bash
# Single test
--treenode-filter "/TUnit.TestProject/Namespace/ClassName/TestMethodName"

# All tests in a class
--treenode-filter "/*/*/ClassName/*"

# Multiple patterns (OR logic)
--treenode-filter "Pattern1|Pattern2"

# Exclude by category
--treenode-filter "/*/*/*/*[Category!=Performance]"
```

---

## Project Overview

### What is TUnit?

TUnit is a **modern .NET testing framework** that prioritizes:
- **Performance**: Source-generated tests, parallel by default
- **Modern .NET**: Native AOT, trimming, latest C# features
- **Microsoft.Testing.Platform**: Not VSTest (legacy)
- **Developer Experience**: Fluent assertions, minimal boilerplate

### Key Differentiators
- **Compile-time test discovery** via source generators
- **Parallel execution** by default with dependency management
- **Dual execution modes** (source-gen + reflection) for flexibility
- **Built-in assertions** with detailed failure messages
- **Property-based testing** support
- **Dynamic test variants** for data-driven scenarios

---

## Architecture

### Execution Modes

TUnit has two execution paths that **MUST** behave identically:

```
┌─────────────────────────────────────────────────────────────────┐
│                    USER TEST CODE                               │
│  [Test] public void MyTest() { ... }                           │
└────────────┬────────────────────────────────┬───────────────────┘
             │                                │
             ▼                                ▼
    ┌────────────────────┐         ┌─────────────────────┐
    │  SOURCE-GENERATED  │         │  REFLECTION MODE    │
    │       MODE         │         │                     │
    │                    │         │                     │
    │ TUnit.Core.        │         │  TUnit.Engine       │
    │   SourceGenerator  │         │                     │
    │                    │         │                     │
    │ Generates code at  │         │ Discovers tests at  │
    │ compile time       │         │ runtime via         │
    │                    │         │ reflection          │
    └─────────┬──────────┘         └──────────┬──────────┘
              │                               │
              │                               │
              └───────────────┬───────────────┘
                              ▼
                    ┌──────────────────┐
                    │   TUnit.Engine   │
                    │   (Execution)    │
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ Microsoft.Testing│
                    │    .Platform     │
                    └──────────────────┘
```

### Core Projects

| Project | Purpose | Notes |
|---------|---------|-------|
| **TUnit.Core** | Abstractions, interfaces, attributes | Public API surface |
| **TUnit.Engine** | Test discovery & execution (reflection) | Runtime path |
| **TUnit.Core.SourceGenerator** | Compile-time test generation | Compile-time path |
| **TUnit.Assertions** | Fluent assertion library | Separate from core |
| **TUnit.Assertions.SourceGenerator** | Generates custom assertions | Extensibility |
| **TUnit.Analyzers** | Roslyn analyzers & code fixes | Compile-time safety |
| **TUnit.PropertyTesting** | Property-based testing support | New feature |
| **TUnit.Playwright** | Playwright integration | Browser testing |

### Roslyn Version Projects
- `*.Roslyn414`, `*.Roslyn44`, `*.Roslyn47`: Multi-targeting for different Roslyn versions
- Ensures compatibility across VS versions and .NET SDK versions

### Test Projects
- **TUnit.TestProject**: Integration tests (uses TUnit to test itself)
- **TUnit.Engine.Tests**: Engine-specific tests
- **TUnit.Assertions.Tests**: Assertion library tests
- **TUnit.Core.SourceGenerator.Tests**: Source generator snapshot tests
- **TUnit.PublicAPI**: Public API snapshot tests (prevents breaking changes)

---

## Development Workflow

### Adding a New Feature

#### Step-by-Step Process

1. **Design Phase**
   ```
   ┌─────────────────────────────────────────────────────┐
   │ Ask yourself:                                       │
   │ • Does this require dual-mode implementation?       │
   │ • Will this affect public API?                      │
   │ • Does this need an analyzer rule?                  │
   │ • What's the performance impact?                    │
   │ • Is this AOT/trimming compatible?                  │
   └─────────────────────────────────────────────────────┘
   ```

2. **Implementation**
   - **Write tests FIRST** (TDD approach)
   - Implement in `TUnit.Core` (if new abstractions needed)
   - Implement in `TUnit.Core.SourceGenerator` (source-gen path)
   - Implement in `TUnit.Engine` (reflection path)
   - Add analyzer rule if misuse is possible

3. **Verification**
   ```bash
   # Run all tests
   dotnet test

   # If source generator changed, accept snapshots
   cd TUnit.Core.SourceGenerator.Tests
   dotnet test
   # Review .received.txt files
   for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

   # If public API changed, accept snapshots
   cd TUnit.PublicAPI
   dotnet test
   # Review .received.txt files
   for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done
   ```

4. **Performance Check**
   ```bash
   # Run benchmarks (if touching hot paths)
   cd TUnit.Performance.Tests
   dotnet run -c Release --framework net9.0
   ```

5. **AOT Verification** (if touching reflection)
   ```bash
   cd TUnit.TestProject
   dotnet publish -c Release -p:PublishAot=true --use-current-runtime
   ```

### Fixing a Bug

#### Step-by-Step Process

1. **Reproduce**
   - Write a failing test that demonstrates the bug
   - Identify which execution mode(s) are affected

2. **Fix**
   - Fix in source generator (if affected)
   - Fix in reflection engine (if affected)
   - Ensure both modes now pass the test

3. **Verify No Regression**
   ```bash
   # Run full test suite
   dotnet test

   # Check performance hasn't regressed
   # (if fix is in hot path)
   ```

4. **Accept Snapshots** (if applicable)
   - Follow snapshot workflow above

---

## Code Style Standards

### Modern C# - Required Syntax

```csharp
// ✅ CORRECT: Use collection expressions (C# 12+)
List<string> items = [];
string[] array = ["a", "b", "c"];
Dictionary<string, int> dict = [];

// ❌ WRONG: Don't use old initialization syntax
List<string> items = new List<string>();
string[] array = new string[] { "a", "b", "c" };

// ✅ CORRECT: Use var when type is obvious
var testName = GetTestName();
var results = ExecuteTests();

// ❌ WRONG: Explicit types for obvious cases
string testName = GetTestName();
List<TestResult> results = ExecuteTests();

// ✅ CORRECT: Always use braces, even for single lines
if (condition)
{
    DoSomething();
}

// ❌ WRONG: No braces
if (condition)
    DoSomething();

// ✅ CORRECT: File-scoped namespaces
namespace TUnit.Core.Features;

public class MyClass { }

// ❌ WRONG: Traditional namespace blocks (unless multiple namespaces in file)
namespace TUnit.Core.Features
{
    public class MyClass { }
}

// ✅ CORRECT: Pattern matching
if (obj is TestContext context)
{
    ProcessContext(context);
}

// ✅ CORRECT: Switch expressions
var result = status switch
{
    TestStatus.Passed => "✓",
    TestStatus.Failed => "✗",
    TestStatus.Skipped => "⊘",
    _ => "?"
};

// ✅ CORRECT: Target-typed new
TestContext context = new(testName, metadata);

// ✅ CORRECT: Record types for immutable data
public record TestMetadata(string Name, string FilePath, int LineNumber);

// ✅ CORRECT: Required properties (C# 11+)
public required string TestName { get; init; }

// ✅ CORRECT: Raw string literals for multi-line strings
string code = """
    public void TestMethod()
    {
        Assert.That(value).IsEqualTo(expected);
    }
    """;
```

### Naming Conventions

```csharp
// Public members: PascalCase
public string TestName { get; }
public void ExecuteTest() { }
public const int MaxRetries = 3;

// Private fields: _camelCase
private readonly ITestExecutor _executor;
private string _cachedResult;

// Local variables: camelCase
var testContext = new TestContext();
int retryCount = 0;

// Type parameters: T prefix for single, descriptive for multiple
public class Repository<T> { }
public class Converter<TSource, TDestination> { }

// Interfaces: I prefix
public interface ITestExecutor { }

// Async methods: Async suffix
public async Task<TestResult> ExecuteTestAsync(CancellationToken ct) { }
```

### Async/Await Patterns

```csharp
// ✅ CORRECT: Use ValueTask for potentially sync operations
public ValueTask<TestResult> ExecuteAsync(CancellationToken ct)
{
    if (IsCached)
    {
        return new ValueTask<TestResult>(cachedResult);
    }

    return ExecuteAsyncCore(ct);
}

// ✅ CORRECT: Always accept CancellationToken
public async Task<TestResult> RunTestAsync(CancellationToken cancellationToken)
{
    await PrepareAsync(cancellationToken);
    return await ExecuteAsync(cancellationToken);
}

// ✅ CORRECT: ConfigureAwait(false) in library code
var result = await ExecuteAsync().ConfigureAwait(false);

// ❌ WRONG: NEVER block on async code
var result = ExecuteAsync().Result;  // DEADLOCK RISK
var result = ExecuteAsync().GetAwaiter().GetResult();  // DEADLOCK RISK
```

### Nullable Reference Types

```csharp
// ✅ CORRECT: Explicit nullability annotations
public string? TryGetTestName(TestContext context)
{
    return context.Metadata?.Name;
}

// ✅ CORRECT: Null-forgiving operator when you know it's safe
var testName = context.Metadata!.Name;

// ✅ CORRECT: Null-coalescing
var name = context.Metadata?.Name ?? "UnknownTest";

// ✅ CORRECT: Required non-nullable properties
public required string TestName { get; init; }
```

### Performance-Critical Code

```csharp
// ✅ CORRECT: Object pooling for frequently allocated objects
private static readonly ObjectPool<StringBuilder> StringBuilderPool =
    ObjectPool.Create<StringBuilder>();

public string BuildMessage()
{
    var builder = StringBuilderPool.Get();
    try
    {
        builder.Append("Test: ");
        builder.Append(TestName);
        return builder.ToString();
    }
    finally
    {
        builder.Clear();
        StringBuilderPool.Return(builder);
    }
}

// ✅ CORRECT: Span<T> for stack-allocated buffers
Span<char> buffer = stackalloc char[256];

// ✅ CORRECT: Avoid allocations in hot paths
// Cache reflection results
private static readonly MethodInfo ExecuteMethod =
    typeof(TestRunner).GetMethod(nameof(Execute))!;

// ✅ CORRECT: Use static readonly for constant data
private static readonly string[] ReservedNames = ["Test", "Setup", "Cleanup"];
```

### Anti-Patterns to Avoid

```csharp
// ❌ WRONG: Catching generic exceptions without re-throwing
try { }
catch (Exception) { } // Swallows all errors

// ✅ CORRECT: Catch specific exceptions or re-throw
try { }
catch (InvalidOperationException ex)
{
    Log(ex);
    throw;
}

// ❌ WRONG: Using Task.Run in library code (pushes threading choice to consumers)
public Task DoWorkAsync() => Task.Run(() => DoWork());

// ✅ CORRECT: Properly async all the way
public async Task DoWorkAsync() => await ActualAsyncWork();

// ❌ WRONG: Unnecessary LINQ in hot paths
var count = tests.Where(t => t.IsPassed).Count();

// ✅ CORRECT: Direct iteration
int count = 0;
foreach (var test in tests)
{
    if (test.IsPassed) count++;
}

// ❌ WRONG: String concatenation in loops
string result = "";
foreach (var item in items)
{
    result += item;
}

// ✅ CORRECT: StringBuilder or collection expressions
var builder = new StringBuilder();
foreach (var item in items)
{
    builder.Append(item);
}
```

---

## Testing Guidelines

### Test Categories

1. **Unit Tests** (`TUnit.Core.Tests`, `TUnit.UnitTests`)
   - Test individual components in isolation
   - Fast execution, no external dependencies
   - Mock dependencies

2. **Integration Tests** (`TUnit.TestProject`, `TUnit.Engine.Tests`)
   - Test interactions between components
   - Use TUnit to test itself (dogfooding)
   - Verify dual-mode parity

3. **Snapshot Tests** (`TUnit.Core.SourceGenerator.Tests`, `TUnit.PublicAPI`)
   - Verify source generator output
   - Track public API surface
   - Prevent unintended breaking changes

4. **Performance Tests** (`TUnit.Performance.Tests`)
   - Benchmark critical paths
   - Compare against other frameworks
   - Track performance regressions

### Writing Tests

```csharp
// ✅ CORRECT: Descriptive test names
[Test]
public async Task ExecuteTest_WhenTestPasses_ReturnsPassedStatus()
{
    // Arrange
    var test = CreatePassingTest();

    // Act
    var result = await test.ExecuteAsync();

    // Assert
    await Assert.That(result.Status).IsEqualTo(TestStatus.Passed);
}

// ✅ CORRECT: Test both execution modes explicitly
[Test]
[Arguments(ExecutionMode.SourceGenerated)]
[Arguments(ExecutionMode.Reflection)]
public async Task MyFeature_WorksInBothModes(ExecutionMode mode)
{
    // Test implementation
}

// ✅ CORRECT: Use Categories for grouping
[Test]
[Category("Performance")]
public void MyPerformanceTest() { }

// Run without performance tests:
// dotnet test -- --treenode-filter "/*/*/*/*[Category!=Performance]"
```

### Snapshot Testing

```csharp
// In TUnit.Core.SourceGenerator.Tests

[Test]
public Task GeneratesCorrectCode_ForSimpleTest()
{
    string source = """
        using TUnit.Core;

        public class MyTests
        {
            [Test]
            public void SimpleTest() { }
        }
        """;

    return VerifySourceGenerator(source);
}

// This will:
// 1. Run the source generator
// 2. Capture the generated code
// 3. Compare to MyTestName.verified.txt
// 4. Create MyTestName.received.txt if different
// 5. Fail test if difference found
```

**Accepting Snapshots:**
```bash
# After verifying .received.txt files are correct:
cd TUnit.Core.SourceGenerator.Tests
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Commit the .verified.txt files
git add *.verified.txt
git commit -m "Update source generator snapshots"
```

---

## Performance Requirements

### Performance Budget

| Operation | Target | Critical |
|-----------|--------|----------|
| Test discovery | < 100ms per 1000 tests | Hot path |
| Test execution overhead | < 1ms per test | Hot path |
| Source generation | < 1s per 1000 tests | Compile-time |
| Memory per test | < 1KB average | At scale |

### Hot Paths (Optimize Aggressively)

1. **Test Discovery**
   - Source generator: Generating test registration code
   - Reflection engine: Scanning assemblies for test attributes

2. **Test Execution**
   - Test invocation
   - Assertion evaluation
   - Result collection

3. **Data Generation**
   - Argument expansion
   - Data source evaluation

### Performance Checklist

```
┌─────────────────────────────────────────────────────────┐
│ Before committing changes to hot paths:                │
│ □ Profiled with BenchmarkDotNet                        │
│ □ No new allocations in tight loops                    │
│ □ Reflection results cached                            │
│ □ String operations minimized                          │
│ □ LINQ avoided in hot paths (use loops)                │
│ □ ValueTask used for potentially sync operations       │
│ □ Compared before/after performance                    │
└─────────────────────────────────────────────────────────┘
```

### Performance Patterns

```csharp
// ✅ CORRECT: Cache reflection results
private static readonly Dictionary<Type, MethodInfo[]> TestMethodCache = new();

public MethodInfo[] GetTestMethods(Type type)
{
    if (!TestMethodCache.TryGetValue(type, out var methods))
    {
        methods = type.GetMethods()
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
            .ToArray();
        TestMethodCache[type] = methods;
    }
    return methods;
}

// ✅ CORRECT: Use spans to avoid allocations
public void ProcessTestName(ReadOnlySpan<char> name)
{
    // Work with span, no string allocation
}

// ✅ CORRECT: ArrayPool for temporary buffers
var buffer = ArrayPool<byte>.Shared.Rent(size);
try
{
    // Use buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

---

## Common Patterns

### Implementing Dual-Mode Features

#### Pattern: New Test Lifecycle Hook

```csharp
// 1. Define in TUnit.Core (abstraction)
namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public class BeforeAllTestsAttribute : Attribute
{
}

// 2. Implement in TUnit.Core.SourceGenerator
// Generates code like:
/*
await MyTestClass.GlobalSetup();
*/

// 3. Implement in TUnit.Engine (reflection)
public class ReflectionTestDiscoverer
{
    private async Task DiscoverHooksAsync(Type testClass)
    {
        var hookMethods = testClass.GetMethods()
            .Where(m => m.GetCustomAttribute<BeforeAllTestsAttribute>() != null);

        foreach (var method in hookMethods)
        {
            RegisterHook(method);
        }
    }
}

// 4. Write tests for BOTH modes
[Test]
[Arguments(ExecutionMode.SourceGenerated)]
[Arguments(ExecutionMode.Reflection)]
public async Task BeforeAllTestsHook_ExecutesOnce(ExecutionMode mode)
{
    // Test implementation
}
```

### Adding Analyzer Rules

```csharp
// TUnit.Analyzers/Rules/TestMethodMustBePublic.cs

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
                    Rule,
                    method.Locations[0],
                    method.Name));
            }
        }
    }
}
```

### Adding Assertions

```csharp
// TUnit.Assertions/Extensions/NumericAssertions.cs

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
                    {
                        return AssertionResult.Failed($"Expected positive value but was {value}");
                    }
                    return AssertionResult.Passed;
                },
                (actual, expected) => $"{actual} is positive"));
    }
}

// Usage:
await Assert.That(value).IsPositive();
```

---

## Troubleshooting

### Snapshot Tests Failing

**Problem**: `TUnit.Core.SourceGenerator.Tests` or `TUnit.PublicAPI` failing with "Snapshots don't match"

**Solution**:
```bash
# 1. Review the .received.txt files to see what changed
cd TUnit.Core.SourceGenerator.Tests  # or TUnit.PublicAPI
ls *.received.txt

# 2. If changes are intentional (you modified the generator or public API):
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# 3. Commit the updated .verified.txt files
git add *.verified.txt
git commit -m "Update snapshots after [your change]"

# 4. NEVER commit .received.txt files
git status  # Ensure no .received.txt files are staged
```

### Tests Pass Locally But Fail in CI

**Common Causes**:
1. **Snapshot mismatch**: Forgot to commit `.verified.txt` files
2. **Platform differences**: Line ending issues (CRLF vs LF)
3. **Timing issues**: Race conditions in parallel tests
4. **Environment differences**: Missing dependencies

**Solution**:
```bash
# Check for uncommitted snapshots
git status | grep verified.txt

# Check line endings
git config core.autocrlf  # Should be consistent

# Run tests with same parallelization as CI
dotnet test --parallel
```

### Dual-Mode Behavior Differs

**Problem**: Test passes in source-generated mode but fails in reflection mode (or vice versa)

**Diagnostic Process**:
```bash
# 1. Run test in specific mode
dotnet test -- --treenode-filter "/*/*/*/YourTest*"

# 2. Check generated code
# Look in obj/Debug/net9.0/generated/TUnit.Core.SourceGenerator/

# 3. Debug reflection path
# Set breakpoint in TUnit.Engine code

# 4. Common issues:
# - Attribute not checked in reflection path
# - Different data expansion logic
# - Missing hook invocation
# - Incorrect test metadata
```

**Solution**: Implement missing logic in the other execution mode

### AOT Compilation Fails

**Problem**: `dotnet publish -p:PublishAot=true` fails

**Common Causes**:
1. Dynamic code generation (not supported in AOT)
2. Reflection without proper annotations
3. Missing `[DynamicallyAccessedMembers]` attributes

**Solution**:
```csharp
// ✅ CORRECT: Annotate methods that use reflection
public void DiscoverTests(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    Type testClass)
{
    var methods = testClass.GetMethods();  // Safe - annotated
}

// ✅ CORRECT: Suppress warnings when you know it's safe
[UnconditionalSuppressMessage("Trimming", "IL2070",
    Justification = "Test methods are preserved by source generator")]
public void InvokeTestMethod(MethodInfo method) { }
```

### Performance Regression

**Problem**: Tests run slower after changes

**Diagnostic**:
```bash
# Run performance benchmarks
cd TUnit.Performance.Tests
dotnet run -c Release --framework net9.0

# Profile with dotnet-trace
dotnet trace collect -- dotnet test

# Analyze with PerfView or similar
```

**Common Causes**:
- Added LINQ in hot path (use loops instead)
- Missing caching of reflection results
- Unnecessary allocations (use object pooling)
- Synchronous blocking on async code

---

## Pre-Commit Checklist

Before committing ANY code, verify:

```
┌─────────────────────────────────────────────────────────────┐
│ □ All tests pass: dotnet test                              │
│ □ If source generator changed:                             │
│   • Ran TUnit.Core.SourceGenerator.Tests                   │
│   • Reviewed and accepted snapshots (.verified.txt)        │
│   • Committed .verified.txt files                          │
│ □ If public API changed:                                   │
│   • Ran TUnit.PublicAPI tests                              │
│   • Reviewed and accepted snapshots                        │
│   • Committed .verified.txt files                          │
│ □ If dual-mode feature:                                    │
│   • Implemented in BOTH source-gen and reflection          │
│   • Tested both modes explicitly                           │
│   • Verified identical behavior                            │
│ □ If performance-critical:                                 │
│   • Profiled before and after                              │
│   • No performance regression                              │
│   • Minimized allocations                                  │
│ □ If touching reflection:                                  │
│   • Tested with AOT: dotnet publish -p:PublishAot=true     │
│   • Added proper DynamicallyAccessedMembers annotations    │
│ □ Code follows style guide                                 │
│ □ No breaking changes (or documented if unavoidable)       │
└─────────────────────────────────────────────────────────────┘
```

---

## Additional Resources

- **Documentation**: https://tunit.dev
- **Contributing Guide**: `.github/CONTRIBUTING.md`
- **Issues**: https://github.com/thomhurst/TUnit/issues
- **Discussions**: https://github.com/thomhurst/TUnit/discussions

---

## Philosophy

TUnit aims to be: **fast, modern, reliable, and enjoyable to use**.

Every change should advance these goals:
- **Fast**: Optimize for performance. Millions of tests depend on it.
- **Modern**: Leverage latest .NET features. Support AOT, trimming, latest C#.
- **Reliable**: Dual-mode parity. Comprehensive tests. No breaking changes without major version bump.
- **Enjoyable**: Great error messages. Intuitive API. Minimal boilerplate.

When in doubt, ask: "Does this make TUnit faster, more modern, more reliable, or more enjoyable to use?"

If the answer is no, reconsider the change.
