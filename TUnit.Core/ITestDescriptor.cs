namespace TUnit.Core;

/// <summary>
/// Base interface for all test descriptors.
/// Provides common properties needed for test identification and execution.
/// </summary>
public interface ITestDescriptor
{
    /// <summary>
    /// Unique identifier for the test.
    /// </summary>
    string TestId { get; }
    
    /// <summary>
    /// Display name shown in test runners.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Source file path where the test is defined.
    /// </summary>
    string TestFilePath { get; }
    
    /// <summary>
    /// Line number in the source file where the test is defined.
    /// </summary>
    int TestLineNumber { get; }
    
    /// <summary>
    /// Whether the test is async.
    /// </summary>
    bool IsAsync { get; }
    
    /// <summary>
    /// Whether the test should be skipped.
    /// </summary>
    bool IsSkipped { get; }
    
    /// <summary>
    /// Skip reason if the test is skipped.
    /// </summary>
    string? SkipReason { get; }
    
    /// <summary>
    /// Timeout for the test execution.
    /// </summary>
    TimeSpan? Timeout { get; }
    
    /// <summary>
    /// Number of times to repeat the test.
    /// </summary>
    int RepeatCount { get; }
}