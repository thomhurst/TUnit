using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class ShortAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this short value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this short value) => value != 0;

    [GenerateAssertion]
    public static bool IsEven(this short value) => value % 2 == 0;

    [GenerateAssertion]
    public static bool IsOdd(this short value) => value % 2 != 0;
}
