using System.ComponentModel;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated comparison assertions using [GenerateAssertion] attributes.
/// </summary>
public static partial class ComparisonAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be greater than {minimum}")]
    public static AssertionResult IsGreaterThan<TValue>(this TValue value, TValue minimum)
        where TValue : IComparable<TValue>
    {
        return value.CompareTo(minimum) > 0
            ? AssertionResult.Passed
            : AssertionResult.Failed($"received {value}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be greater than or equal to {minimum}")]
    public static AssertionResult IsGreaterThanOrEqualTo<TValue>(this TValue value, TValue minimum)
        where TValue : IComparable<TValue>
    {
        return value.CompareTo(minimum) >= 0
            ? AssertionResult.Passed
            : AssertionResult.Failed($"received {value}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be less than {maximum}")]
    public static AssertionResult IsLessThan<TValue>(this TValue value, TValue maximum)
        where TValue : IComparable<TValue>
    {
        return value.CompareTo(maximum) < 0
            ? AssertionResult.Passed
            : AssertionResult.Failed($"received {value}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be less than or equal to {maximum}")]
    public static AssertionResult IsLessThanOrEqualTo<TValue>(this TValue value, TValue maximum)
        where TValue : IComparable<TValue>
    {
        return value.CompareTo(maximum) <= 0
            ? AssertionResult.Passed
            : AssertionResult.Failed($"received {value}");
    }
}
