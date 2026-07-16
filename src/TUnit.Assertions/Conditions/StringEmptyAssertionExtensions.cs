using System.ComponentModel;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated string empty/not-empty assertions using [GenerateAssertion] attributes.
/// </summary>
public static partial class StringEmptyAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be empty")]
    public static AssertionResult IsEmpty(this string value)
    {
        return value == string.Empty
            ? AssertionResult.Passed
            : AssertionResult.Failed($"received \"{value}\"");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be null or empty")]
    public static AssertionResult IsNotEmpty(this string value)
    {
        return !string.IsNullOrEmpty(value)
            ? AssertionResult.Passed
            : AssertionResult.Failed($"received \"{value}\"");
    }
}
