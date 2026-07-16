using System.Threading.Tasks;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Async Task&lt;AssertionResult&gt;-returning method
/// Should generate Assertion class with await and direct return
/// </summary>
public static partial class AsyncAssertionResultExtensions
{
    [GenerateAssertion]
    public static async Task<AssertionResult> IsEvenAsync(this int value)
    {
        await Task.Delay(1); // Simulate async work

        if (value % 2 == 0)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"{value} is odd");
    }

    [GenerateAssertion]
    public static async Task<AssertionResult> IsBetweenAsync(this int value, int min, int max)
    {
        await Task.Delay(1); // Simulate async work

        if (value >= min && value <= max)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"{value} is not between {min} and {max}");
    }
}
