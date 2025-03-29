using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestsCollector(string sessionId)
{
    public IEnumerable<TestMetadata> GetTests()
    {
        return Sources.TestSources.SelectMany(x => x.CollectTests(sessionId));
    }
    
    public IEnumerable<DynamicTest> GetDynamicTests()
    {
        return Sources.DynamicTestSources.SelectMany(x => x.CollectTests(sessionId));
    }
}