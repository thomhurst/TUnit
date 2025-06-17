using TUnit.Core;

namespace TUnit.Engine.Services;

internal class TestsCollector(string sessionId)
{
    public async Task<DiscoveryResult> DiscoverTestsAsync()
    {
        var allDefinitions = new List<ITestDefinition>();
        var allFailures = new List<DiscoveryFailure>();
        
        while (Sources.TestSources.TryDequeue(out var testSource))
        {
            var result = await testSource.DiscoverTestsAsync(sessionId);
            allDefinitions.AddRange(result.TestDefinitions);
            allFailures.AddRange(result.DiscoveryFailures);
        }
        
        return new DiscoveryResult
        {
            TestDefinitions = allDefinitions,
            DiscoveryFailures = allFailures
        };
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