using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Implementation of ITestFinder that uses TestDiscoveryServiceV2's cached tests
/// </summary>
internal class TestFinder : ITestFinder
{
    private readonly TestDiscoveryService _discoveryService;

    public TestFinder(TestDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
    }

    /// <summary>
    /// Gets all test contexts for the specified class type
    /// </summary>
    public IEnumerable<TestContext> GetTests(Type classType)
    {
        var allTests = _discoveryService.GetCachedTestContexts();
        foreach (var test in allTests)
        {
            if (test.TestDetails?.ClassType == classType)
            {
                yield return test;
            }
        }
    }

    /// <summary>
    /// Gets test contexts by name and parameters
    /// </summary>
    public TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type>? methodParameterTypes,
        Type classType, IEnumerable<Type>? classParameterTypes, IEnumerable<object?>? classArguments)
    {
        var paramTypes = methodParameterTypes as Type[] ?? methodParameterTypes?.ToArray() ?? [];
        var classParamTypes = classParameterTypes as Type[] ?? classParameterTypes?.ToArray() ?? [];

        var allTests = _discoveryService.GetCachedTestContexts();
        var results = new List<TestContext>();

        // If no parameter types are specified, match by name and class type only
        if (paramTypes.Length == 0 && classParamTypes.Length == 0)
        {
            foreach (var test in allTests)
            {
                if (test.TestName == testName && test.TestDetails?.ClassType == classType)
                {
                    results.Add(test);
                }
            }
            return results.ToArray();
        }

        // Match with parameter types
        foreach (var test in allTests)
        {
            if (test.TestName != testName || test.TestDetails?.ClassType != classType)
            {
                continue;
            }

            var testParams = test.TestDetails.MethodMetadata.Parameters;
            var testParamTypes = new Type[testParams.Length];
            for (int i = 0; i < testParams.Length; i++)
            {
                testParamTypes[i] = testParams[i].Type;
            }

            if (ParameterTypesMatch(testParamTypes, paramTypes) &&
                ClassParametersMatch(test, classParamTypes, classArguments))
            {
                results.Add(test);
            }
        }

        return results.ToArray();
    }

    private bool ParameterTypesMatch(Type[]? testParamTypes, Type[] expectedParamTypes)
    {
        if (testParamTypes == null && expectedParamTypes.Length == 0)
        {
            return true;
        }
        if (testParamTypes == null || testParamTypes.Length != expectedParamTypes.Length)
        {
            return false;
        }

        for (var i = 0; i < testParamTypes.Length; i++)
        {
            if (testParamTypes[i] != expectedParamTypes[i])
            {
                return false;
            }
        }
        return true;
    }

    private bool ClassParametersMatch(TestContext context, Type[] classParamTypes, IEnumerable<object?>? classArguments)
    {
        // For now, just check parameter count
        int argCount;
        if (classArguments == null)
        {
            argCount = 0;
        }
        else if (classArguments is ICollection<object?> collection)
        {
            argCount = collection.Count;
        }
        else
        {
            argCount = classArguments.Count();
        }

        var actualArgCount = context.TestDetails?.TestClassArguments?.Length ?? 0;
        return argCount == actualArgCount;
    }
}
