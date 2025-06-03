namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver interface that is triggered when the first test in a class is about to execute.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform class-level setup operations that should occur once before
/// any tests in the class are executed. This event receiver is useful for initializing resources
/// or configuring state that should be shared across all tests in the class.
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
public interface IFirstTestInClassEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when the first test in a class is about to be executed.
    /// </summary>
    /// <remarks>
    /// This event is triggered once per class before any test within that class starts execution.
    /// It can be used to perform class-level setup tasks such as initializing resources that should be
    /// available for all tests in the class.
    /// </remarks>
    /// <param name="context">The class hook context containing information about the class and its test methods.</param>
    /// <param name="testContext">The context of the first test that triggered this event.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnFirstTestInClass(ClassHookContext context, TestContext testContext);
}