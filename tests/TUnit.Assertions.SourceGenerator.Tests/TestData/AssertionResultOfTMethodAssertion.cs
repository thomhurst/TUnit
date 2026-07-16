using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: AssertionResult&lt;T&gt;-returning method (terminal assertion)
/// Should generate Assertion class with GetAwaiter override that returns the result value
/// </summary>
public static partial class AssertionResultOfTMethodExtensions
{
    [GenerateAssertion(ExpectationMessage = "to contain '{needle}'")]
    public static AssertionResult<string> ContainsMatch(this IEnumerable<string> strings, string needle)
    {
        var result = strings.FirstOrDefault(x => x.Contains(needle));
        if (result is not null)
        {
            return AssertionResult<string>.Passed(result);
        }

        return AssertionResult.Failed($"{needle} not found");
    }
}
