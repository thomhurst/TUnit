using TUnit.Core;
using TUnit.Engine.Models;

namespace TUnit.Engine;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(IEnumerable<TestInformation> testCases)
    {
        var allTestsOrderedByClass = testCases
            .GroupBy(x => x.ClassType)
            .SelectMany(x => x)
            .OrderByDescending(x => x.Order)
            .ToList();

        var notInParallel = new Queue<TestInformation>();
        var keyedNotInParallel = new List<NotInParallelTestCase>();
        var parallel = new Queue<TestInformation>();

        foreach (var test in allTestsOrderedByClass)
        {
            var notInParallelConstraintKey = test.NotInParallelConstraintKeys;
            
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