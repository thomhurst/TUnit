using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Engine.Extensions;
using TUnit.Engine.Models;
using TUnit.Engine.Models.Properties;

namespace TUnit.Engine;

internal class TestGrouper
{
    public GroupedTests OrganiseTests(IEnumerable<TestNode> testCases)
    {
        var allTestsOrderedByClass = testCases
            .GroupBy(x => x.GetRequiredProperty<ClassInformationProperty>().AssemblyQualifiedName)
            .SelectMany(x => x)
            .OrderByDescending(x => x.GetRequiredProperty<OrderProperty>().Order)
            .ToList();

        var notInParallel = new Queue<TestNode>();
        var keyedNotInParallel = new List<NotInParallelTestCase>();
        var parallel = new Queue<TestNode>();

        foreach (var test in allTestsOrderedByClass)
        {
            var notInParallelConstraintKey = test.GetRequiredProperty<NotInParallelConstraintKeysProperty>().ConstraintKeys;
            
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
                    TestNode = test,
                    ConstraintKeys = notInParallelConstraintKey
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