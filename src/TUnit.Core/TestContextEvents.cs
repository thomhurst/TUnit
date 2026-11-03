using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test context events container
/// </summary>
public class TestContextEvents : ITestEvents
{
    internal TestContextEvents Clone() => new()
    {
        OnDataRowDispose = OnDataRowDispose?.Clone(),
        OnDispose = OnDispose?.Clone(),
        OnTestRegistered = OnTestRegistered?.Clone(),
        OnInitialize = OnInitialize?.Clone(),
        OnTestStart = OnTestStart?.Clone(),
        OnTestEnd = OnTestEnd?.Clone(),
        OnTestSkipped = OnTestSkipped?.Clone(),
        OnTestRetry = OnTestRetry?.Clone()
    };

    // Notified before each tracked object is released, then with null for any untracked scopes.
    // Scope handlers match their resolved objects and dispose at most once.
    internal AsyncEvent<(TestContext Context, object? Object)>? OnDataRowDispose { get; set; }

    public AsyncEvent<TestContext>? OnDispose { get; set; }
    public AsyncEvent<TestContext>? OnTestRegistered { get; set; }
    public AsyncEvent<TestContext>? OnInitialize { get; set; }
    // Delegate subscribers run once in the late stage, after early-stage receivers.
    public AsyncEvent<TestContext>? OnTestStart { get; set; }
    public AsyncEvent<TestContext>? OnTestEnd { get; set; }
    public AsyncEvent<TestContext>? OnTestSkipped { get; set; }
    /// <summary>
    /// Gets or sets handlers invoked before a retry, after backoff and cancellation checks.
    /// The retry attempt is one-based. A handler failure aborts retry processing;
    /// an accepted cancellation is preserved as a cancelled result.
    /// </summary>
    public AsyncEvent<(TestContext TestContext, int RetryAttempt)>? OnTestRetry { get; set; }
}
