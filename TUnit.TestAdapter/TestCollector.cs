using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TUnit.Core;

namespace TUnit.TestAdapter;

public class TestCollector
{
    private readonly IMessageLogger? _messageLogger;

    public TestCollector(IMessageLogger? messageLogger)
    {
        _messageLogger = messageLogger;
    }
    
    public TestCollection CollectionFromSources(IEnumerable<string> sources)
    {
        var sourcesAsList = sources.ToList();
        
        return new TestCollection(sourcesAsList, TestsFromSources(sourcesAsList));
    }
    
    public IEnumerable<Test> TestsFromSources(IEnumerable<string> sources)
    {
        var assemblyLoader = new AssemblyLoader();
        var testsLoader = new TestsLoader(_messageLogger);

        var tests = sources
            .Select(assemblyLoader.LoadByPath)
            .OfType<Assembly>()
            .Select(x => new TypeInformation(x))
            .SelectMany(x => testsLoader.GetTests(x));

        return tests;
    }
}