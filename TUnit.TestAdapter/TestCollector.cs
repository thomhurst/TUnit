using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;
using TUnit.TestAdapter.Extensions;

namespace TUnit.TestAdapter;

public class TestCollector(IMessageLogger? messageLogger)
{
    public TestCollection CollectionFromSources(IEnumerable<string> sources)
    {
        var sourcesAsList = sources.ToList();
        
        return new TestCollection(sourcesAsList, TestsFromSources(sourcesAsList));
    }

    public IEnumerable<TestWithTestCase> TestsFromTestCases(IEnumerable<TestCase> testCases)
    {
        var assemblyLoader = new AssemblyLoader();
        var testsLoader = new TestsLoader(messageLogger);
        
        foreach (var testCase in testCases)
        {
            var source = testCase.Source;
            var assembly = assemblyLoader.LoadByPath(source);

            if (assembly is null)
            {
                continue;
            }

            var tests = testsLoader.GetTests(new TypeInformation(assembly));

            var matchingTest = tests.First(x => x.FullyQualifiedName == testCase.FullyQualifiedName
                                                && x.DisplayName == testCase.DisplayName);

            yield return new TestWithTestCase(matchingTest, testCase);
        }
    }

    public IEnumerable<Test> TestsFromSources(IEnumerable<string> sources)
    {
        var assemblyLoader = new AssemblyLoader();
        var testsLoader = new TestsLoader(messageLogger);

        var tests = sources
            .Select(source => Path.IsPathRooted(source) ? source : Path.Combine(Directory.GetCurrentDirectory(), source))
            .Select(assemblyLoader.LoadByPath)
            .OfType<Assembly>()
            .Select(x => new TypeInformation(x))
            .SelectMany(x => testsLoader.GetTests(x));

        return tests;
    }
}