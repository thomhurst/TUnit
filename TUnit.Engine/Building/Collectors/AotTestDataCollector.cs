using TUnit.Core;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// Test data collector for AOT mode - collects source-generated test metadata
/// </summary>
public sealed class AotTestDataCollector : ITestDataCollector
{
    public Task<IEnumerable<TestMetadata>> CollectTestsAsync()
    {
        var allTests = new List<TestMetadata>();
        
        // Collect tests from all registered test sources
        foreach (var testSource in Sources.TestSources)
        {
            try
            {
                var tests = testSource.GetTests();
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
            return Task.FromResult(Enumerable.Empty<TestMetadata>());
        }

        // Filter out any tests that should be handled by specialized generators
        var filteredMetadata = allTests.Where(m =>
            !HasAsyncDataSourceGenerator(m) &&
            !IsGenericTypeDefinition(m));

        return Task.FromResult(filteredMetadata);
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
