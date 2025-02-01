using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(IReadOnlyCollection<DiscoveredTest> testCases)
    {
        var notInParallel = new PriorityQueue<DiscoveredTest, int>();
        var keyedNotInParallel = new ConcurrentDictionary<ConstraintKeysCollection, PriorityQueue<DiscoveredTest, int>>();
        var parallel = new List<DiscoveredTest>();
        var parallelGroups = new ConcurrentDictionary<string, List<DiscoveredTest>>();
        
        foreach (var discoveredTest in testCases)
        {
            if (discoveredTest.TestDetails.ParallelConstraint == null)
            {
                parallel.Add(discoveredTest);
            }
            else if (discoveredTest.TestDetails.ParallelConstraint is NotInParallelConstraint notInParallelConstraint)
            {
                if (notInParallelConstraint.NotInParallelConstraintKeys.Count == 0)
                {
                    notInParallel.Enqueue(discoveredTest, notInParallelConstraint.Order);
                }
                else
                {
                    keyedNotInParallel.GetOrAdd(
                        new ConstraintKeysCollection(notInParallelConstraint.NotInParallelConstraintKeys),
                        _ => new PriorityQueue<DiscoveredTest, int>()
                    ).Enqueue(discoveredTest, notInParallelConstraint.Order);
                }
            }
            else if (discoveredTest.TestDetails.ParallelConstraint is ParallelGroupConstraint parallelGroupConstraint)
            {
                parallelGroups.GetOrAdd(parallelGroupConstraint.Group, _ => []).Add(discoveredTest);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        return new GroupedTests
        {
            AllValidTests = testCases,
            
            Parallel = parallel,
            
            KeyedNotInParallel = keyedNotInParallel,
            
            NotInParallel = notInParallel,
            
            ParallelGroups = parallelGroups
        };
    }
}