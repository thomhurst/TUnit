using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with default parameter values
/// Should generate extension method preserving default values
/// </summary>
public static partial class MethodWithDefaultValuesExtensions
{
    [GenerateAssertion(ExpectationMessage = "to contain message '{needle}'")]
    public static bool ContainsMessage(this string[] strings, string needle, bool exact = true)
    {
        return exact ? strings.Any(x => x == needle) : strings.Any(x => x.Contains(needle));
    }
}
