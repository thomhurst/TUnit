using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// Test data collector for AOT mode - collects source-generated test metadata
/// </summary>
public sealed class AotTestDataCollector : ITestDataCollector
{
    private static Func<IReadOnlyList<TestMetadata>>? _metadataProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Registers the metadata provider from generated code.
    /// This is called by the source-generated module initializer.
    /// </summary>
    public static void RegisterMetadataProvider(Func<IReadOnlyList<TestMetadata>> provider)
    {
        lock (_lock)
        {
            _metadataProvider = provider;
        }
    }

    public Task<IEnumerable<TestMetadata>> CollectTestsAsync()
    {
        // Get the registered metadata provider
        var provider = _metadataProvider;
        if (provider == null)
        {
            // No generated tests found
            return Task.FromResult(Enumerable.Empty<TestMetadata>());
        }

        // Get all test metadata from the generated registry
        var metadata = provider();

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
