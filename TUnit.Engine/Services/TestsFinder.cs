using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

internal class TestsFinder(TUnitTestDiscoverer testDiscoverer) : ITestFinder
{
    public IEnumerable<TestContext> GetTests(Type classType)
    {
        return testDiscoverer.GetCachedTests()
            .Where(x => x.TestDetails.ClassType == classType)
            .Select(x => x.TestContext);
    }

    public TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes, Type classType, IEnumerable<Type> classParameterTypes)
    {
        var testsWithoutMethodParameterTypesMatching = testDiscoverer.GetCachedTests().Where(x =>
                x.TestContext.TestDetails.TestName == testName &&
                x.TestContext.TestDetails.ClassType == classType &&
                x.TestContext.TestDetails.TestClassParameterTypes.SequenceEqual(classParameterTypes))
            .ToArray();

        if (testsWithoutMethodParameterTypesMatching.GroupBy(x => string.Join(", ", x.TestContext.TestDetails.TestMethodParameterTypes.Select(t => t.FullName)))
                .Count() > 1)
        {
            return testsWithoutMethodParameterTypesMatching.Where(x =>
                x.TestContext.TestDetails.TestMethodParameterTypes.SequenceEqual(methodParameterTypes))
                .Select(x => x.TestContext)
                .ToArray();
        }
        
        return testsWithoutMethodParameterTypesMatching.Select(x => x.TestContext).ToArray();
    }
}