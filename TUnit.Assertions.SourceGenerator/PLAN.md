I want you to design a source generator that can generate assertions easily given based on a method that returns either a `bool` or an `AssertionResult` and an attribute on it.

The source generator should generate one file per attribute, and not collect them all up front, to help performance.

The source generator should generate two things:

The extension method, and the assertion class itself.

An example would be:

```
public static partial class StringAssertions
{
    [Assertion(
        expectation: "to be empty",
        but: "it was {value}"
    )]
    public static bool IsEmpty(string value) => value == "";
}
```

Or for more complicated assertions with different error messages we could have:

```
public static partial class StringAssertions
{
    [Assertion(
        expectation: "to be a valid email address",
        but: "it was {value}"
    )]
    public static bool IsValidEmail(string value)
    {
        if(string.IsNullOrWhitespace(value))
        {
            return AssertionResult.Fail("it was empty");
        }

        if(!value.Contains('@'))
        {
            return AssertionResult.Fail("it did not contain an '@' character");
        }

        return EmailRegex.Matches(value);
    }
}
```

The extension method should use the same name as the method, so the above two would be used like:

`await Assert.That(value).IsEmpty()` or `await Assert.That(value).IsValidEmail()`

We should generate the extensions in the same partial class and namespace as where they're defined.

Things to consider are struct types vs reference types. And generic definitions.
I'd like to be able to convert most simple assertions to this format.
