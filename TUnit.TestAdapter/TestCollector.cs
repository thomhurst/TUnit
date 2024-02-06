using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using TUnit.Core;
using TUnit.TestAdapter.Models;

namespace TUnit.TestAdapter;

internal class TestCollector(AssemblyLoader assemblyLoader, TestsLoader testsLoader, ITestExecutionRecorder testExecutionRecorder)
{
    public AssembliesAnd<TestWithTestCase> TestsFromTestCases(IEnumerable<TestCase> testCases)
    {
        var testCasesArray = testCases.ToArray();
        var allAssemblies = testCasesArray
            .Select(x => assemblyLoader.LoadByPath(x.Source))
            .OfType<AssemblyWithSource>()
            .ToArray();
        
        return new AssembliesAnd<TestWithTestCase>(allAssemblies, TestWithTestCaseCore(testCasesArray, allAssemblies));
    }

    private IEnumerable<TestWithTestCase> TestWithTestCaseCore(TestCase[] testCasesArray, AssemblyWithSource[] allAssemblies)
    {
        foreach (var testCase in testCasesArray)
        {
            var source = testCase.Source;
            var assembly = assemblyLoader.LoadByPath(source);

            if (assembly is null)
            {
                MarkNotFound(testCase);
                continue;
            }

            var tests = testsLoader.GetTests(new TypeInformation(assembly), allAssemblies);

            var matchingTest = tests.FirstOrDefault(x => MatchTest(x, testCase));

            if (matchingTest is null)
            {
                MarkNotFound(testCase);
                continue;
            }

            yield return new TestWithTestCase(matchingTest, testCase);
        }
    }

    private static bool MatchTest(TestDetails testDetails, TestCase testCase)
    {
        return testDetails.UniqueId == testCase.FullyQualifiedName;
    }

    private void MarkNotFound(TestCase testCase)
    {
        var now = DateTimeOffset.Now;
        
        testExecutionRecorder.RecordResult(new TestResult(testCase)
        {
            DisplayName = testCase.DisplayName,
            Outcome = TestOutcome.NotFound,
            Duration = TimeSpan.Zero,
            StartTime = now,
            EndTime = now,
            ComputerName = Environment.MachineName
        });
    }

    public AssembliesAnd<TestDetails> TestsFromSources(IEnumerable<string> sources)
    {
        var allAssemblies = sources
            .Select(assemblyLoader.LoadByPath)
            .OfType<AssemblyWithSource>()
            .ToArray();
        
        var tests = allAssemblies
            .Select(x => new TypeInformation(x))
            .SelectMany(x => testsLoader.GetTests(x, allAssemblies));

        return new AssembliesAnd<TestDetails>(allAssemblies, tests);
    }
}