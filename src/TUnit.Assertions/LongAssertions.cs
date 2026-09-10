using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class LongAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this long value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this long value) => value != 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsEven(this long value) => value % 2 == 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsOdd(this long value) => value % 2 != 0;
}
