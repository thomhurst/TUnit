using System;
using System.ComponentModel;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated string length range assertions using [GenerateAssertion] attributes.
/// </summary>
public static partial class StringLengthRangeAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have a minimum length of {minLength}")]
    public static AssertionResult HasMinLength(this string value, int minLength)
    {
        return value.Length >= minLength
            ? AssertionResult.Passed
            : AssertionResult.Failed($"found length {value.Length}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have a maximum length of {maxLength}")]
    public static AssertionResult HasMaxLength(this string value, int maxLength)
    {
        return value.Length <= maxLength
            ? AssertionResult.Passed
            : AssertionResult.Failed($"found length {value.Length}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have length between {minLength} and {maxLength}")]
    public static AssertionResult HasLengthBetween(this string value, int minLength, int maxLength)
    {
        if (minLength > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minLength),
                $"minLength ({minLength}) must be less than or equal to maxLength ({maxLength}).");
        }

        return value.Length >= minLength && value.Length <= maxLength
            ? AssertionResult.Passed
            : AssertionResult.Failed($"found length {value.Length}");
    }
}
