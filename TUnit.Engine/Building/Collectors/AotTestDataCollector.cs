using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// Test data collector for AOT mode - collects source-generated test metadata
/// </summary>
public sealed class AotTestDataCollector : ITestDataCollector
{
    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        var allTests = new List<TestMetadata>();

        // Collect tests from all registered test sources
        foreach (var testSource in Sources.TestSources)
        {
            try
            {
                var tests = await testSource.GetTestsAsync(testSessionId);
                allTests.AddRange(tests);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to collect tests from source {testSource.GetType().Name}: {ex.Message}", ex);
            }
        }

        if (allTests.Count == 0)
        {
            // No generated tests found
            return [
            ];
        }

        return allTests;
    }
}
