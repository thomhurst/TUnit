# Code Patterns

Common implementation patterns for TUnit development.

---

## Adding Assertions

Prefer `[GenerateAssertion]` for simplicity. Use manual patterns only for complex cases.

### Preferred: GenerateAssertion

```csharp
public static class StringAssertions
{
    [GenerateAssertion]
    public static bool IsUpperCase(this string value)
        => value.All(char.IsUpper);
}

// Usage:
await Assert.That(myString).IsUpperCase();
```

### Manual Pattern (Complex Cases Only)

Use when you need custom error messages or complex validation logic:

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
```

---

## Dual-Mode Features

Only needed for core engine metadata collection. See `mandatory-rules.md` for when this applies.

1. Define abstraction in `TUnit.Core`
2. Implement in `TUnit.Core.SourceGenerator`
3. Implement in `TUnit.Engine`
4. Test both modes:

```csharp
[Test]
[Arguments(ExecutionMode.SourceGenerated)]
[Arguments(ExecutionMode.Reflection)]
public async Task MyFeature_WorksInBothModes(ExecutionMode mode) { }
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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.GetAttributes().Any(a => a.AttributeClass?.Name == "TestAttribute")
            && method.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.Name));
        }
    }
}
```
