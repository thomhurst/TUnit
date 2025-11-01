using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class UlongAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this ulong value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this ulong value) => value != 0;

    [GenerateAssertion]
    public static bool IsEven(this ulong value) => value % 2 == 0;

    [GenerateAssertion]
    public static bool IsOdd(this ulong value) => value % 2 != 0;
}
