# Chaining and Converting

TUnit allows you to chain assertions that change the type being asserted, enabling fluent and expressive test code.
This is useful when an assertion transforms the value (e.g., parsing a response), and you want to continue asserting on the new type.

Chaining is especially helpful when you want to perform multiple assertions on a value that is transformed by a previous assertion, without having to create intermediate variables.

For example:

```csharp
        HttpResponseMessage response = ...;

        await Assert.That(response)
            .IsProblemDetails()
            .And
            .HasTitle("Invalid Authentication Token")
            .And
            .HasDetail("No token provided");
```

The `response` object initially passed in is a `HttpResponseMessage`, but then after we assert it's a `ProblemDetails` object, the chain has changed to that type so that we can further assert with methods specific to `ProblemDetails` instead of `HttpResponseMessage`.

## How to Implement Type Conversion Assertions

Creating a type-converting assertion involves two main steps:

### 1. Create the Assertion Class

Create a custom assertion class that extends `Assertion<TTo>` where `TTo` is the target type. The constructor should take `AssertionContext<TFrom>` (the source type) and use `.Map<TTo>(...)` to transform the value.

In the example above, that'd look like:

```csharp
public class IsProblemDetailsAssertion : Assertion<ProblemDetails>
{
    public IsProblemDetailsAssertion(AssertionContext<HttpResponseMessage> context)
        : base(context.Map<ProblemDetails>(async response =>
        {
            var content = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            if (content is null)
            {
                throw new InvalidOperationException("Response body is not Problem Details");
            }

            return content;
        }))
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<ProblemDetails> metadata)
    {
        // The transformation already happened in the Map function
        // If we got here without exception, the conversion succeeded
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(metadata.Exception.Message));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation()
    {
        return "HTTP response to be in the format of a Problem Details object";
    }
}
```

The `.Map<TTo>(...)` method handles the type conversion. If the conversion fails, throw an exception which will be captured and reported as an assertion failure.

**Note:** The `Map` method supports both synchronous and asynchronous transformations:
- **Synchronous**: `context.Map<TTo>(value => transformedValue)`
- **Asynchronous**: `context.Map<TTo>(async value => await transformedValueAsync)`

In both cases, the Task is automatically unwrapped, allowing you to chain assertions directly on the result type.

### 2. Create the Extension Method

Create an extension method on `IAssertionSource<TFrom>` that returns your assertion class:

```csharp
public static class HttpResponseAssertionExtensions
{
    public static IsProblemDetailsAssertion IsProblemDetails(
        this IAssertionSource<HttpResponseMessage> source)
    {
        source.Context.ExpressionBuilder.Append(".IsProblemDetails()");
        return new IsProblemDetailsAssertion(source.Context);
    }
}
```

That's it!

Now any assertions built for the `ProblemDetails` type will work off of that same chain.

### 3. Create Assertions for the Target Type

You can then create standard assertions for the target type (`ProblemDetails` in this case):

```csharp
public static class ProblemDetailsAssertionExtensions
{
    public static HasTitleAssertion HasTitle(
        this IAssertionSource<ProblemDetails> source,
        string expectedTitle,
        [CallerArgumentExpression(nameof(expectedTitle))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".HasTitle({expression})");
        return new HasTitleAssertion(source.Context, expectedTitle);
    }

    public static HasDetailAssertion HasDetail(
        this IAssertionSource<ProblemDetails> source,
        string expectedDetail,
        [CallerArgumentExpression(nameof(expectedDetail))] string? expression = null)
    {
        source.Context.ExpressionBuilder.Append($".HasDetail({expression})");
        return new HasDetailAssertion(source.Context, expectedDetail);
    }
}

public class HasTitleAssertion : Assertion<ProblemDetails>
{
    private readonly string _expectedTitle;

    public HasTitleAssertion(AssertionContext<ProblemDetails> context, string expectedTitle)
        : base(context)
    {
        _expectedTitle = expectedTitle;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<ProblemDetails> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed("ProblemDetails is null"));
        }

        if (metadata.Value?.Title != _expectedTitle)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"Expected title '{_expectedTitle}' but was '{metadata.Value?.Title}'"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to have title '{_expectedTitle}'";
}

public class HasDetailAssertion : Assertion<ProblemDetails>
{
    private readonly string _expectedDetail;

    public HasDetailAssertion(AssertionContext<ProblemDetails> context, string expectedDetail)
        : base(context)
    {
        _expectedDetail = expectedDetail;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<ProblemDetails> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed("ProblemDetails is null"));
        }

        if (metadata.Value?.Detail != _expectedDetail)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"Expected detail '{_expectedDetail}' but was '{metadata.Value?.Detail}'"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to have detail '{_expectedDetail}'";
}
```

## Built-in Examples

TUnit includes several built-in examples of type conversion assertions:

- `WhenParsedInto<T>()` - Converts a string to a parsed type (e.g., `await Assert.That("123").WhenParsedInto<int>().IsEqualTo(123)`)
- `IsTypeOf<T>()` - Converts to a specific type (e.g., `await Assert.That(obj).IsTypeOf<StringBuilder>().Length().IsEqualTo(5)`)
