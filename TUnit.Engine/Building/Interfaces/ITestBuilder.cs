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
    /// <returns>An executable test ready for execution</returns>
    Task<ExecutableTest> BuildTestAsync(TestMetadata metadata, TestBuilder.TestData testData, TestBuilderContext testBuilderContext);

    /// <summary>
    /// Builds all executable tests from a single TestMetadata using its DataCombinationGenerator delegate.
    /// This is the main method that replaces the old DataSourceExpander approach.
    /// </summary>
    /// <param name="metadata">The test metadata with DataCombinationGenerator</param>
    /// <returns>Collection of executable tests for all data combinations</returns>
    Task<IEnumerable<ExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata);
}
