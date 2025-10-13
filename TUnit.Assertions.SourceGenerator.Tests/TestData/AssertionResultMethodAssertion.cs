using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: AssertionResult-returning method
/// Should generate Assertion class that returns result directly
/// </summary>
public static partial class AssertionResultMethodExtensions
{
    [GenerateAssertion]
    public static AssertionResult IsEven(this int value)
    {
        if (value % 2 == 0)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"{value} is odd");
    }

    [GenerateAssertion]
    public static AssertionResult IsBetween(this int value, int min, int max)
    {
        if (value >= min && value <= max)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"{value} is not between {min} and {max}");
    }
}
