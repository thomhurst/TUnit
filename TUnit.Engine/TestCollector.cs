using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class TestCollector(CacheableAssemblyLoader assemblyLoader, TestsLoader testsLoader)
{
    public IEnumerable<TestDetails> TestsFromSources(IEnumerable<string> sources)
    {
        return sources
            .Select(assemblyLoader.GetOrLoadAssembly)
            .SelectMany(testsLoader.GetTests);
    }
}