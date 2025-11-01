using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class IntAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this int value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this int value) => value != 0;

    [GenerateAssertion]
    public static bool IsEven(this int value) => value % 2 == 0;

    [GenerateAssertion]
    public static bool IsOdd(this int value) => value % 2 != 0;
}
