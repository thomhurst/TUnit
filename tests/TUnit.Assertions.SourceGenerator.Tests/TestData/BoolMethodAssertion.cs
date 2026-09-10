using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Simple bool-returning method
/// Should generate Assertion class and extension method
/// </summary>
public static partial class BoolMethodAssertionExtensions
{
    [GenerateAssertion]
    public static bool IsPositive(this int value)
    {
        return value > 0;
    }

    [GenerateAssertion]
    public static bool IsGreaterThan(this int value, int threshold)
    {
        return value > threshold;
    }
}
