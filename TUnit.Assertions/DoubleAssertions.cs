using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class DoubleAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this double value) => value == 0.0;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this double value) => value != 0.0;
}
