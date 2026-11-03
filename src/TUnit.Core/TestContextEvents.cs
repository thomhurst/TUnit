using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test context events container
/// </summary>
public class TestContextEvents : ITestEvents
{
    internal TestContextEvents Clone() => new()
    {
        DataRowScopes = DataRowScopes?.Clone(),
        OnDispose = OnDispose?.Clone(),
        OnTestRegistered = OnTestRegistered?.Clone(),
        OnInitialize = OnInitialize?.Clone(),
        OnTestStart = OnTestStart?.Clone(),
        OnTestEnd = OnTestEnd?.Clone(),
        OnTestSkipped = OnTestSkipped?.Clone(),
        OnTestRetry = OnTestRetry?.Clone()
    };

    private object? _scopeLock;

    internal void AddDataRowScopes(DataRowScopeCollection scopes)
    {
        lock (LazyInitializer.EnsureInitialized(ref _scopeLock)!)
        {
            (DataRowScopes ??= new()).AddRange(scopes);
        }
    }

    internal DataRowScopeCollection? DetachDataRowScopes()
    {
        lock (LazyInitializer.EnsureInitialized(ref _scopeLock)!)
        {
            var scopes = DataRowScopes;
            DataRowScopes = null;
            return scopes;
        }
    }

    internal void AddCleanupFrom(TestContextEvents source)
    {
        if (source.DataRowScopes is { } scopes)
            AddDataRowScopes(scopes);
        if (source.OnDispose is { } onDispose)
            (OnDispose ??= new()).AddMissingFrom(onDispose);
    }

    // Clones share once-only scope owners while keeping row ownership collections isolated.
    internal DataRowScopeCollection? DataRowScopes { get; set; }

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
