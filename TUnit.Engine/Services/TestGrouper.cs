using TUnit.Engine.Models;

namespace TUnit.Engine.Services;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(List<DiscoveredTest> testCases)
    {
        var allTestsOrderedByClass = testCases
            .GroupBy(x => x.TestInformation.ClassType)
            .SelectMany(x => x)
            .OrderByDescending(x => x.TestInformation.Order)
            .ToList();

        var notInParallel = new Queue<DiscoveredTest>();
        var keyedNotInParallel = new List<NotInParallelTestCase>();
        var parallel = new Queue<DiscoveredTest>();

        foreach (var test in allTestsOrderedByClass)
        {
            var notInParallelConstraintKey = test.TestInformation.NotInParallelConstraintKeys;
            
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
            AllTests = allTestsOrderedByClass,
            
            Parallel = parallel,
            
            KeyedNotInParallel = keyedNotInParallel,
            
            NotInParallel = notInParallel,
        };
    }
}