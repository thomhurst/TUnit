using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class LongAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this long value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this long value) => value != 0;

    [GenerateAssertion]
    public static bool IsEven(this long value) => value % 2 == 0;

    [GenerateAssertion]
    public static bool IsOdd(this long value) => value % 2 != 0;
}
