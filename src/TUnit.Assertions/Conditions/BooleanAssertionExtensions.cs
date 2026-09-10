using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for bool type using [GenerateAssertion] attributes.
/// These wrap simple boolean checks as extension methods.
/// </summary>
file static partial class BooleanAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be true", InlineMethodBody = true)]
    public static bool IsTrue(this bool value) => value == true;
    [GenerateAssertion(ExpectationMessage = "to be false", InlineMethodBody = true)]
    public static bool IsFalse(this bool value) => value == false;
    [GenerateAssertion(ExpectationMessage = "to be true", InlineMethodBody = true)]
    public static bool IsTrue(this bool? value) => value == true;
    [GenerateAssertion(ExpectationMessage = "to be false", InlineMethodBody = true)]
    public static bool IsFalse(this bool? value) => value == false;
}
