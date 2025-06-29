namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a mechanism for executing tests within the TUnit test framework.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ITestExecutor"/> interface is a core component of the TUnit testing framework that
/// provides a way to customize how tests are executed. Implementers of this interface can control
/// the execution environment, threading model, synchronization context, or other aspects of test execution.
/// </para>
/// <para>
/// Built-in implementations include:
/// <list type="bullet">
/// <item><description><see cref="DefaultExecutor"/> - executes the test directly on the current thread</description></item>
/// <item><description><see cref="DedicatedThreadExecutor"/> - executes the test on a dedicated thread</description></item>
/// <item><description><see cref="STAThreadExecutor"/> - executes the test on a Single-Threaded Apartment (STA) thread (Windows only)</description></item>
/// </list>
/// </para>
/// <para>
/// Test executors can be specified at the assembly, class, or method level using attributes such as 
/// <see cref="Executors.STAThreadExecutorAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom test executor implementation
/// public class CustomExecutor : GenericAbstractExecutor
/// {
///     protected override ValueTask ExecuteAsync(Func&lt;ValueTask&gt; action)
///     {
///         // Custom execution logic here
///         return action();
///     }
/// }
/// 
/// // Using a custom executor with a test
/// [Test]
/// [HookExecutor&lt;CustomExecutor&gt;]
/// public async Task TestWithCustomExecutor()
/// {
///     // Test code here
/// }
/// </code>
/// </example>
public interface ITestExecutor
{
    /// <summary>
    /// Executes a test within the provided test context.
    /// </summary>
    /// <param name="context">The test context containing information about the test being executed, including test details, 
    /// dependency information, and result tracking.</param>
    /// <param name="action">A function that represents the test body to be executed, which returns a <see cref="ValueTask"/>.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous execution of the test.
    /// When the returned task completes, the test execution has finished.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Called by the test framework to execute a test. The implementation 
    /// defines how the test is executed, such as on which thread or with what synchronization context.
    /// </para>
    /// <para>
    /// Custom test executors can provide specialized execution environments, such as running tests on the STA thread
    /// or with specific threading models. This is particularly useful for testing UI components or code that has
    /// specific threading requirements.
    /// </para>
    /// <para>
    /// The method should ensure that any exceptions thrown during test execution are properly propagated back to
    /// the caller to ensure correct test result reporting.
    /// </para>
    /// </remarks>
    ValueTask ExecuteTest(TestContext context, Func<ValueTask> action);
}
