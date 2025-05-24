# Chaining and Converting

TUnit allows you to chain assertions that change the type being asserted, enabling fluent and expressive test code.  
This is useful when an assertion transforms the value (e.g., parsing a response), and you want to continue asserting on the new type.

We may want to chain assertions together that change the type of object being asserted, to keep the assertions reading clear and concise, and not having to declare more variables and more boiler-plate assert calls.

This is possible in TUnit.

For example:

```csharp
        await Assert.That(response)
            .IsProblemDetails()
            .And
            .HasTitle("Invalid Authentication Token")
            .And
            .HasDetail("No token provided");
```

The `response` object initially passed in is a `HttpResponseMessage`, but then after we assert it's a `ProblemDetails` object, the chain has changed to that type so that we can further assert with methods specific to `ProblemDetails` instead of `HttpResponseMessage`.

This involves a few steps and specific types to work.

Firstly, you need to create a custom assert condition that inherits from `ConvertToAssertCondition<TFromType, TToType>`.

In the example above, that'd look like:

```csharp
public class HttpResponseIsProblemDetailsAssertCondition() : ConvertToAssertCondition<HttpResponseMessage, ProblemDetails>()
{
    protected override string GetExpectation()
    {
        return $"HTTP response to be in the format of a Problem Details object";
    }

    public override async ValueTask<(AssertionResult, TToType?)> ConvertValue(HttpResponseMessage value)
    {
        var content = await value.Content.ReadFromJsonAsync<ProblemDetails>();

        return 
        (
            AssertionResult.FailIf(content is null, $"response body is not Problem Details"), 
            content
        );
    }
}
```

As you can see, this returns two objects (via a Tuple) - An assertion result, so we know if it's passed or failed the conversion, and then the actual converted object.

You then need to create an extension method to register this assert condition on your data.
Instead of calling `source.RegisterAssertion(...)` like we do on standard assertions, we instead call `source.RegisterConversionAssertion(...)`.

Again, for the above example, that'd look like:

```csharp
public static class HttpResponseAssertionExtensions
{
    public static InvokableValueAssertionBuilder<ProblemDetails> IsProblemDetails(this IValueSource<HttpResponseMessage> valueSource)
    {
        return valueSource.RegisterConversionAssertion(new HttpResponseIsProblemDetailsAssertCondition());
    }
}
```

That's it!

Now any assertions built for the `ProblemDetails` type will work off of that same chain.

E.g.

```csharp
public static class ProblemDetailsAssertionExtensions
{
    public static InvokableValueAssertionBuilder<ProblemDetails> HasTitle(this IValueSource<ProblemDetails> valueSource,
        string title, [CallerArgumentExpression("title")] string? titleExpression = null)
    {
        return valueSource.RegisterAssertion(new ProblemDetailsHasTitleAssertCondition(title), [titleExpression]);
    }
    
    public static InvokableValueAssertionBuilder<ProblemDetails> HasDetail(this IValueSource<ProblemDetails> valueSource,
        string detail, [CallerArgumentExpression("detail")] string? detailExpression = null)
    {
        return valueSource.RegisterAssertion(new ProblemDetailsHasDetailAssertCondition(detail), [detailExpression]);
    }
}
```
