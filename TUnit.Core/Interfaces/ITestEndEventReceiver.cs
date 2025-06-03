namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver interface that is triggered when a test has completed execution.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to perform operations that should occur after each test has executed.
/// This event receiver is useful for post-test processing such as cleanup, logging, or modifying test results.
/// </para>
/// <para>
/// The <see cref="IEventReceiver.Order"/> property can be used to control the execution order
/// when multiple implementations of this interface exist.
/// </para>
/// <para>
/// The order of execution for test-level events is:
/// <list type="number">
/// <item><see cref="ITestRegisteredEventReceiver"/> - when a test is registered</item>
/// <item><see cref="ITestStartEventReceiver"/> - before a test runs</item>
/// <item>Test execution</item>
/// <item><see cref="ITestEndEventReceiver"/> - after a test completes</item>
/// </list>
/// </para>
/// </remarks>
public interface ITestEndEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test has completed execution.
    /// </summary>
    /// <remarks>
    /// This method is invoked after a test has finished executing, regardless of whether the test
    /// passed, failed, or was skipped. It provides access to the test context and details through
    /// the <paramref name="afterTestContext"/> parameter.
    /// 
    /// Implementations can use this method to:
    /// <list type="bullet">
    ///   <item>Perform cleanup after test execution</item>
    ///   <item>Log test results or execution details</item>
    ///   <item>Override or modify the test result using <see cref="AfterTestContext.OverrideResult"/></item>
    ///   <item>Add artifacts or data to the test context</item>
    /// </list>
    /// </remarks>
    /// <param name="afterTestContext">
    /// The context containing information about the completed test, including its results and details.
    /// This context also provides methods to override the test result if needed.
    /// </param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnTestEnd(AfterTestContext afterTestContext);
}