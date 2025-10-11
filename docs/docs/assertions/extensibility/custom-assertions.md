---
sidebar_position: 1
---

# Custom Assertions

The TUnit Assertions can be easily extended so that you can create your own assertions.

## Creating a Custom Assertion

To create a custom assertion, you need to:

1. **Create an Assertion Class** that inherits from `Assertion<TValue>`
2. **Implement the required methods**
3. **Create an extension method** on `IAssertionSource<T>`

### Step 1: Create an Assertion Class

Your assertion class should inherit from `Assertion<TValue>` where `TValue` is the type being asserted.

```csharp
using TUnit.Assertions.Core;

public class StringContainsAssertion : Assertion<string>
{
    private readonly string _expected;
    private readonly StringComparison _comparison;

    public StringContainsAssertion(
        EvaluationContext<string> context,
        string expected,
        StringBuilder expressionBuilder,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context, expressionBuilder)
    {
        _expected = expected ?? throw new ArgumentNullException(nameof(expected));
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (value.Contains(_expected, _comparison))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"'{value}' does not contain '{_expected}'"));
    }

    protected override string GetExpectation()
        => $"to contain \"{_expected}\"";
}
```

### Step 2: Create an Extension Method

Create an extension method on `IAssertionSource<T>` that returns your custom assertion:

```csharp
using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;

public static class StringAssertionExtensions
{
    public static StringContainsAssertion ContainsIgnoreCase(
        this IAssertionSource<string> source,
        string expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        source.ExpressionBuilder.Append($".ContainsIgnoreCase({expression})");
        return new StringContainsAssertion(
            source.Context,
            expected,
            source.ExpressionBuilder,
            StringComparison.OrdinalIgnoreCase);
    }
}
```

### Step 3: Use Your Custom Assertion

```csharp
await Assert.That("Hello World")
    .ContainsIgnoreCase("WORLD");  // Uses your custom assertion!
```

## Chaining with And/Or

Because your assertion returns an `Assertion<T>` type, it automatically supports chaining with `.And` and `.Or`:

```csharp
await Assert.That("Hello World")
    .ContainsIgnoreCase("WORLD")
    .And
    .ContainsIgnoreCase("HELLO");
```

## Key Points

- **Extension target**: Always extend `IAssertionSource<T>` so your method works on assertions, And, and Or continuations
- **Append expression**: Call `source.ExpressionBuilder.Append(...)` to build helpful error messages
- **Return your assertion**: Return a new instance of your custom `Assertion<T>` subclass
- **Context sharing**: Pass `source.Context` and `source.ExpressionBuilder` to your assertion constructor
- **CallerArgumentExpression**: Use this attribute to capture parameter expressions for better error messages
