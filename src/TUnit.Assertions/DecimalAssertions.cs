using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class DecimalAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this decimal value) => value == 0m;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this decimal value) => value != 0m;
}
