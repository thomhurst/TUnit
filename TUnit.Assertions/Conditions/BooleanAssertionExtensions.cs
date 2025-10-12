using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for bool type using [GenerateAssertion] attributes.
/// These wrap simple boolean checks as extension methods.
/// </summary>
public static class BooleanAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be true")]
    public static bool IsTrue(this bool value) => value == true;

    [GenerateAssertion(ExpectationMessage = "to be false")]
    public static bool IsFalse(this bool value) => value == false;
}
