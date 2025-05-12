using System.Diagnostics;
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
            return CollectDependencies(test, allTests, new HashSet<TestDetails>([test.TestDetails], new TestDetailsEqualityComparer()), new HashSet<TestDetails>([test.TestDetails], new TestDetailsEqualityComparer()), cancellationToken).ToArray();
        }
        catch (Exception e)
        {
            test.TestContext.SetResult(e);
        }

        return [];
    }

    internal IEnumerable<Dependency> CollectDependencies(
        DiscoveredTest test, 
        DiscoveredTest[] allTests, 
        HashSet<TestDetails> visited, 
        HashSet<TestDetails> currentChain, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var dependsOnAttribute in test.TestDetails.Attributes.OfType<DependsOnAttribute>())
        {
            foreach (var dependency in GetDependencies(test, dependsOnAttribute, allTests))
            {
                if (!currentChain.Add(dependency.TestDetails))
                {
                    throw new DependencyConflictException(currentChain.Select(x => x).Append(dependency.TestDetails).ToArray());
                }

                if (!visited.Add(dependency.TestDetails))
                {
                    currentChain.Remove(dependency.TestDetails);
                    continue;
                }
                
                yield return new Dependency(dependency, dependsOnAttribute.ProceedOnFailure);

                foreach (var nestedDependency in CollectDependencies(dependency, allTests, visited, currentChain, cancellationToken))
                {
                    yield return nestedDependency;
                }
                
                currentChain.Remove(dependency.TestDetails);
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

        if (foundTests.Length == 0)
        {
            test.TestContext.SetResult(new TUnitException($"No tests found for DependsOn({dependsOnAttribute}) - If using Inheritance remember to use an [InheritsTest] attribute"));
        }

        return foundTests;
    }

    [DebuggerDisplay("{TestDetails.TestClass.Name}.{TestDetails.TestName}")]
    internal class TestDetailsEqualityComparer : IEqualityComparer<TestDetails>
    {
        public bool Equals(TestDetails? x, TestDetails? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            
            if (x is null)
            {
                return false;
            }
            
            if (y is null)
            {
                return false;
            }
            
            if (x.GetType() != y.GetType())
            {
                return false;
            }
            
            return x.IsSameTest(y);
        }

        public int GetHashCode(TestDetails obj)
        {
            return obj.TestName.GetHashCode();
        }
    }
}