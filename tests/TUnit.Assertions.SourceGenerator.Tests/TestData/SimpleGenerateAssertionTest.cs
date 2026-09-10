using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

public static partial class SimpleAssertionExtensions
{
    [GenerateAssertion]
    public static bool IsPositive(this int value)
        => value > 0;

    [GenerateAssertion]
    public static bool IsGreaterThan(this int value, int threshold)
        => value > threshold;
}
