using TUnit.Assertions.Attributes;

namespace TUnit.Assertions;

file static partial class FloatAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be zero", InlineMethodBody = true)]
    public static bool IsZero(this float value) => value == 0.0f;

    [GenerateAssertion(ExpectationMessage = "to not be zero", InlineMethodBody = true)]
    public static bool IsNotZero(this float value) => value != 0.0f;
}
