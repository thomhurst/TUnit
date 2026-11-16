using System.Globalization;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// A <c>[GenerateAssertion]</c> method that returns <c>AssertionResult&lt;T&gt;</c> for a value type gives that value
/// when awaited.
/// </summary>
public class GenerateAssertionValueTypeResultTests
{
    [Test]
    public async Task Value_Type_Result_Is_Returned()
    {
        int number = await Assert.That("42").ParsesAsIntForValueTypeTests();

        await Assert.That(number).IsEqualTo(42);
    }

    [Test]
    public async Task Async_Value_Type_Result_Is_Returned()
    {
        int number = await Assert.That("7").ParsesAsIntForValueTypeTestsAsync();

        await Assert.That(number).IsEqualTo(7);
    }

    [Test]
    public async Task Nullable_Value_Type_Result_Is_Returned()
    {
        int? length = await Assert.That(string.Empty).LengthOrNullForValueTypeTests();

        await Assert.That(length).IsNull();
    }

    [Test]
    public async Task Struct_Constrained_Generic_Result_Is_Returned()
    {
        long value = await Assert.That(5L).IsPositiveForValueTypeTests();

        await Assert.That(value).IsEqualTo(5L);
    }

    [Test]
    public async Task Failing_Value_Type_Assertion_Throws()
    {
        var action = async () =>
        {
            _ = await Assert.That("forty-two").ParsesAsIntForValueTypeTests();
        };

        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("forty-two is not an integer");
    }
}

/// <summary>
/// Generated assertions with value-type results, for <see cref="GenerateAssertionValueTypeResultTests"/>.
/// </summary>
public static partial class GenerateAssertionValueTypeResultExtensions
{
    [GenerateAssertion(ExpectationMessage = "to parse as an integer")]
    public static AssertionResult<int> ParsesAsIntForValueTypeTests(this string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return AssertionResult<int>.Passed(number);
        }

        return AssertionResult.Failed($"{value} is not an integer");
    }

    [GenerateAssertion(ExpectationMessage = "to parse as an integer")]
    public static async Task<AssertionResult<int>> ParsesAsIntForValueTypeTestsAsync(this string value)
    {
        await Task.Yield();
        return ParsesAsIntForValueTypeTests(value);
    }

    [GenerateAssertion(ExpectationMessage = "to have a length")]
    public static AssertionResult<int?> LengthOrNullForValueTypeTests(this string value)
    {
        return AssertionResult<int?>.Passed(value.Length == 0 ? null : value.Length);
    }

    [GenerateAssertion(ExpectationMessage = "to be positive")]
    public static AssertionResult<T> IsPositiveForValueTypeTests<T>(this T value)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) > 0)
        {
            return AssertionResult<T>.Passed(value);
        }

        return AssertionResult.Failed($"{value} is not positive");
    }
}
