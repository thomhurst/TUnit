using System.Globalization;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions;

public static partial class DecimalAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this decimal value) => value == 0m;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this decimal value) => value != 0m;

    [GenerateAssertion(ExpectationMessage = "to be a whole number", InlineMethodBody = true)]
    public static bool IsWhole(this decimal value) => value == Math.Truncate(value);

    [GenerateAssertion(ExpectationMessage = "to not be a whole number", InlineMethodBody = true)]
    public static bool IsNotWhole(this decimal value) => value != Math.Truncate(value);

    [GenerateAssertion(ExpectationMessage = "to be positive", InlineMethodBody = true)]
    public static bool IsPositive(this decimal value) => value > 0m;

    [GenerateAssertion(ExpectationMessage = "to not be positive", InlineMethodBody = true)]
    public static bool IsNotPositive(this decimal value) => value <= 0m;

    [GenerateAssertion(ExpectationMessage = "to be negative", InlineMethodBody = true)]
    public static bool IsNegative(this decimal value) => value < 0m;

    [GenerateAssertion(ExpectationMessage = "to not be negative", InlineMethodBody = true)]
    public static bool IsNotNegative(this decimal value) => value >= 0m;

    [GenerateAssertion(ExpectationMessage = "to have scale {expectedScale}")]
    public static AssertionResult HasScale(this decimal value, int expectedScale)
    {
        var bits = decimal.GetBits(value);
        var actualScale = (bits[3] >> 16) & 0x1F;
        if (actualScale == expectedScale)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has scale {actualScale}");
    }

    [GenerateAssertion(ExpectationMessage = "to have precision {expectedPrecision}")]
    public static AssertionResult HasPrecision(this decimal value, int expectedPrecision)
    {
        // Get the string representation without decimal point and leading zeros
        var str = Math.Abs(value).ToString(CultureInfo.InvariantCulture)
            .Replace(".", string.Empty)
            .TrimStart('0');

        var actualPrecision = str.Length == 0 ? 1 : str.Length;
        if (actualPrecision == expectedPrecision)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has precision {actualPrecision}");
    }
}
