namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test-specific events for advanced integration scenarios.
/// Accessed via <see cref="TestContext.Events"/>.
/// </summary>
public interface ITestEvents
{
    /// <summary>
    /// Event triggered when the test context is disposed.
    /// </summary>
    AsyncEvent<TestContext>? OnDispose { get; set; }

    /// <summary>
    /// Event triggered when the test is registered in the test framework.
    /// </summary>
    AsyncEvent<TestContext>? OnTestRegistered { get; set; }

    /// <summary>
    /// Event triggered during test initialization, before test execution begins.
    /// </summary>
    AsyncEvent<TestContext>? OnInitialize { get; set; }

    /// <summary>
    /// Event triggered when the test execution starts.
    /// </summary>
    AsyncEvent<TestContext>? OnTestStart { get; set; }

    /// <summary>
    /// Event triggered when the test execution completes.
    /// </summary>
    AsyncEvent<TestContext>? OnTestEnd { get; set; }

    /// <summary>
    /// Event triggered when the test is skipped.
    /// </summary>
    AsyncEvent<TestContext>? OnTestSkipped { get; set; }

    /// <summary>
    /// Event triggered when the test is retried after a failure.
    /// </summary>
    AsyncEvent<(TestContext, int RetryAttempt)>? OnTestRetry { get; set; }
}
