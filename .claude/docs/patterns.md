# Code Patterns

## Adding Assertions

IMPORTANT: **Prefer `[GenerateAssertion]` for simplicity.** Use manual patterns only for complex cases.

### Preferred: GenerateAssertion Attribute

```csharp
// Simple and recommended approach
public static class StringAssertions
{
    [GenerateAssertion]
    public static bool IsUpperCase(this string value)
    {
        return value.All(char.IsUpper);
    }
}

// Usage:
await Assert.That(myString).IsUpperCase();
```

### Advanced: Manual Assertion Pattern

Only use when `[GenerateAssertion]` is insufficient (custom error messages, complex logic):

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

## Implementing Dual-Mode Features

Only needed for core engine metadata collection. See `mandatory-rules.md` for when this applies.

### Step 1: Define Abstraction in TUnit.Core

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class BeforeAllTestsAttribute : Attribute { }
```

### Step 2: Implement in Source Generator

In `TUnit.Core.SourceGenerator`:

```csharp
// Generated code example:
// await MyTestClass.GlobalSetup();
```

### Step 3: Implement in Reflection Engine

In `TUnit.Engine`:

```csharp
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
```

### Step 4: Test Both Modes

```csharp
[Test]
[Arguments(ExecutionMode.SourceGenerated)]
[Arguments(ExecutionMode.Reflection)]
public async Task BeforeAllTestsHook_ExecutesOnce(ExecutionMode mode) { }
```

---

## Adding Analyzer Rules

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
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

---

## Async Patterns

```csharp
// ValueTask for potentially-sync operations
public ValueTask<TestResult> ExecuteAsync(CancellationToken ct)
{
    if (IsCached)
        return new ValueTask<TestResult>(cachedResult);

    return ExecuteAsyncCore(ct);
}

// Always accept CancellationToken
public async Task<T> RunAsync(CancellationToken cancellationToken) { }

// NEVER block on async
// var result = ExecuteAsync().Result;                    // DEADLOCK RISK
// var result = ExecuteAsync().GetAwaiter().GetResult();  // DEADLOCK RISK
```
