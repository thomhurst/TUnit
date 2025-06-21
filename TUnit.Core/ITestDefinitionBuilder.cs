namespace TUnit.Core;

/// <summary>
/// Interface for building test definitions from test descriptors.
/// Implementations handle the conversion from compile-time or runtime metadata
/// into executable test definitions.
/// </summary>
public interface ITestDefinitionBuilder
{
    /// <summary>
    /// Builds test definitions from the given test descriptor.
    /// </summary>
    /// <param name="testDescriptor">The test descriptor containing test metadata.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of test definitions ready for execution.</returns>
    Task<IEnumerable<TestDefinition>> BuildTestDefinitionsAsync(ITestDescriptor testDescriptor, CancellationToken cancellationToken = default);
}