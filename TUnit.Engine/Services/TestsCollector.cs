using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestsCollector(string sessionId)
{
    public IEnumerable<TestMetadata> GetTests()
    {
        while (Sources.TestSources.TryDequeue(out var testSource))
        {
            foreach (var testMetadata in testSource.CollectTests(sessionId))
            {
                yield return testMetadata;
            }
        }
    }
    
    public IEnumerable<DynamicTest> GetDynamicTests()
    {
        while (Sources.DynamicTestSources.TryDequeue(out var dynamicTestSource))
        {
            foreach (var dynamicTest in dynamicTestSource.CollectDynamicTests(sessionId))
            {
                yield return dynamicTest;
            }
        }
    }
}