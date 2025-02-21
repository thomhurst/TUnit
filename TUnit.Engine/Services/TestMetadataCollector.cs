using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestMetadataCollector(string sessionId)
{
    public IEnumerable<TestMetadata> GetTests()
    {
        return Sources.TestSources.SelectMany(x => x.CollectTests(sessionId));
    }
}