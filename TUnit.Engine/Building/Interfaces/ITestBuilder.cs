using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for building executable tests from test metadata using the simplified approach
/// </summary>
internal interface ITestBuilder
{
    /// <summary>
    /// Builds an executable test from metadata and data combination
    /// </summary>
    /// <param name="metadata">The test metadata</param>
    /// <param name="testData">The test data</param>
    /// <param name="testBuilderContext"></param>
    /// <param name="isReusingDiscoveryInstance">Whether this test is reusing the discovery instance</param>
    /// <returns>An executable test ready for execution</returns>
    Task<AbstractExecutableTest> BuildTestAsync(TestMetadata metadata, TestBuilder.TestData testData, TestBuilderContext testBuilderContext, bool isReusingDiscoveryInstance = false);

    /// <summary>
    /// Builds all executable tests from a single TestMetadata using its DataCombinationGenerator delegate.
    /// This is the main method that replaces the old DataSourceExpander approach.
    /// </summary>
    /// <param name="metadata">The test metadata with DataCombinationGenerator</param>
    /// <param name="buildingContext">Context for optimizing test building (e.g., pre-filtering during execution)</param>
    /// <returns>Collection of executable tests for all data combinations</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Test building in reflection mode uses generic type resolution which requires unreferenced code")]
#endif
    Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata, TestBuildingContext buildingContext);

    /// <summary>
    /// Streaming version that yields tests as they're built without buffering
    /// </summary>
    /// <param name="metadata">The test metadata with DataCombinationGenerator</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of executable tests for all data combinations</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Test building in reflection mode uses generic type resolution which requires unreferenced code")]
#endif
    IAsyncEnumerable<AbstractExecutableTest> BuildTestsStreamingAsync(
        TestMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes event receivers after dependencies have been resolved.
    /// This populates TestContext._dependencies and fires ITestRegisteredEventReceiver
    /// and ITestDiscoveryEventReceiver events.
    /// </summary>
    /// <param name="test">The test with resolved dependencies</param>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    ValueTask InvokePostResolutionEventsAsync(AbstractExecutableTest test);

    /// <summary>
    /// Populates TestContext._dependencies from resolved test.Dependencies without invoking event receivers.
    /// This should be called for all tests before After(TestDiscovery) hooks run, so that hooks
    /// can access dependency information on any TestContext.
    /// </summary>
    /// <param name="test">The test with resolved dependencies</param>
    void PopulateDependenciesOnly(AbstractExecutableTest test);
}
