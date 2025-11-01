using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

public static partial class UintAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero")]
    public static bool IsZero(this uint value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero")]
    public static bool IsNotZero(this uint value) => value != 0;

    [GenerateAssertion]
    public static bool IsEven(this uint value) => value % 2 == 0;

    [GenerateAssertion]
    public static bool IsOdd(this uint value) => value % 2 != 0;
}
