using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class SbyteAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this sbyte value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this sbyte value) => value != 0;

    [GenerateAssertion]
    public static bool IsEven(this sbyte value) => value % 2 == 0;

    [GenerateAssertion]
    public static bool IsOdd(this sbyte value) => value % 2 != 0;
}
