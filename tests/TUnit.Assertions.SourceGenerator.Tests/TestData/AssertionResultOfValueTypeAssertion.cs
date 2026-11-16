using System.Globalization;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: AssertionResult&lt;T&gt;-returning methods with a value-type T (terminal assertions)
/// The result field must be T, not T? (Nullable&lt;T&gt;), so the awaiter can return it
/// </summary>
public static partial class AssertionResultOfValueTypeExtensions
{
    [GenerateAssertion(ExpectationMessage = "to parse as an integer")]
    public static AssertionResult<int> ParsesAsInt(this string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return AssertionResult<int>.Passed(number);
        }

        return AssertionResult.Failed($"{value} is not an integer");
    }

    [GenerateAssertion(ExpectationMessage = "to parse as an integer")]
    public static async Task<AssertionResult<int>> ParsesAsIntAsync(this string value)
    {
        await Task.Yield();
        return ParsesAsInt(value);
    }

    [GenerateAssertion(ExpectationMessage = "to have a length")]
    public static AssertionResult<int?> MaybeLength(this string value)
    {
        return AssertionResult<int?>.Passed(value.Length == 0 ? null : value.Length);
    }

    [GenerateAssertion(ExpectationMessage = "to be positive")]
    public static AssertionResult<T> IsPositiveValue<T>(this T value)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) > 0)
        {
            return AssertionResult<T>.Passed(value);
        }

        return AssertionResult.Failed($"{value} is not positive");
    }
}
