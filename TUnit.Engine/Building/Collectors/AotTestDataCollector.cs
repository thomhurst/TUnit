using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// Test data collector for AOT mode - collects source-generated test metadata
/// </summary>
public sealed class AotTestDataCollector : ITestDataCollector
{
    private static readonly List<TestMetadata> _allTestMetadata = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Registers test metadata from generated code.
    /// This is called by the source-generated module initializer.
    /// </summary>
    public static void RegisterTests(IEnumerable<TestMetadata> tests)
    {
        lock (_lock)
        {
            _allTestMetadata.AddRange(tests);
        }
    }
    
    /// <summary>
    /// Registers a single test metadata from generated code.
    /// This is safer as it isolates errors to individual tests.
    /// </summary>
    public static void RegisterTest(TestMetadata test)
    {
        lock (_lock)
        {
            _allTestMetadata.Add(test);
        }
    }

    public Task<IEnumerable<TestMetadata>> CollectTestsAsync()
    {
        IReadOnlyList<TestMetadata> metadata;
        lock (_lock)
        {
            // Create a copy to avoid concurrency issues
            metadata = _allTestMetadata.ToList();
        }

        if (metadata.Count == 0)
        {
            // No generated tests found
            return Task.FromResult(Enumerable.Empty<TestMetadata>());
        }

        // Filter out any tests that should be handled by specialized generators
        var filteredMetadata = metadata.Where(m =>
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
