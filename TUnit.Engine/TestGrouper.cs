using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Engine.Models;

namespace TUnit.Engine;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(IEnumerable<TestCase> testCases)
    {
        var allTestsOrderedByClass = testCases
            .GroupBy(x => x.GetPropertyValue(TUnitTestProperties.AssemblyQualifiedClassName, ""))
            .SelectMany(x => x)
            .OrderByDescending(x => x.GetPropertyValue(TUnitTestProperties.Order, int.MaxValue))
            .ToList();

        var notInParallel = new Queue<TestCase>();
        var keyedNotInParallel = new List<TestCase>();
        var parallel = new Queue<TestCase>();

        foreach (var test in allTestsOrderedByClass)
        {
            var notInParallelConstraintKey = test.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKeys, null as string[]);
            
            if (notInParallelConstraintKey == null)
            {
                parallel.Enqueue(test);
            }
            else if (notInParallelConstraintKey.Length == 0)
            {
                notInParallel.Enqueue(test);
            }
            else
            {
                keyedNotInParallel.Add(test);
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