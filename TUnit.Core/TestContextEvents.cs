namespace TUnit.Core;

/// <summary>
/// Simplified test context events
/// </summary>
public record TestContextEvents
{
    public AsyncEvent<TestContext>? OnDispose { get; set; }
    public AsyncEvent<TestContext>? OnTestRegistered { get; set; }
    public AsyncEvent<TestContext>? OnInitialize { get; set; }
    public AsyncEvent<TestContext>? OnTestStart { get; set; }
    public AsyncEvent<TestContext>? OnTestEnd { get; set; }
    public AsyncEvent<TestContext>? OnTestSkipped { get; set; }
    public AsyncEvent<(TestContext, int RetryAttempt)>? OnTestRetry { get; set; }
}