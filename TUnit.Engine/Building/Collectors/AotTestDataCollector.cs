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
                // Log but continue with other sources
                Console.WriteLine($"Warning: Failed to collect tests from source {testSource.GetType().Name}: {ex.Message}");
            }
        }

        if (allTests.Count == 0)
        {
            // No generated tests found
            return Enumerable.Empty<TestMetadata>();
        }

        // Filter out tests that the source generator cannot handle yet
        // TODO: Source generation mode currently cannot handle:
        // 1. Generic type definitions - requires runtime type resolution
        // 2. Async data source generators - requires runtime async execution
        // This creates a feature parity gap with reflection mode (~1000 tests difference)
        var filteredMetadata = allTests.Where(m =>
            !HasAsyncDataSourceGenerator(m) &&
            !IsGenericTypeDefinition(m));

        return filteredMetadata;
    }

    private static bool HasAsyncDataSourceGenerator(TestMetadata metadata)
    {
        // Check if any data sources are async
        return metadata.DataSources.Any(ds => ds is AsyncDelegateDataSource || ds is TaskDelegateDataSource);
    }

    private static bool IsGenericTypeDefinition(TestMetadata metadata)
    {
        // Generic type definitions are handled separately through inheritance
        return metadata.TestClassType.IsGenericTypeDefinition ||
               (metadata.GenericMethodInfo != null);
    }
}
