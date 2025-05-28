namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an event receiver that is invoked when a test is registered with the test framework.
/// </summary>
/// <remarks>
/// This interface allows components to respond to test registration events. It can be used to:
/// - Skip tests based on certain conditions
/// - Modify test behavior or configuration
/// - Add constraints to test execution
/// - Register additional resources or dependencies for tests
/// 
/// Implementations of this interface are typically added through attributes applied to
/// test methods, test classes, or assemblies.
/// </remarks>
public interface ITestRegisteredEventReceiver : IEventReceiver
{
    /// <summary>
    /// Called when a test is registered with the test framework.
    /// </summary>
    /// <param name="context">The context providing information about the registered test.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is invoked during the test discovery phase, before the test is executed.
    /// The <paramref name="context"/> parameter provides access to the test information and
    /// allows modifications to the test behavior.
    /// 
    /// Common operations in this method include:
    /// - Skipping tests using <see cref="TestRegisteredContext.SkipTest"/>
    /// - Setting parallel execution limits using <see cref="TestRegisteredContext.SetParallelLimiter"/>
    /// - Adding additional test metadata
    /// </remarks>
    public ValueTask OnTestRegistered(TestRegisteredContext context);
}