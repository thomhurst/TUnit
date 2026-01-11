using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Extensions;

internal static class TestContextExtensions
{
    private static object[] GetInternal(TestContext testContext)
    {
        var testClassArgs = testContext.Metadata.TestDetails.TestClassArguments;
        var attributes = (List<Attribute>)testContext.Metadata.TestDetails.GetAllAttributes();
        var testMethodArgs = testContext.Metadata.TestDetails.TestMethodArguments;
        var injectedProps = testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments;

        // Pre-calculate capacity to avoid reallocations
        var capacity = 3 + testClassArgs.Length + attributes.Count + testMethodArgs.Length + injectedProps.Count;
        var result = new ValueListBuilder<object>(capacity);

        result.AppendIfNotNull(testContext.ClassConstructor);
        result.AppendIfNotNull(testContext.Events);
        foreach (var value in testClassArgs)
        {
            result.AppendIfNotNull(value);
        }
        result.AppendIfNotNull(testContext.Metadata.TestDetails.ClassInstance);
        foreach (var value in attributes)
        {
            result.AppendIfNotNull(value);
        }
        foreach (var value in testMethodArgs)
        {
            result.AppendIfNotNull(value);
        }

        if (injectedProps.Count > 0)
        {
            foreach (var prop in injectedProps)
            {
                result.AppendIfNotNull(prop.Value);
            }
        }

        var arr = result.AsSpan().ToArray();
        result.Dispose();
        return arr;
    }

    public static IEnumerable<object> GetEligibleEventObjects(this TestContext testContext)
    {
        // Return cached result if available
        if (testContext.CachedEligibleEventObjects != null)
        {
            return testContext.CachedEligibleEventObjects;
        }

        // Materialize and cache the result
        var result = GetInternal(testContext);
        testContext.CachedEligibleEventObjects = result;
        return result;
    }
}
