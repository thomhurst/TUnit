using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal class DependencyCollector
{
    private readonly ConcurrentDictionary<TestDetailsEqualityWrapper, Dependency[]> _dependenciesCache = new();
    
    public void ResolveDependencies(DiscoveredTest[] discoveredTests, CancellationToken cancellationToken)
    {
        foreach (var discoveredTest in discoveredTests)
        {
            discoveredTest.Dependencies = GetDependencies(discoveredTest, discoveredTests, cancellationToken);
        }

        ValidateDependencyChains(discoveredTests, cancellationToken);
    }

    private void ValidateDependencyChains(DiscoveredTest[] discoveredTests, CancellationToken cancellationToken)
    {
        foreach (var discoveredTest in discoveredTests)
        {
            try
            {
                ValidateDependencyChain(discoveredTest, [], cancellationToken);
            }
            catch (Exception e)
            {
                discoveredTest.TestContext.SetResult(e);
            }
        }
    }

    private void ValidateDependencyChain(DiscoveredTest discoveredTest, HashSet<TestDetailsEqualityWrapper> visited, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var testDetailsEqualityWrapper = new TestDetailsEqualityWrapper(discoveredTest.TestDetails);
        
        if (!visited.Add(testDetailsEqualityWrapper))
        {
            throw new DependencyConflictException([..visited.Select(x => x.TestDetails), discoveredTest.TestDetails]);
        }

        foreach (var dependency in discoveredTest.Dependencies)
        {
            if (dependency.TestDetails.IsSameTest(discoveredTest.TestDetails))
            {
                throw new DependencyConflictException([..visited.Select(x => x.TestDetails), discoveredTest.TestDetails]);
            }

            ValidateDependencyChain(dependency.Test, visited, cancellationToken);
        }
        
        visited.Remove(testDetailsEqualityWrapper);
    }

    private Dependency[] GetDependencies(DiscoveredTest test, DiscoveredTest[] allTests,
        CancellationToken cancellationToken)
    {
        try
        {
            return _dependenciesCache.GetOrAdd(new TestDetailsEqualityWrapper(test.TestDetails), _ =>
                GetDependenciesEnumerable(test, allTests, cancellationToken).ToArray()
            );
        }
        catch (Exception e)
        {
            test.TestContext.SetResult(e);
        }

        return [];
    }

    private IEnumerable<Dependency> GetDependenciesEnumerable(DiscoveredTest test, DiscoveredTest[] allTests, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        foreach (var dependsOnAttribute in test.TestDetails.Attributes.OfType<DependsOnAttribute>())
        {
            foreach (var dependency in GetDependencies(test, dependsOnAttribute, allTests))
            {
                if (dependency.TestDetails.IsSameTest(test.TestDetails))
                {
                    throw new DependencyConflictException([dependency.TestDetails, test.TestDetails]);
                }
                
                yield return new Dependency(dependency, dependsOnAttribute.ProceedOnFailure);
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

    private class TestDetailsEqualityWrapper(TestDetails testDetails)
    {
        public TestDetails TestDetails
        {
            get;
        } = testDetails;
        

        protected bool Equals(TestDetailsEqualityWrapper other)
        {
            return TestDetails.IsSameTest(other.TestDetails);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }
            
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            
            if (obj.GetType() != GetType())
            {
                return false;
            }
            
            return Equals((TestDetailsEqualityWrapper)obj);
        }

        public override int GetHashCode()
        {
            return TestDetails.GetHashCode();
        }

        public static bool operator ==(TestDetailsEqualityWrapper? left, TestDetailsEqualityWrapper? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TestDetailsEqualityWrapper? left, TestDetailsEqualityWrapper? right)
        {
            return !Equals(left, right);
        }
    }
}