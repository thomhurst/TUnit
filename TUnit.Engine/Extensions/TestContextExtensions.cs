using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class TestContextExtensions
{
    private static object?[] GetInternal(TestContext testContext)
    {
        var testClassArgs = testContext.Metadata.TestDetails.TestClassArguments;
        var attributes = testContext.Metadata.TestDetails.GetAllAttributes();
        var testMethodArgs = testContext.Metadata.TestDetails.TestMethodArguments;
        var injectedProps = testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments;

        // Pre-calculate capacity to avoid reallocations
        var capacity = 3 + testClassArgs.Length + attributes.Count + testMethodArgs.Length + injectedProps.Count;
        var result = new List<object?>(capacity);

        result.Add(testContext.ClassConstructor);
        result.Add(testContext.Events);
        result.AddRange(testClassArgs);
        result.Add(testContext.Metadata.TestDetails.ClassInstance);
        result.AddRange(attributes);
        result.AddRange(testMethodArgs);

        // Manual loop instead of .Select() to avoid LINQ allocation
        foreach (var prop in injectedProps)
        {
            result.Add(prop.Value);
        }

        return result.ToArray();
    }

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
