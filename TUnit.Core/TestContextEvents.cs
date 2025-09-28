namespace TUnit.Core;

/// <summary>
/// Simplified test context events
/// </summary>
public class TestContextEvents
{
    public AsyncEvent<TestContext>? OnDispose { get; set; }
    public AsyncEvent<TestContext>? OnTestRegistered { get; set; }
    public AsyncEvent<TestContext>? OnInitialize { get; set; }
    public AsyncEvent<TestContext>? OnTestStart { get; set; }
    public AsyncEvent<TestContext>? OnTestEnd { get; set; }
    public AsyncEvent<TestContext>? OnTestSkipped { get; set; }
    public AsyncEvent<(TestContext, int RetryAttempt)>? OnTestRetry { get; set; }

    /// <summary>
    /// Internal framework event that fires after all retry attempts are complete.
    /// Used for framework resource management and reference counting.
    /// </summary>
    internal AsyncEvent<TestContext>? OnTestFinalized { get; set; }
}
