using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// A fully prepared test ready for execution, containing all necessary data and invokers
/// </summary>
public sealed class ExecutableTest
{
    /// <summary>
    /// Unique identifier for this test instance
    /// </summary>
    public required string TestId { get; init; }
    
    /// <summary>
    /// Display name for this test instance (includes parameter values for data-driven tests)
    /// </summary>
    public required string DisplayName { get; init; }
    
    /// <summary>
    /// The source metadata this test was created from
    /// </summary>
    public required TestMetadata Metadata { get; init; }
    
    /// <summary>
    /// Arguments to pass to the test method (empty for parameterless tests)
    /// </summary>
    public required object?[] Arguments { get; init; }
    
    /// <summary>
    /// Factory to create the test class instance
    /// </summary>
    public required Func<Task<object>> CreateInstance { get; init; }
    
    /// <summary>
    /// Invoker for the test method
    /// </summary>
    public required Func<object, Task> InvokeTest { get; init; }
    
    /// <summary>
    /// Property values to inject after instance creation
    /// </summary>
    public Dictionary<string, object?> PropertyValues { get; init; } = new();
    
    /// <summary>
    /// Lifecycle hooks for this test
    /// </summary>
    public required TestLifecycleHooks Hooks { get; init; }
    
    /// <summary>
    /// Test execution context
    /// </summary>
    public TestContext? Context { get; set; }
    
    /// <summary>
    /// Tests that must complete before this one can run
    /// </summary>
    public ExecutableTest[] Dependencies { get; set; } = [];
    
    /// <summary>
    /// Current execution state
    /// </summary>
    public TestState State { get; set; } = TestState.NotStarted;
    
    /// <summary>
    /// Test result after execution
    /// </summary>
    public TestResult? Result { get; set; }
    
    /// <summary>
    /// When the test started executing
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }
    
    /// <summary>
    /// When the test finished executing
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }
    
    /// <summary>
    /// Total execution duration
    /// </summary>
    public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue 
        ? EndTime.Value - StartTime.Value 
        : null;
}