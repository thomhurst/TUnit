using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class TestCollector(CacheableAssemblyLoader assemblyLoader, TestsLoader testsLoader)
{
    public IEnumerable<TestDetails> TestsFromSources(IEnumerable<string> sources)
    {
        var allAssemblies = sources
            .Select(assemblyLoader.GetOrLoadAssembly)
            .OfType<Assembly>()
            .ToArray();
        
        return allAssemblies
            .Select(x => new TypeInformation(x))
            .SelectMany(testsLoader.GetTests);
    }
}