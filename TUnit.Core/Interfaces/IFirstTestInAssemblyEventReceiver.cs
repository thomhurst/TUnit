namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver interface that is triggered when the first test in an assembly is about to execute.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform assembly-level setup operations that should occur once before
/// any tests in the assembly are executed. This event receiver is useful for initializing resources
/// or configuring state that should be shared across all tests in the assembly.
/// </para>
/// <para>
/// The order of execution for assembly-level events is:
/// <list type="number">
/// <item><see cref="IFirstTestInAssemblyEventReceiver"/> - before any tests in the assembly run</item> 
/// <item>Tests execute within the assembly</item> 
/// <item><see cref="ILastTestInAssemblyEventReceiver"/> - after all tests in the assembly have completed</item> 
/// </list>
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// </remarks>
public interface IFirstTestInAssemblyEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when the first test in an assembly is about to be executed.
    /// </summary>
    /// <remarks>
    /// This event is triggered once per assembly before any test within that assembly starts execution.
    /// It can be used to perform assembly-level setup tasks such as initializing resources that should be
    /// available for all tests in the assembly.
    /// </remarks>
    /// <param name="context">The assembly hook context containing information about the assembly and its test classes.</param>
    /// <param name="testContext">The context of the first test that triggered this event.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext);
}
