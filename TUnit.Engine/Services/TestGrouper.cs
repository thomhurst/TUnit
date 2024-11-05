using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(DiscoveredTest[] testCases)
    {
        var notInParallel = new PriorityQueue<DiscoveredTest, int>();
        var keyedNotInParallel = new List<NotInParallelTestCase>();
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
                    keyedNotInParallel.Add(new NotInParallelTestCase
                    {
                        Test = discoveredTest,
                        ConstraintKeys = new ConstraintKeysCollection(notInParallelConstraint.NotInParallelConstraintKeys)
                    });
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