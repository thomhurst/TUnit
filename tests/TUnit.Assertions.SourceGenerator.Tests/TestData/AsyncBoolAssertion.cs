using System.Threading.Tasks;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Async Task&lt;bool&gt;-returning method
/// Should generate Assertion class with await
/// </summary>
public static partial class AsyncBoolAssertionExtensions
{
    [GenerateAssertion]
    public static async Task<bool> IsPositiveAsync(this int value)
    {
        await Task.Delay(1); // Simulate async work
        return value > 0;
    }

    [GenerateAssertion]
    public static async Task<bool> IsGreaterThanAsync(this int value, int threshold)
    {
        await Task.Delay(1); // Simulate async work
        return value > threshold;
    }
}
