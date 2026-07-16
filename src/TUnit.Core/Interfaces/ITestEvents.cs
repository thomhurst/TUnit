namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test-specific events for advanced integration scenarios.
/// Accessed via <see cref="TestContext.Events"/>.
/// </summary>
public interface ITestEvents
{
    /// <summary>
    /// Gets the event that is raised when the test context is disposed.
    /// </summary>
    AsyncEvent<TestContext>? OnDispose { get; }

    /// <summary>
    /// Gets the event that is raised when the test has been registered with the test runner.
    /// </summary>
    AsyncEvent<TestContext>? OnTestRegistered { get; }

    /// <summary>
    /// Gets the event that is raised before the test is initialized.
    /// </summary>
    AsyncEvent<TestContext>? OnInitialize { get; }

    /// <summary>
    /// Gets the event that is raised before the test method is invoked.
    /// </summary>
    AsyncEvent<TestContext>? OnTestStart { get; }

    /// <summary>
    /// Gets the event that is raised after the test method has completed.
    /// </summary>
    AsyncEvent<TestContext>? OnTestEnd { get; }

    /// <summary>
    /// Gets the event that is raised if the test was skipped.
    /// </summary>
    AsyncEvent<TestContext>? OnTestSkipped { get; }

    /// <summary>
    /// Gets the event that is raised before a test is retried.
    /// </summary>
    AsyncEvent<(TestContext TestContext, int RetryAttempt)>? OnTestRetry { get; }
}
