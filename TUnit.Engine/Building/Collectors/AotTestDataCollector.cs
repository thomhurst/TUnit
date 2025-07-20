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
