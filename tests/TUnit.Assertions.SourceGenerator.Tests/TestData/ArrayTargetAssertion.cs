using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: GenerateAssertion targeting a concrete array type (string[])
/// Should generate Assertion class and extension method using IAssertionSource&lt;string[]&gt;
/// </summary>
public static partial class ArrayTargetAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to contain message '{needle}'")]
    public static bool ContainsMessage(this string[] strings, string needle, bool exact = true)
    {
        return strings.Any(x => exact ? x == needle : x.Contains(needle));
    }
}
