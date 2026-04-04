using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions.Helpers;

internal static class InlineAssertionHelper
{
    internal static async Task<AssertionResult> ExecuteInlineAssertionAsync(
        int actualValue,
        string label,
        Func<IAssertionSource<int>, Assertion<int>?> assertion)
    {
        var source = new ValueAssertion<int>(actualValue, label);
        var resultingAssertion = assertion(source);

        if (resultingAssertion == null)
        {
            return AssertionResult.Passed;
        }

        try
        {
            await resultingAssertion.AssertAsync();
            return AssertionResult.Passed;
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed(ex.Message);
        }
    }
}
