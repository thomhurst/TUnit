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
        return _discoveryService.GetCachedTestContexts()
            .Where(t => t.TestDetails?.ClassType == classType);
    }

    /// <summary>
    /// Gets test contexts by name and parameters
    /// </summary>
    public TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes,
        Type classType, IEnumerable<Type> classParameterTypes, IEnumerable<object?> classArguments)
    {
        var paramTypes = methodParameterTypes?.ToArray() ?? [
        ];
        var classParamTypes = classParameterTypes?.ToArray() ?? [
        ];

        var allTests = _discoveryService.GetCachedTestContexts();

        // If no parameter types are specified, match by name and class type only
        if (paramTypes.Length == 0 && classParamTypes.Length == 0)
        {
            return allTests.Where(t =>
                t.TestName == testName &&
                t.TestDetails?.ClassType == classType).ToArray();
        }

        return allTests.Where(t =>
            t.TestName == testName &&
            t.TestDetails?.ClassType == classType &&
            ParameterTypesMatch(t.TestDetails.MethodMetadata.Parameters.Select(p => p.Type).ToArray(), paramTypes) &&
            ClassParametersMatch(t, classParamTypes, classArguments)).ToArray();
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

    private bool ClassParametersMatch(TestContext context, Type[] classParamTypes, IEnumerable<object?> classArguments)
    {
        // For now, just check parameter count
        var argCount = classArguments?.Count() ?? 0;
        var actualArgCount = context.TestDetails?.TestClassArguments?.Length ?? 0;
        return argCount == actualArgCount;
    }
}
