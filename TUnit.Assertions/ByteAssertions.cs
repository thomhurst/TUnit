using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class ByteAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this byte value)
    {
        return value == 0;
    }

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this byte value)
    {
        return value != 0;
    }

    [GenerateAssertion]
    public static bool IsEven(this byte value)
    {
        return value % 2 == 0;
    }

    [GenerateAssertion]
    public static bool IsOdd(this byte value)
    {
        return value % 2 != 0;
    }
}
