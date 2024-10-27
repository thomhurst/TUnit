using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(DiscoveredTest[] testCases)
    {
        var allTestsOrderedByClass = testCases
            .OrderBy(x => x.TestDetails.Order)
            .GroupBy(x => x.TestDetails.ClassType)
            .SelectMany(x => x)
            .ToList();

        var notInParallel = new Queue<DiscoveredTest>();
        var keyedNotInParallel = new List<NotInParallelTestCase>();
        var parallel = new Queue<DiscoveredTest>();

        foreach (var test in allTestsOrderedByClass)
        {
            var notInParallelConstraintKey = test.TestDetails.NotInParallelConstraintKeys;
            
            if (notInParallelConstraintKey == null)
            {
                parallel.Enqueue(test);
            }
            else if (notInParallelConstraintKey.Count == 0)
            {
                notInParallel.Enqueue(test);
            }
            else
            {
                keyedNotInParallel.Add(new NotInParallelTestCase
                {
                    Test = test,
                    ConstraintKeys = new ConstraintKeysCollection(notInParallelConstraintKey)
                });
            }
        }

        return new GroupedTests
        {
            AllValidTests = allTestsOrderedByClass,
            
            Parallel = parallel,
            
            KeyedNotInParallel = keyedNotInParallel,
            
            NotInParallel = notInParallel,
        };
    }
}