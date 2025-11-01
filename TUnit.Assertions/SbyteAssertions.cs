using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class SbyteAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this sbyte value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this sbyte value) => value != 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsEven(this sbyte value) => value % 2 == 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsOdd(this sbyte value) => value % 2 != 0;
}
