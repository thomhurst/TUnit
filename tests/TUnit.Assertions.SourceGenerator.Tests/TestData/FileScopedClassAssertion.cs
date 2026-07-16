using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: File-scoped class with inlined method bodies
/// Should generate Assertion class with inlined expressions (no method calls)
/// </summary>
file static class FileScopedBoolAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be true", InlineMethodBody = true)]
    public static bool IsTrue(this bool value) => value == true;

    [GenerateAssertion(ExpectationMessage = "to be false", InlineMethodBody = true)]
    public static bool IsFalse(this bool value) => value == false;
}

file static class FileScopedIntAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be positive", InlineMethodBody = true)]
    public static bool IsPositive(this int value) => value > 0;

    [GenerateAssertion(ExpectationMessage = "to be greater than {threshold}", InlineMethodBody = true)]
    public static bool IsGreaterThan(this int value, int threshold) => value > threshold;
}
