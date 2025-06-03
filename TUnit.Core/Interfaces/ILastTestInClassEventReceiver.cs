namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver interface that is triggered when the last test in a class has completed execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform class-level cleanup operations that should occur once after
/// all tests in the class have executed. This event receiver is useful for cleaning up resources
/// or finalizing state that was shared across all tests in the class.
/// </para>
/// <para>
/// The order of execution for class-level events is:
/// <list type="number">
/// <item><see cref="IFirstTestInClassEventReceiver"/> - before any tests in the class run</item>
/// <item>Tests execute within the class</item>
/// <item><see cref="ILastTestInClassEventReceiver"/> - after all tests in the class have completed</item>
/// </list>
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
public interface ILastTestInClassEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when the last test in a class has completed execution.
    /// </summary>
    /// <remarks>
    /// This event is triggered once per class after all tests within that class have completed execution.
    /// It can be used to perform class-level cleanup tasks such as releasing resources that were
    /// initialized for the tests in the class or generating class-level test reports.
    /// </remarks>
    /// <param name="context">The class hook context containing information about the class and its test methods.</param>
    /// <param name="testContext">The context of the last test that triggered this event.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnLastTestInClass(ClassHookContext context, TestContext testContext);
}