using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// AOT-compatible test data collector that uses source-generated test metadata.
/// Operates without reflection by leveraging pre-compiled test sources.
/// </summary>
internal sealed class AotTestDataCollector : ITestDataCollector
{
    private readonly HashSet<Type>? _filterTypes;

    public AotTestDataCollector(HashSet<Type>? filterTypes)
    {
        _filterTypes = filterTypes;
    }
    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        // Get all test sources as a list to enable indexed parallel processing
        var testSourcesList = Sources.TestSources
            .Where(kvp => _filterTypes == null || _filterTypes.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value)
            .ToList();

        if (testSourcesList.Count == 0)
        {
            return [];
        }

        // Use indexed collection to maintain order and prevent race conditions
        var resultsByIndex = new ConcurrentDictionary<int, IEnumerable<TestMetadata>>();

        // Use true parallel processing with optimal concurrency
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(testSourcesList.Select((source, index) => new { source, index }),
                parallelOptions, item =>
                {
                    var index = item.index;
                    var testSource = item.source;

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
        for (var i = 0; i < testSourcesList.Count; i++)
        {
            if (resultsByIndex.TryGetValue(i, out var tests))
            {
                allTests.AddRange(tests);
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