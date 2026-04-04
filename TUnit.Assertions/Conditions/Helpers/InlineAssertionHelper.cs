using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions.Helpers;

internal static class InlineAssertionHelper
{
    internal static async Task<(AssertionResult Result, Assertion<T>? InnerAssertion)> ExecuteInlineAssertionAsync<T>(
        T actualValue,
        string label,
        Func<IAssertionSource<T>, Assertion<T>?> assertion)
    {
        var source = new ValueAssertion<T>(actualValue, label);
        var resultingAssertion = assertion(source);

        if (resultingAssertion == null)
        {
            return (AssertionResult.Passed, null);
        }

        try
        {
            await resultingAssertion.AssertAsync();
            return (AssertionResult.Passed, resultingAssertion);
        }
        catch (Exception ex)
        {
            return (AssertionResult.Failed(ex.Message), resultingAssertion);
        }
    }
}
