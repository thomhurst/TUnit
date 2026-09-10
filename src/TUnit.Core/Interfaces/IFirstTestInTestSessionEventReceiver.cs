namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver interface that is triggered when the first test in a test session is about to execute.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform test session-level setup operations that should occur once before
/// any tests in the session are executed. This event receiver is useful for initializing resources
/// or configuring state that should be shared across all tests in the test session.
/// </para>
/// <para>
/// The order of execution for test session-level events is:
/// <list type="number">
/// <item><see cref="IFirstTestInTestSessionEventReceiver"/> - before any tests in the session run</item>
/// <item>Tests execute within the test session</item>
/// <item><see cref="ILastTestInTestSessionEventReceiver"/> - after all tests in the session have completed</item>
/// </list>
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
public interface IFirstTestInTestSessionEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when the first test in a test session is about to be executed.
    /// </summary>
    /// <remarks>
    /// This event is triggered once per test session before any test within that session starts execution.
    /// It can be used to perform test session-level setup tasks such as initializing resources that should be
    /// available for all tests in the session.
    /// </remarks>
    /// <param name="current">The current test session context containing information about the session and its tests.</param>
    /// <param name="testContext">The context of the first test that triggered this event.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnFirstTestInTestSession(TestSessionContext current, TestContext testContext);
}
