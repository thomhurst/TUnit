using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated null assertion using [GenerateAssertion] with InlineMethodBody.
/// </summary>
file static partial class NullAssertionExtension
{
    [GenerateAssertion(ExpectationMessage = "to be null", InlineMethodBody = true)]
    public static bool IsNull<TValue>(this TValue value) => value == null;
}
