using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class DecimalAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this decimal value)
    {
        return value == 0m;
    }

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this decimal value)
    {
        return value != 0m;
    }
}
