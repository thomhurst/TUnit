using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal class DependencyCollector
{
    public void ResolveDependencies(DiscoveredTest[] discoveredTests, CancellationToken cancellationToken)
    {
        foreach (var discoveredTest in discoveredTests)
        {
            discoveredTest.Dependencies = GetDependencies(discoveredTest, discoveredTests, cancellationToken);
        }
    }

    private Dependency[] GetDependencies(DiscoveredTest test, DiscoveredTest[] allTests,
        CancellationToken cancellationToken)
    {
        try
        {
            return GetDependencies(test, [], [], allTests, cancellationToken).ToArray();
        }
        catch (Exception e)
        {
            test.TestContext.SetResult(e);
        }

        return [];
    }

    private IEnumerable<Dependency> GetDependencies(DiscoveredTest test,
        List<DiscoveredTest> currentChain, HashSet<DiscoveredTest> visited, DiscoveredTest[] allTests, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (currentChain.Any(x => x.TestDetails.IsSameTest(test.TestDetails)))
        {
            var chain = currentChain
                .SkipWhile(x => !x.TestDetails.IsSameTest(test.TestDetails))
                .Select(x => x.TestDetails)
                .Append(test.TestDetails);

            throw new DependencyConflictException(chain);
        }

        if (!visited.Add(test))
        {
            yield break;
        }

        currentChain.Add(test);
        
        foreach (var dependsOnAttribute in test.TestDetails.Attributes.OfType<DependsOnAttribute>())
        {
            var dependencies = GetDependencies(test, dependsOnAttribute, allTests);

            foreach (var dependency in dependencies)
            {
                yield return new Dependency(dependency, dependsOnAttribute.ProceedOnFailure);

                foreach (var nestedDependency in GetDependencies(dependency, [..currentChain], visited, allTests, cancellationToken))
                {
                    yield return nestedDependency;
                }
            }
        }
        
        currentChain.Remove(test);
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

        if (foundTests.Length == 0)
        {
            test.TestContext.SetResult(new TUnitException($"No tests found for DependsOn({dependsOnAttribute}) - If using Inheritance remember to use an [InheritsTest] attribute"));
        }

        return foundTests;
    }
}