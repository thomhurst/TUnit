using TUnit.Core;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Services;

internal class DependencyCollector
{
    public void ResolveDependencies(DiscoveredTest[] discoveredTests)
    {
        foreach (var discoveredTest in discoveredTests)
        {
            discoveredTest.Dependencies = GetDependencies(discoveredTest, discoveredTests);
        }
    }

    private Dependency[] GetDependencies(DiscoveredTest test, DiscoveredTest[] allTests)
    {
        return GetDependencies(test, test, [test], allTests).ToArray();
    }

    private IEnumerable<Dependency> GetDependencies(DiscoveredTest original, DiscoveredTest test,
        List<DiscoveredTest> currentChain, DiscoveredTest[] allTests)
    {
        if (test.Dependencies is { Length: > 0 })
        {
            foreach (var testDependency in test.Dependencies)
            {
                yield return testDependency;
            }
            
            yield break;
        }
        
        foreach (var dependsOnAttribute in test.TestDetails.Attributes.OfType<DependsOnAttribute>())
        {
            var dependencies = GetDependencies(test, dependsOnAttribute, allTests);

            foreach (var dependency in dependencies)
            {
                currentChain.Add(dependency);

                if (dependency.TestDetails.IsSameTest(original.TestDetails))
                {
                    throw new DependencyConflictException(currentChain.Select(x => x.TestDetails));
                }

                yield return new Dependency(dependency, dependsOnAttribute.ProceedOnFailure);

                foreach (var nestedDependency in GetDependencies(original, dependency, currentChain, allTests))
                {
                    yield return nestedDependency;
                }
            }
        }
    }

    private DiscoveredTest[] GetDependencies(DiscoveredTest test, DependsOnAttribute dependsOnAttribute, DiscoveredTest[] allTests)
    {
        var testsForClass = allTests.Where(x => x.TestDetails.TestClass.Type == (dependsOnAttribute.TestClass ?? test.TestDetails.TestClass.Type));

        if (dependsOnAttribute.TestClass == null)
        {
            testsForClass = testsForClass
                .Where(x => x.TestDetails.TestClassArguments.SequenceEqual(test.TestDetails.TestClassArguments));
        }

        if (dependsOnAttribute.TestName != null)
        {
            testsForClass = testsForClass.Where(x => x.TestDetails.TestName == dependsOnAttribute.TestName);
        }

        if (dependsOnAttribute.ParameterTypes != null)
        {
            testsForClass = testsForClass.Where(x =>
                x.TestDetails.TestMethodParameterTypes.SequenceEqual(dependsOnAttribute.ParameterTypes));
        }
        
        var foundTests = testsForClass.ToArray();

        if (!foundTests.Any())
        {
            throw new TUnitException($"No tests found for DependsOn({dependsOnAttribute}) - If using Inheritance remember to use an [InheritsTest] attribute");
        }

        return foundTests;
    }
}