# Code Patterns

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

Only needed for core engine metadata collection. See `mandatory-rules.md`.

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

See existing analyzers in `TUnit.Analyzers` for patterns.
