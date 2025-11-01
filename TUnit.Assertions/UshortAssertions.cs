using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class UshortAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this ushort value)
    {
        return value == 0;
    }

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this ushort value)
    {
        return value != 0;
    }

    [GenerateAssertion]
    public static bool IsEven(this ushort value)
    {
        return value % 2 == 0;
    }

    [GenerateAssertion]
    public static bool IsOdd(this ushort value)
    {
        return value % 2 != 0;
    }
}
