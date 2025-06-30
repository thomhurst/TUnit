using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// Test data collector for AOT mode - reads from source-generated metadata
/// </summary>
public sealed class AotTestDataCollector : ITestDataCollector
{
    private readonly ITestMetadataSource _metadataSource;

    public AotTestDataCollector(ITestMetadataSource metadataSource)
    {
        _metadataSource = metadataSource ?? throw new ArgumentNullException(nameof(metadataSource));
    }

    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync()
    {
        // In AOT mode, all test metadata is pre-generated and registered
        // We simply retrieve it from the metadata source
        var metadata = await _metadataSource.GetTestMetadata();

        // Filter out any tests that should be handled by specialized generators
        var filteredMetadata = metadata.Where(m =>
            !HasAsyncDataSourceGenerator(m) &&
            !IsGenericTypeDefinition(m));

        return filteredMetadata;
    }

    private static bool HasAsyncDataSourceGenerator(TestMetadata metadata)
    {
        // Check if any data sources are from AsyncDataSourceGenerator attributes
        // These are handled by specialized generators
        return metadata.DataSources.Any(ds => ds is DynamicTestDataSource dts &&
            dts.SourceType.Name.Contains("AsyncDataSourceGenerator"));
    }

    private static bool IsGenericTypeDefinition(TestMetadata metadata)
    {
        // Generic type definitions are handled separately through inheritance
        return metadata.TestClassType.IsGenericTypeDefinition ||
               (metadata.MethodInfo?.IsGenericMethodDefinition ?? false);
    }
}
