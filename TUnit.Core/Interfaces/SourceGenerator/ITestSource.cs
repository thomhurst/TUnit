namespace TUnit.Core.Interfaces.SourceGenerator;

/// <summary>
/// Provides test metadata for test discovery and execution.
/// </summary>
/// <remarks>
/// <para>
/// For optimized test discovery, implementations can also implement
/// <see cref="ITestDescriptorSource"/> to enable two-phase discovery:
/// </para>
/// <list type="number">
/// <item>Fast enumeration via <see cref="ITestDescriptorSource.EnumerateTestDescriptors"/></item>
/// <item>Lazy materialization via <see cref="GetTestsAsync"/></item>
/// </list>
/// <para>
/// If <see cref="ITestDescriptorSource"/> is not implemented, the discovery pipeline
/// falls back to <see cref="GetTestsAsync"/> for all tests.
/// </para>
/// </remarks>
public interface ITestSource
{
    /// <summary>
    /// Gets all test metadata for this test source.
    /// </summary>
    /// <param name="testSessionId">The test session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of test metadata.</returns>
    IAsyncEnumerable<TestMetadata> GetTestsAsync(string testSessionId, CancellationToken cancellationToken = default);
}
