using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class TestContextExtensions
{
    public static IEnumerable<object> GetEligibleEventObjects(this TestContext testContext)
    {
        return GetInternal(testContext).OfType<object>();
    }

    private static IEnumerable<object?> GetInternal(TestContext testContext) =>
    [
        testContext.Events,
        ..testContext.TestDetails.Attributes,
        ..testContext.TestDetails.TestMethodArguments,
        ..testContext.TestDetails.TestClassArguments,
        ..testContext.TestDetails.TestClassInjectedPropertyArguments.Select(x => x.Value),
    ];
}
