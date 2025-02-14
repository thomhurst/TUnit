using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestMetadataCollector(string sessionId)
{
    public ParallelQuery<TestMetadata> GetTests()
    {
        return Sources.TestSources
            .AsParallel()
            .SelectMany(x => x.CollectTests(sessionId));
    }
}