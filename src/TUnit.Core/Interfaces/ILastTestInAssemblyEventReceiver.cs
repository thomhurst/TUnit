namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver interface that is triggered when the last test in an assembly has completed execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform assembly-level cleanup operations that should occur once after
/// all tests in the assembly have executed. This event receiver is useful for cleaning up resources
/// or finalizing state that was shared across all tests in the assembly.
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
public interface ILastTestInAssemblyEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when the last test in an assembly has completed execution.
    /// </summary>
    /// <remarks>
    /// This event is triggered once per assembly after all tests within that assembly have completed execution.
    /// It can be used to perform assembly-level cleanup tasks such as releasing resources that were
    /// initialized for the tests in the assembly or generating assembly-level test reports.
    /// </remarks>
    /// <param name="context">The assembly hook context containing information about the assembly and its test classes.</param>
    /// <param name="testContext">The context of the last test that triggered this event.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnLastTestInAssembly(AssemblyHookContext context, TestContext testContext);
}
