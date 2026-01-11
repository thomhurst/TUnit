using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class TestContextExtensions
{
    public static IEnumerable<object> GetEligibleEventObjects(this TestContext testContext)
    {
        // Return cached result if available
        if (testContext.CachedEligibleEventObjects != null)
        {
            return testContext.CachedEligibleEventObjects;
        }

        // Build result directly with single allocation
        var result = BuildEligibleEventObjects(testContext);
        testContext.CachedEligibleEventObjects = result;
        return result;
    }

    private static object[] BuildEligibleEventObjects(TestContext testContext)
    {
        var details = testContext.Metadata.TestDetails;
        var testClassArgs = details.TestClassArguments;
        var attributes = details.GetAllAttributes();
        var testMethodArgs = details.TestMethodArguments;
        var injectedProps = details.TestClassInjectedPropertyArguments;

        // Count non-null items first to allocate exact size
        var count = CountNonNull(testContext.ClassConstructor)
                  + CountNonNull(testContext.Events)
                  + CountNonNullInArray(testClassArgs)
                  + CountNonNull(details.ClassInstance)
                  + attributes.Count  // Attributes are never null
                  + CountNonNullInArray(testMethodArgs)
                  + CountNonNullValues(injectedProps);

        if (count == 0)
        {
            return [];
        }

        // Single allocation with exact size
        var result = new object[count];
        var index = 0;

        // Add items, skipping nulls
        if (testContext.ClassConstructor is { } constructor)
        {
            result[index++] = constructor;
        }

        if (testContext.Events is { } events)
        {
            result[index++] = events;
        }

        foreach (var arg in testClassArgs)
        {
            if (arg is { } nonNullArg)
            {
                result[index++] = nonNullArg;
            }
        }

        if (details.ClassInstance is { } classInstance)
        {
            result[index++] = classInstance;
        }

        foreach (var attr in attributes)
        {
            result[index++] = attr;
        }

        foreach (var arg in testMethodArgs)
        {
            if (arg is { } nonNullArg)
            {
                result[index++] = nonNullArg;
            }
        }

        foreach (var prop in injectedProps)
        {
            if (prop.Value is { } value)
            {
                result[index++] = value;
            }
        }

        return result;
    }

    private static int CountNonNull(object? obj) => obj != null ? 1 : 0;

    private static int CountNonNullInArray(object?[] array)
    {
        var count = 0;
        foreach (var item in array)
        {
            if (item != null)
            {
                count++;
            }
        }
        return count;
    }

    private static int CountNonNullValues(IDictionary<string, object?> props)
    {
        var count = 0;
        foreach (var prop in props)
        {
            if (prop.Value != null)
            {
                count++;
            }
        }
        return count;
    }
}
