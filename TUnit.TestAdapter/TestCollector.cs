using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using TUnit.Core;

namespace TUnit.TestAdapter;

public class TestCollector(AssemblyLoader assemblyLoader, TestsLoader testsLoader, ITestExecutionRecorder testExecutionRecorder)
{
    public TestCollection CollectionFromSources(IEnumerable<string> sources)
    {
        var sourcesAsList = sources.ToList();
        
        return new TestCollection(sourcesAsList, TestsFromSources(sourcesAsList));
    }

    public AssembliesAnd<TestWithTestCase> TestsFromTestCases(IEnumerable<TestCase> testCases)
    {
        var testCasesArray = testCases.ToArray();
        var allAssemblies = testCasesArray
            .Select(x => assemblyLoader.LoadByPath(x.Source))
            .OfType<Assembly>()
            .ToArray();
        
        return new AssembliesAnd<TestWithTestCase>(allAssemblies, TestWithTestCaseCore(testCasesArray, allAssemblies));
    }

    private IEnumerable<TestWithTestCase> TestWithTestCaseCore(TestCase[] testCasesArray, Assembly[] allAssemblies)
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
        if (testDetails.IsSingleTest)
        {
            return testDetails.FullyQualifiedName == testCase.FullyQualifiedName
                && testDetails.MinLineNumber == testCase.LineNumber;
        }
        
        var uniqueId = testCase.GetPropertyValue(TUnitTestProperties.UniqueId, null as string);
        
        if (uniqueId == null)
        {
            return testDetails.FullyQualifiedName == testCase.FullyQualifiedName
                   && testDetails.MinLineNumber == testCase.LineNumber
                   && testDetails.DisplayName == testCase.DisplayName;
        }
        
        return uniqueId == testDetails.UniqueId;
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
            .Select(source => Path.IsPathRooted(source) ? source : Path.Combine(Directory.GetCurrentDirectory(), source))
            .Select(assemblyLoader.LoadByPath)
            .OfType<Assembly>()
            .ToArray();
        
        var tests = allAssemblies
            .Select(x => new TypeInformation(x))
            .SelectMany(x => testsLoader.GetTests(x, allAssemblies));

        return new AssembliesAnd<TestDetails>(allAssemblies, tests);
    }
}