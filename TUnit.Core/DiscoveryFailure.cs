namespace TUnit.Core;

/// <summary>
/// Represents a test that failed to be discovered properly.
/// </summary>
public sealed record DiscoveryFailure
{
    /// <summary>
    /// Test ID if it could be determined.
    /// </summary>
    public string? TestId { get; init; }
    
    /// <summary>
    /// The exception that occurred during discovery.
    /// </summary>
    public required Exception Exception { get; init; }
    
    /// <summary>
    /// Source file path where the test was supposed to be defined.
    /// </summary>
    public required string TestFilePath { get; init; }
    
    /// <summary>
    /// Line number in the source file where the test was supposed to be defined.
    /// </summary>
    public required int TestLineNumber { get; init; }
    
    /// <summary>
    /// Name of the test class, if known.
    /// </summary>
    public string? TestClassName { get; init; }
    
    /// <summary>
    /// Name of the test method, if known.
    /// </summary>
    public string? TestMethodName { get; init; }
    
    /// <summary>
    /// Human-readable reason for the discovery failure.
    /// </summary>
    public string Reason => Exception.Message;
}