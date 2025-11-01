using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class ByteAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this byte value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this byte value) => value != 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsEven(this byte value) => value % 2 == 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsOdd(this byte value) => value % 2 != 0;
}
