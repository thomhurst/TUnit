using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.TestAdapter.Extensions;
using TUnit.TestAdapter.Models;

namespace TUnit.TestAdapter;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(IEnumerable<TestCase> testCases)
    {
        var allTestsOrderedByClass = testCases
            .GroupBy(x => x.GetPropertyValue(TUnitTestProperties.FullyQualifiedClassName, ""))
            .SelectMany(x => x)
            .OrderBy(x => x.GetPropertyValue(TUnitTestProperties.Order, int.MaxValue));

        var notInParallel = new Queue<TestCase>();
        var keyedNotInParallel = new List<TestCase>();
        var parallel = new Queue<TestCase>();

        foreach (var test in allTestsOrderedByClass)
        {
            var notInParallelConstraintKey = test.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKey, null as string);
            if (notInParallelConstraintKey == null)
            {
                parallel.Enqueue(test);
            }
            else if (notInParallelConstraintKey == string.Empty)
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
            Parallel = parallel,
            
            KeyedNotInParallel = keyedNotInParallel
                .GroupBy(x => x.GetPropertyValue(TUnitTestProperties.NotInParallelConstraintKey, ""))
                .ToQueue(),
            
            NotInParallel = notInParallel,
            
            LastTestOfClasses = parallel
                .Concat(keyedNotInParallel)
                .Concat(notInParallel)
                .GroupBy(x => x.GetPropertyValue(TUnitTestProperties.FullyQualifiedClassName, ""))
                .Select(x => x.Last())
                .ToList()
        };
    }
}