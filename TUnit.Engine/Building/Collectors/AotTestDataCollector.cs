using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// Test data collector for AOT mode - reads from source-generated metadata
/// </summary>
public sealed class AotTestDataCollector : ITestDataCollector
{
    public AotTestDataCollector()
    {
        // No dependencies needed - we access generated metadata directly
    }

    public Task<IEnumerable<TestMetadata>> CollectTestsAsync()
    {
        // In AOT mode, all test metadata is pre-generated and registered
        // We access it directly from the generated registry
        var metadata = global::TUnit.Core.DirectTestMetadataProvider.GetAllTests();

        // Filter out any tests that should be handled by specialized generators
        var filteredMetadata = metadata.Where(m =>
            !HasAsyncDataSourceGenerator(m) &&
            !IsGenericTypeDefinition(m));

        return Task.FromResult(filteredMetadata);
    }

    private static bool HasAsyncDataSourceGenerator(TestMetadata metadata)
    {
        // Check if any data sources are from AsyncDataSourceGenerator attributes
        // These are handled by specialized generators
        // In AOT mode, async data sources are identified by their factory key pattern
        return metadata.DataSources.Any(ds => ds is DynamicTestDataSource dts &&
            dts.FactoryKey.Contains("AsyncDataSource"));
    }

    private static bool IsGenericTypeDefinition(TestMetadata metadata)
    {
        // Generic type definitions are handled separately through inheritance
        return metadata.TestClassType.IsGenericTypeDefinition ||
               (metadata.GenericMethodInfo != null);
    }
}
