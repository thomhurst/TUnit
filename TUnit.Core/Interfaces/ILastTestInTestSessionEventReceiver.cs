namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver interface that is triggered when the last test in a test session has completed execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform test session-level cleanup operations that should occur once after
/// all tests in the session have executed. This event receiver is useful for cleaning up resources
/// or finalizing state that was shared across all tests in the test session.
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
public interface ILastTestInTestSessionEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when the last test in a test session has completed execution.
    /// </summary>
    /// <remarks>
    /// This event is triggered once per test session after all tests within that session have completed execution.
    /// It can be used to perform test session-level cleanup tasks such as releasing resources that were
    /// initialized for the tests in the session or generating session-level test reports.
    /// </remarks>
    /// <param name="current">The test session context containing information about the session and its test assemblies.</param>
    /// <param name="testContext">The context of the last test that triggered this event.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnLastTestInTestSession(TestSessionContext current, TestContext testContext);
}
