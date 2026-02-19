namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver that is notified when a test is registered with the test framework.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform custom logic when a test is first registered,
/// before it is discovered or executed. This is useful for collecting test metadata,
/// modifying test registration, or tracking test counts.
/// </para>
/// <para>
/// Test registration occurs early in the test lifecycle, after the test has been
/// identified but before it goes through the discovery pipeline.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
public interface ITestRegisteredEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is registered with the test framework.
    /// </summary>
    /// <param name="context">The test registered context containing information about the registered test.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnTestRegistered(TestRegisteredContext context);
}
