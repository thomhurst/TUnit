using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(DiscoveredTest[] testCases)
    {
        var notInParallel = new SortedList<int, DiscoveredTest>();
        var keyedNotInParallel = new SortedList<int, NotInParallelTestCase>();
        var parallel = new List<DiscoveredTest>();
        var parallelGroups = new ConcurrentDictionary<string, List<DiscoveredTest>>();
        
        foreach (var discoveredTest in testCases.GroupBy(x => x.TestDetails.ClassType).SelectMany(x => x))
        {
            if (discoveredTest.TestDetails.ParallelConstraint == null)
            {
                parallel.Add(discoveredTest);
            }
            else if (discoveredTest.TestDetails.ParallelConstraint is NotInParallelConstraint notInParallelConstraint)
            {
                if (notInParallelConstraint.NotInParallelConstraintKeys.Count == 0)
                {
                    notInParallel.Add(notInParallelConstraint.Order, discoveredTest);
                }
                else
                {
                    keyedNotInParallel.Add(notInParallelConstraint.Order, new NotInParallelTestCase
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
            
            Parallel = parallel.ToQueue(),
            
            KeyedNotInParallel = keyedNotInParallel.Values,
            
            NotInParallel = notInParallel.Values.ToQueue(),
            
            ParallelGroups = parallelGroups
        };
    }
}