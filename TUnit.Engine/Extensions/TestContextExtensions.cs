using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class TestContextExtensions
{
    private static IEnumerable<object?> GetInternal(TestContext testContext) =>
    [
        testContext.ClassConstructor,
        testContext.Events,
        ..testContext.Metadata.TestDetails.TestClassArguments,
        testContext.Metadata.TestDetails.ClassInstance,
        ..testContext.Metadata.TestDetails.GetAllAttributes(),
        ..testContext.Metadata.TestDetails.TestMethodArguments,
        ..testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments.Select(x => x.Value),
    ];

    public static IEnumerable<object> GetEligibleEventObjects(this TestContext testContext)
    {
        // Return cached result if available
        if (testContext.CachedEligibleEventObjects != null)
        {
            return testContext.CachedEligibleEventObjects;
        }

        // Materialize and cache the result
        var result = GetInternal(testContext).OfType<object>().ToArray();
        testContext.CachedEligibleEventObjects = result;
        return result;
    }
}
