using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Task&lt;AssertionResult&lt;T&gt;&gt;-returning method (async terminal assertion)
/// Should generate async Assertion class with GetAwaiter override that returns the result value
/// </summary>
public static partial class AsyncAssertionResultOfTMethodExtensions
{
    [GenerateAssertion(ExpectationMessage = "to contain '{needle}'")]
    public static Task<AssertionResult<string>> ContainsMatchAsync(this IEnumerable<string> strings, string needle)
    {
        var result = strings.FirstOrDefault(x => x.Contains(needle));
        if (result is not null)
        {
            return Task.FromResult(AssertionResult<string>.Passed(result));
        }

        return Task.FromResult<AssertionResult<string>>(AssertionResult.Failed($"{needle} not found"));
    }
}
