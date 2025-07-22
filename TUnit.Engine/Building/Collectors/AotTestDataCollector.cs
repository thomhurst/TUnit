using System.Collections.Concurrent;
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
        // Use indexed collection to maintain order
        var resultsByIndex = new ConcurrentDictionary<int, IEnumerable<TestMetadata>>();

        // Collect tests from all registered test sources using true parallel processing
        var testSourcesList = Sources.TestSources.ToList();
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(testSourcesList.Select((source, index) => new { source, index }), parallelOptions, item =>
            {
                var testSource = item.source;
                var index = item.index;
                
                try
                {
                    // Run async method synchronously since we're already on thread pool
                    var tests = testSource.GetTestsAsync(testSessionId).GetAwaiter().GetResult();
                    resultsByIndex[index] = tests;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to collect tests from source {testSource.GetType().Name}: {ex.Message}", ex);
                }
            });
        });

        // Reassemble results in original order
        var allTests = new List<TestMetadata>();
        for (int i = 0; i < testSourcesList.Count; i++)
        {
            if (resultsByIndex.TryGetValue(i, out var tests))
            {
                allTests.AddRange(tests);
            }
        }

        // TODO: Dynamic test sources (marked with [DynamicTestBuilder]) are not yet supported in AOT mode
        // This is a known limitation. Dynamic tests require runtime code generation which is incompatible
        // with AOT compilation. Consider using compile-time data sources instead.
        // 
        // To support dynamic tests in the future, we would need to:
        // 1. Convert DynamicTest instances to proper TestMetadata
        // 2. Handle Expression<Action<T>> test methods without reflection
        // 3. Create appropriate MethodMetadata structures
        // 4. Implement PropertyInjectionData for dynamic tests
        
        if (Sources.DynamicTestSources.Any())
        {
            // Log a warning or consider throwing an exception
            var count = Sources.DynamicTestSources.Count();
            Console.WriteLine($"Warning: {count} dynamic test source(s) found but not supported in AOT mode. Use compile-time data sources instead.");
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
