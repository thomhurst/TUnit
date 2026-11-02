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
    /// <remarks>
    /// Raised once per test, after its final attempt and after the test class instance is disposed, so a
    /// handler must not use the instance. A retried test raises it once, after the last attempt, not once per
    /// attempt. It is also raised for a test that never runs: one that is skipped, skipped because a
    /// dependency failed, cancelled before it starts, or removed by circular-dependency detection.
    /// <para>
    /// Handlers that a data source subscribes through <see cref="TestBuilderContext.Events"/> while it builds a
    /// test's row (a dependency injection scope's disposal, for example) are raised with that test's handlers.
    /// </para>
    /// </remarks>
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
