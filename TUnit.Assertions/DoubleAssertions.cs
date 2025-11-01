using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class DoubleAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this double value)
    {
        return value == 0.0;
    }

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this double value)
    {
        return value != 0.0;
    }
}
