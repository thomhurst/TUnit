using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class TestContextExtensions
{
    private static IEnumerable<object?> GetInternal(TestContext testContext) =>
    [
        testContext.ClassConstructor,
        testContext.Events,
        ..testContext.TestDetails.TestClassArguments,
        testContext.TestDetails.ClassInstance,
        ..testContext.TestDetails.Attributes,
        ..testContext.TestDetails.TestMethodArguments,
        ..testContext.TestDetails.TestClassInjectedPropertyArguments.Select(x => x.Value),
    ];

    public static IEnumerable<object> GetEligibleEventObjects(this TestContext testContext)
    {
        return GetInternal(testContext).OfType<object>();
    }
}
