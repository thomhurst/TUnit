---
sidebar_position: 7
---

# Extensibility / Custom Assertions

The TUnit Assertions can be easily extended so that you can create your own assertions.

In TUnit, there are two types of things we can assert on:
- Values
- Delegates

Values is what you'd guess, some basic return value, such as a `string` or `int`.

Delegates are bits of code that haven't executed yet - Instead they are passed into the assertion builder, and the TUnit assertion library will execute it. If it throws, then there will be an `Exception` object we can check in our assertion.

So to create a custom assertion:

1. Create a class that inherits from `AssertCondition<TActual, TExpected>`
   `TActual` will the the type of object that is being asserted. For example if I started with `Assert.That("Some text")` then `TActual` would be a `string` because that's what we're asserting on.

   `TExpected` will be the data (if any) that you receive from your extension method, so you'll be responsible for passing this in. You must pass it to the base class via the base constructor: `base(expectedValue)`

2. Override the method: 
   `private protected override bool Passes(TActual? actualValue, Exception? exception)`

   If this method returns a bool, then your assertion has passed, if it hasn't, then your exception will throw.

   To access `TExpected` here, it's an accessible property called `ExpectedValue`

   The `Exception` object will be populated if your assertion is a Delegate type and the delegate threw.

   The `TActual` object will be populated if a value was passed into `Assert.That(...)`, or a delegate with a return value was executed successfully.

3. Override the `GetFailureMessage` method to return a message when the assertion fails.

4. Create the extension method!
   This is where things can start to look daunting because of the generic constraints, but this allows chaining assertions together.

   You need to create an extension off of either `IValueSource<TActual, TAnd, TOr>` or `IDelegateSource<TActual, TAnd, TOr>` - Depending on what you're planning to write an assertion for. By extending off of the relevant interface we make sure that it won't be shown where it doesn't make sense thanks to the C# typing system.

   Your return type for the extension method should be `InvokableAssertionBuilder<string, TAnd, TOr>`

   And then finally, you just new up your custom assert condition class, and then call the extension method `ChainedTo(source.AssertionBuilder, [...argumentExpression])` on it, which will add it to our assertion builder. You don't have to worry what that's doing behind the scenes, it's just building rules that can chain together. 

   The argument expression array allows you to pass in `[CallerArgumentExpression]` values so that your assertion errors show you the code executed to give clear exception messages.

Here's a fully fledged assertion in action:

```csharp
public static InvokableAssertionBuilder<string, TAnd, TOr> Contains<TAnd, TOr>(this IValueSource<string, TAnd, TOr> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<string, TAnd, TOr>
        where TOr : IOr<string, TAnd, TOr>
    {
        return new StringContainsAssertCondition<TAnd, TOr>(expected, stringComparison)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
```

```csharp
public class StringContainsAssertCondition(string expected, StringComparison stringComparison)
    : AssertCondition<string, string>(expected)
{
    private protected override bool Passes(string? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = $"{ActualExpression ?? "Actual"} string is null";
            return false;
        }
        
        if (ExpectedValue is null)
        {
            OverriddenMessage = "No expected value given";
            return false;
        }
        
        return actualValue.Contains(ExpectedValue, stringComparison);
    }

    protected internal override string GetFailureMessage() => $"""
                                              Expected "{ActualValue}" to contain "{ExpectedValue}"
                                              """;
}
```