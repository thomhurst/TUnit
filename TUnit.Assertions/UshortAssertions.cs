using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class UshortAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this ushort value) => value == 0;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this ushort value) => value != 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsEven(this ushort value) => value % 2 == 0;

    [GenerateAssertion(InlineMethodBody = true)]
    public static bool IsOdd(this ushort value) => value % 2 != 0;
}
