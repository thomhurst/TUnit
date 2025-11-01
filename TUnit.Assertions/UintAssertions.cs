using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class UintAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this uint value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this uint value) => value != 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsEven(this uint value) => value % 2 == 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsOdd(this uint value) => value % 2 != 0;
}
