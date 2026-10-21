// No namespace declaration: everything below is in the global namespace.
using TUnit.Assertions.Attributes;

[AssertionFrom<string>(nameof(string.IsNullOrEmpty), CustomName = "IsBlankGlobal", ExpectationMessage = "be null or empty")]
public static partial class GlobalStringAssertions
{
}
