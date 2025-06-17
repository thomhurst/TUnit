namespace TUnit.Core;

/// <summary>
/// Result of test discovery, containing both successful definitions and failures.
/// </summary>
public sealed record DiscoveryResult
{
    /// <summary>
    /// Successfully discovered test definitions.
    /// </summary>
    public required IReadOnlyList<ITestDefinition> TestDefinitions { get; init; }
    
    /// <summary>
    /// Tests that failed to be discovered properly.
    /// </summary>
    public required IReadOnlyList<DiscoveryFailure> DiscoveryFailures { get; init; }
    
    /// <summary>
    /// Total number of tests (successful + failed).
    /// </summary>
    public int TotalCount => TestDefinitions.Count + DiscoveryFailures.Count;
    
    /// <summary>
    /// Whether all tests were discovered successfully.
    /// </summary>
    public bool HasFailures => DiscoveryFailures.Count > 0;
}