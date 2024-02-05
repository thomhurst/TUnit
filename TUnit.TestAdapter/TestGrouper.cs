using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.TestAdapter.Extensions;
using TUnit.TestAdapter.Models;

namespace TUnit.TestAdapter;

internal class TestGrouper(IMessageLogger messageLogger)
{
    public GroupedTests OrganiseTests(AssembliesAnd<TestWithTestCase> assembliesAndTests)
    {
        var allTestsOrderedByClass = assembliesAndTests
            .Values
            .GroupBy(x => x.Details.FullyQualifiedClassName)
            .SelectMany(x => x)
            .OrderBy(x => x.Details.Order);

        var notInParallel = new Queue<TestWithTestCase>();
        var keyedNotInParallel = new List<TestWithTestCase>();
        var parallel = new Queue<TestWithTestCase>();

        foreach (var testWithTestCase in allTestsOrderedByClass)
        {
            if (testWithTestCase.Details.NotInParallelConstraintKey == null)
            {
                parallel.Enqueue(testWithTestCase);
            }
            else if (testWithTestCase.Details.NotInParallelConstraintKey == string.Empty)
            {
                notInParallel.Enqueue(testWithTestCase);
            }
            else
            {
                keyedNotInParallel.Add(testWithTestCase);
            }
        }

        var groupedTests = new GroupedTests
        {
            Parallel = parallel,
            
            KeyedNotInParallel = keyedNotInParallel
                .GroupBy(x => x.Details.NotInParallelConstraintKey!)
                .ToQueue(),
            
            NotInParallel = notInParallel,
            
            LastTestOfClasses = parallel
                .Concat(keyedNotInParallel)
                .Concat(notInParallel)
                .GroupBy(x => x.Details.ClassType.FullName!)
                .Select(x => x.Last())
                .ToList()
        };
        
        messageLogger.SendMessage(TestMessageLevel.Informational, groupedTests.ToString());

        return groupedTests;
    }
}