using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Implementation of ITestFinder that uses TestDiscoveryServiceV2's cached tests
/// </summary>
internal class TestFinder : ITestFinder
{
    private readonly TestDiscoveryService _discoveryService;
    private Dictionary<Type, List<TestContext>>? _testsByType;
    private Dictionary<(Type ClassType, string TestName), List<TestContext>>? _testsByTypeAndName;

    public TestFinder(TestDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
    }

    /// <summary>
    /// Builds index dictionaries from cached tests for O(1) lookups
    /// </summary>
    private void EnsureIndexesBuilt()
    {
        if (_testsByType != null)
        {
            return; // Already built
        }

        var allTests = _discoveryService.GetCachedTestContexts();
        var testsByType = new Dictionary<Type, List<TestContext>>();
        var testsByTypeAndName = new Dictionary<(Type, string), List<TestContext>>();

        foreach (var test in allTests)
        {
            if (test.TestDetails?.ClassType == null)
            {
                continue;
            }

            var classType = test.Metadata.TestDetails.ClassType;
            var testName = test.Metadata.TestDetails.TestName;

            // Index by type
            if (!testsByType.TryGetValue(classType, out var testsForType))
            {
                testsForType = [];
                testsByType[classType] = testsForType;
            }
            testsForType.Add(test);

            // Index by (type, name)
            var key = (classType, testName);
            if (!testsByTypeAndName.TryGetValue(key, out var testsForKey))
            {
                testsForKey = [];
                testsByTypeAndName[key] = testsForKey;
            }
            testsForKey.Add(test);
        }

        _testsByType = testsByType;
        _testsByTypeAndName = testsByTypeAndName;
    }

    /// <summary>
    /// Gets all test contexts for the specified class type
    /// </summary>
    public IEnumerable<TestContext> GetTests(Type classType)
    {
        EnsureIndexesBuilt();

        if (_testsByType!.TryGetValue(classType, out var tests))
        {
            return tests;
        }

        return [];
    }

    /// <summary>
    /// Gets test contexts by name and parameters
    /// </summary>
    public TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type>? methodParameterTypes,
        Type classType, IEnumerable<Type>? classParameterTypes, IEnumerable<object?>? classArguments)
    {
        EnsureIndexesBuilt();

        var paramTypes = methodParameterTypes as Type[] ?? methodParameterTypes?.ToArray() ?? [];
        var classParamTypes = classParameterTypes as Type[] ?? classParameterTypes?.ToArray() ?? [];

        // Use the (type, name) index for O(1) lookup instead of O(n) scan
        var key = (classType, testName);
        if (!_testsByTypeAndName!.TryGetValue(key, out var candidateTests))
        {
            return [];
        }

        // If no parameter types are specified, return all matches
        if (paramTypes.Length == 0 && classParamTypes.Length == 0)
        {
            return candidateTests.ToArray();
        }

        // Filter by parameter types
        var results = new List<TestContext>(candidateTests.Count);
        foreach (var test in candidateTests)
        {
            var testParams = test.TestDetails!.MethodMetadata.Parameters;
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
