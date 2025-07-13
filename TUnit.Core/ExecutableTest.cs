namespace TUnit.Core;

/// <summary>
/// A fully prepared test ready for execution, containing all necessary data and invokers
/// </summary>
public abstract class ExecutableTest
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
    public virtual TestMetadata Metadata { get; init; } = null!;

    /// <summary>
    /// Arguments to pass to the test method (empty for parameterless tests)
    /// </summary>
    public required object?[] Arguments { get; init; }

    /// <summary>
    /// Arguments to pass to the class constructor (empty for parameterless constructors)
    /// </summary>
    public object?[] ClassArguments { get; init; } = [];

    /// <summary>
    /// Creates the test class instance
    /// </summary>
    public abstract Task<object> CreateInstanceAsync();

    /// <summary>
    /// Invokes the test method with the given instance and cancellation token
    /// </summary>
    public abstract Task InvokeTestAsync(object instance, CancellationToken cancellationToken);

    /// <summary>
    /// Property values to inject after instance creation
    /// </summary>
    public Dictionary<string, object?> PropertyValues { get; init; } = new();

    /// <summary>
    /// Hooks to run before the test method executes
    /// </summary>
    public required Func<TestContext, CancellationToken, Task>[] BeforeTestHooks { get; init; }

    /// <summary>
    /// Hooks to run after the test method executes
    /// </summary>
    public required Func<TestContext, CancellationToken, Task>[] AfterTestHooks { get; init; }

    /// <summary>
    /// Test execution context
    /// </summary>
    public required TestContext Context
    {
        get;
        init
        {
            field = value;
            value.InternalExecutableTest = this;
        }
    }

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
