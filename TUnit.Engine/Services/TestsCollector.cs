using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestsCollector(string sessionId)
{
    public async IAsyncEnumerable<TestMetadata> GetTestsAsync()
    {
        while (Sources.TestSources.TryDequeue(out var testSource))
        {
            var tests = await testSource.CollectTestsAsync(sessionId);
            foreach (var testMetadata in tests)
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