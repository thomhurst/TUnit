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

    internal AsyncEvent<TestContext>? OnDataRowDispose { get; set; }

    public AsyncEvent<TestContext>? OnDispose { get; set; }
    public AsyncEvent<TestContext>? OnTestRegistered { get; set; }
    public AsyncEvent<TestContext>? OnInitialize { get; set; }
    public AsyncEvent<TestContext>? OnTestStart { get; set; }
    public AsyncEvent<TestContext>? OnTestEnd { get; set; }
    public AsyncEvent<TestContext>? OnTestSkipped { get; set; }
    public AsyncEvent<(TestContext TestContext, int RetryAttempt)>? OnTestRetry { get; set; }
}
