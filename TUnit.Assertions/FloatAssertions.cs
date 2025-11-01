using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class FloatAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this float value)
    {
        return value == 0.0f;
    }

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this float value)
    {
        return value != 0.0f;
    }
}
