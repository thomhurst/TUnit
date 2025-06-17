namespace TUnit.Core;

/// <summary>
/// Mutable context for a single test execution attempt.
/// Created per test run, including each repeat attempt.
/// </summary>
public class TestExecutionContext
{
    /// <summary>
    /// The immutable test definition being executed.
    /// </summary>
    public ITestDefinition Definition { get; }
    
    /// <summary>
    /// Current repeat attempt number (1-based).
    /// </summary>
    public int CurrentRepeatAttempt { get; set; }
    
    /// <summary>
    /// When the test execution started.
    /// </summary>
    public DateTimeOffset? TestStart { get; set; }
    
    /// <summary>
    /// When the test execution ended.
    /// </summary>
    public DateTimeOffset? TestEnd { get; set; }
    
    /// <summary>
    /// Duration of the test execution.
    /// </summary>
    public TimeSpan? Duration => TestStart.HasValue && TestEnd.HasValue 
        ? TestEnd.Value - TestStart.Value 
        : null;
    
    /// <summary>
    /// Current status of the test execution.
    /// </summary>
    public TestStatus Status { get; set; } = TestStatus.NotStarted;
    
    /// <summary>
    /// Exception that occurred during test execution, if any.
    /// </summary>
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Current retry attempt for the test (for retry logic).
    /// </summary>
    public int CurrentRetryAttempt { get; set; }
    
    /// <summary>
    /// Logs collected during test execution.
    /// </summary>
    public List<string> Logs { get; } = [];
    
    /// <summary>
    /// Artifacts (screenshots, files, etc.) collected during test execution.
    /// </summary>
    public List<Artifact> Artifacts { get; } = [];
    
    /// <summary>
    /// Custom properties specific to this test execution.
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = [];
    
    /// <summary>
    /// Timings for various phases of test execution.
    /// </summary>
    public List<Timing> Timings { get; } = [];
    
    /// <summary>
    /// Argument display formatters for better test output.
    /// </summary>
    public List<ArgumentDisplayFormatter> ArgumentDisplayFormatters { get; } = [];
    
    /// <summary>
    /// Linked cancellation tokens for this test execution.
    /// </summary>
    public List<CancellationToken> LinkedCancellationTokens { get; } = [];
    
    /// <summary>
    /// Lock object for thread-safe operations.
    /// </summary>
#if NET9_0_OR_GREATER
    public readonly Lock Lock = new();
#else
    public readonly object Lock = new();
#endif
    
    /// <summary>
    /// Creates a new test execution context for a specific test definition and attempt.
    /// </summary>
    public TestExecutionContext(ITestDefinition definition, int currentRepeatAttempt)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CurrentRepeatAttempt = currentRepeatAttempt;
    }
}

/// <summary>
/// Status of a test execution.
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// Test has not started yet.
    /// </summary>
    NotStarted,
    
    /// <summary>
    /// Test is currently running.
    /// </summary>
    InProgress,
    
    /// <summary>
    /// Test completed successfully.
    /// </summary>
    Passed,
    
    /// <summary>
    /// Test failed with an error.
    /// </summary>
    Failed,
    
    /// <summary>
    /// Test was skipped.
    /// </summary>
    Skipped,
    
    /// <summary>
    /// Test execution was cancelled.
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Test execution timed out.
    /// </summary>
    Timeout
}