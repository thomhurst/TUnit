using System.Collections.Concurrent;
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
           return CollectDependencies(test, allTests, [], cancellationToken).ToArray();
        }
        catch (Exception e)
        {
            test.TestContext.SetResult(e);
        }

        return [];
    }

    private IEnumerable<Dependency> CollectDependencies(DiscoveredTest test, DiscoveredTest[] allTests, HashSet<TestDetailsEqualityWrapper> visited, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        foreach (var dependsOnAttribute in test.TestDetails.Attributes.OfType<DependsOnAttribute>())
        {
            foreach (var dependency in GetDependencies(test, dependsOnAttribute, allTests))
            {
                var testDetailsEqualityWrapper = new TestDetailsEqualityWrapper(dependency.TestDetails);
                
                if (!visited.Add(testDetailsEqualityWrapper))
                {
                    throw new DependencyConflictException([..visited.Select(x => x.TestDetails), dependency.TestDetails]);
                }
                
                yield return new Dependency(dependency, dependsOnAttribute.ProceedOnFailure);

                foreach (var nestedDependency in CollectDependencies(dependency, allTests, visited, cancellationToken))
                {
                    yield return nestedDependency;
                }
                
                visited.Remove(testDetailsEqualityWrapper);
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