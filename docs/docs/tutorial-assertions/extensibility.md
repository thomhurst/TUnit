---
sidebar_position: 8
---

# Extensibility / Custom Assertions

The TUnit Assertions can be easily extended so that you can create your own assertions.

In TUnit, there are two types of things we can assert on:
- Values
- Delegates

Values is what you'd guess, some return value, such as a `string` or `int` or even a complex class.

Delegates are bits of code that haven't executed yet - Instead they are passed into the assertion builder, and the TUnit assertion library will execute it. If it throws, then there will be an `Exception` object we can check in our assertion.

So to create a custom assertion:

1. There are multiple classes you can inherit from to simplify your needs:
   1. If you want to assert a value has some expected data, then inherit from the `ExpectedValueAssertCondition<TActual, TExpected>`
   2. If you want to assert a value meets some criteria (e.g. IsNull) then inherit from `ValueAssertCondition<TActual>`
   3. If you want to assert a delegate threw or didn't throw an exception, inherit from `DelegateAssertCondition` or `ExpectedExceptionDelegateAssertCondition<TException>`
   4. If those don't fit what you need, the most basic class to inherit from is `BaseAssertCondition<TActual>`
2. For the generic types above, `TActual` will be the type of object that is being asserted. For example if I started with `Assert.That("Some text")` then `TActual` would be a `string` because that's what we're asserting on.

   `TExpected` will be the data (if any) that you receive from your extension method, so you'll be responsible for passing this in. You must pass it to the base class via the base constructor: `base(expectedValue)`

3. Override the method: 
   `protected override AssertionResult GetResult(...)`

   `AssertionResult` has static methods to represent a pass or a fail.

   You will be passed relevant objects based on what you're asserting. These may or may not be null, so the logic is up to you.

   Any `Exception` object will be populated if your assertion is a Delegate type and the delegate threw.

   Any `TActual` object will be populated if a value was passed into `Assert.That(...)`, or a delegate with a return value was executed successfully.

4. Override the `GetExpectation` method to return a message representing what would have been a success, in the format of "to [Your Expectation]".
e.g. Expected [Actual Value] *to be equal to [Expected Value]*

When you return an `AssertionResult.Fail` result, you supply a message. This is appended after the above statement with a `but {Your Message}`
e.g. Expected [Actual Value] to be equal to [Expected Value] *but it was null*

In your assertion class, that'd be set up like:

```csharp
    protected override string GetExpectation()
        => $"to be equal to {Format(expected).TruncateWithEllipsis(100)}";

   protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is not null,
                    "it was null");
        }

        ...
    }
```


1. Create the extension method!

   You need to create an extension off of either `IValueSource<TActual>` or `IDelegateSource<TActual>` - Depending on what you're planning to write an assertion for. By extending off of the relevant interface we make sure that it won't be shown where it doesn't make sense thanks to the C# typing system.

   Your return type for the extension method should be `InvokableValueAssertionBuilder<TActual>` or `InvokableDelegateAssertionBuilder<TActual>` depending on what type your assertion is.

   And then finally, you call `source.RegisterAssertion(assertCondition, [...callerExpressions])` - passing in your newed up your custom assert condition class. 
   The argument expression array allows you to pass in `[CallerArgumentExpression]` values so that your assertion errors show you the code executed to give clear exception messages.

Here's a fully fledged assertion in action:

```csharp
public static InvokableValueAssertionBuilder<string> Contains(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "", [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = "")
    {
        return valueSource.RegisterAssertion(
            assertCondition: new StringEqualsAssertCondition(expected, stringComparison),
            argumentExpressions: [doNotPopulateThisValue1, doNotPopulateThisValue2]
            );
    }
```

```csharp
public class StringEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<string, string>(expected)
{
    protected override string GetExpectation()
        => $"to be equal to {Format(expected).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(string? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is not null,
                    "it was null");
        }

        return AssertionResult
            .FailIf(
                () => !string.Equals(actualValue, expectedValue, stringComparison),
                $"found {Format(actualValue).TruncateWithEllipsis(100)}");
    }
}
```
