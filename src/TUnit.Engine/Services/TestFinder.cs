using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Implementation of ITestFinder that uses TestDiscoveryServiceV2's cached tests
/// </summary>
internal class TestFinder : ITestFinder
{
    private readonly TestDiscoveryService _discoveryService;
    private readonly Lock _indexLock = new();
    private IndexSnapshot? _indexes;

    private sealed record IndexSnapshot(
        int Version,
        Dictionary<Type, List<TestContext>> ByType,
        Dictionary<(Type ClassType, string TestName), List<TestContext>> ByTypeAndName);

    public TestFinder(TestDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
    }

    /// <summary>
    /// Builds index dictionaries from cached tests for O(1) lookups
    /// </summary>
    private IndexSnapshot EnsureIndexesBuilt()
    {
        lock (_indexLock)
        {
            if (_indexes is { } indexes && indexes.Version == _discoveryService.CachedTestsVersion)
            {
                return indexes;
            }

            // Capture the version before reading contexts; a concurrent update must invalidate this snapshot.
            var version = _discoveryService.CachedTestsVersion;
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
                testsByType.AddToList(classType, test);

                // Index by (type, name)
                testsByTypeAndName.AddToList((classType, testName), test);
            }

            return _indexes = new IndexSnapshot(version, testsByType, testsByTypeAndName);
        }
    }

    /// <summary>
    /// Gets all test contexts for the specified class type
    /// </summary>
    public IEnumerable<TestContext> GetTests(Type classType)
    {
        var indexes = EnsureIndexesBuilt();

        if (indexes.ByType.TryGetValue(classType, out var tests))
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
        var indexes = EnsureIndexesBuilt();

        var paramTypes = methodParameterTypes as Type[] ?? methodParameterTypes?.ToArray() ?? [];
        var classParamTypes = classParameterTypes as Type[] ?? classParameterTypes?.ToArray() ?? [];

        // Use the (type, name) index for O(1) lookup instead of O(n) scan
        var key = (classType, testName);
        if (!indexes.ByTypeAndName.TryGetValue(key, out var candidateTests))
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

        var actualArgCount = context.Metadata.TestDetails?.TestClassArguments?.Length ?? 0;
        return argCount == actualArgCount;
    }
}
