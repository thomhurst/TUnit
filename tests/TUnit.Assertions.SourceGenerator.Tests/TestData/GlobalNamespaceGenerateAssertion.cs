// No namespace declaration: everything below is in the global namespace.
using TUnit.Assertions.Attributes;

public static class GlobalGenerateAssertions
{
    [GenerateAssertion(ExpectationMessage = "to be positive")]
    public static bool IsPositiveGlobal(this int value) => value > 0;
}
