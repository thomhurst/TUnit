using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class IntAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this int value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this int value) => value != 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsEven(this int value) => value % 2 == 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsOdd(this int value) => value % 2 != 0;
}
