using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal class DependencyCollector
{
    private ConcurrentDictionary<TestDetailsEqualityWrapper, Dependency[]> _dependenciesCache = new();
    
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
            return _dependenciesCache.GetOrAdd(new TestDetailsEqualityWrapper(test.TestDetails), _ =>
            {
                var dependencies = GetDependencies(test, [], [], allTests, cancellationToken).ToArray();

                AddRecursively(dependencies);
                
                return dependencies;
            });
        }
        catch (Exception e)
        {
            test.TestContext.SetResult(e);
        }

        return [];
    }

    private void AddRecursively(Dependency[] dependencies)
    {
        foreach (var dependency in dependencies)
        {
            _dependenciesCache.TryAdd(new TestDetailsEqualityWrapper(dependency.TestDetails), dependency.Test.Dependencies);
            
            AddRecursively(dependency.Test.Dependencies);
        }
    }

    private IEnumerable<Dependency> GetDependencies(DiscoveredTest test,
        List<TestDetailsEqualityWrapper> currentChain, HashSet<TestDetailsEqualityWrapper> visited, DiscoveredTest[] allTests, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var testDetailsEqualityWrapper = new TestDetailsEqualityWrapper(test.TestDetails);

        if (currentChain.Any(x => new TestDetailsEqualityWrapper(x.TestDetails) == testDetailsEqualityWrapper))
        {
            var chain = currentChain
                .SkipWhile(x => !x.TestDetails.IsSameTest(test.TestDetails))
                .Select(x => x.TestDetails)
                .Append(test.TestDetails);

            throw new DependencyConflictException(chain);
        }

        if (!visited.Add(testDetailsEqualityWrapper))
        {
            yield break;
        }

        currentChain.Add(testDetailsEqualityWrapper);
        
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
        
        currentChain.Remove(testDetailsEqualityWrapper);
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