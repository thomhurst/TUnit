namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a mechanism for executing lifecycle hooks within the TUnit test framework.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IHookExecutor"/> interface provides a way to customize how lifecycle hooks
/// (such as <c>[Before(Test)]</c>, <c>[After(Class)]</c>, etc.) are executed. Implementers
/// can control the execution environment, threading model, or synchronization context
/// for hook execution, similar to how <see cref="ITestExecutor"/> controls test execution.
/// </para>
/// <para>
/// This is particularly useful when hooks need to run on a specific thread (e.g., STA thread
/// for UI testing), within a specific synchronization context, or with custom error handling.
/// </para>
/// <para>
/// Hook executors can be specified using the <c>[HookExecutor&lt;T&gt;]</c> attribute at the
/// assembly, class, or method level.
/// </para>
/// </remarks>
public interface IHookExecutor
{
    /// <summary>
    /// Executes a "before test discovery" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The context for the test discovery phase.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteBeforeTestDiscoveryHook(MethodMetadata hookMethodInfo, BeforeTestDiscoveryContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes a "before test session" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The test session context.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteBeforeTestSessionHook(MethodMetadata hookMethodInfo, TestSessionContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes a "before assembly" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The assembly hook context.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteBeforeAssemblyHook(MethodMetadata hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes a "before class" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The class hook context.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteBeforeClassHook(MethodMetadata hookMethodInfo, ClassHookContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes a "before test" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The test context for the test about to execute.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteBeforeTestHook(MethodMetadata hookMethodInfo, TestContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes an "after test discovery" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The test discovery context.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAfterTestDiscoveryHook(MethodMetadata hookMethodInfo, TestDiscoveryContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes an "after test session" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The test session context.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAfterTestSessionHook(MethodMetadata hookMethodInfo, TestSessionContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes an "after assembly" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The assembly hook context.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAfterAssemblyHook(MethodMetadata hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes an "after class" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The class hook context.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAfterClassHook(MethodMetadata hookMethodInfo, ClassHookContext context, Func<ValueTask> action);

    /// <summary>
    /// Executes an "after test" hook.
    /// </summary>
    /// <param name="hookMethodInfo">Metadata about the hook method being executed.</param>
    /// <param name="context">The test context for the completed test.</param>
    /// <param name="action">The hook body to execute.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask ExecuteAfterTestHook(MethodMetadata hookMethodInfo, TestContext context, Func<ValueTask> action);
}
