namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a unified plugin interface for extending TUnit's test lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to create a self-contained plugin that can hook into multiple
/// points of the TUnit test lifecycle. On .NET 8.0+, this interface provides default (no-op)
/// implementations for all lifecycle methods, so plugins only need to override the methods
/// they care about.
/// </para>
/// <para>
/// Plugins are activated by applying them as attributes to test methods, classes, or assemblies
/// (when the plugin inherits from <see cref="TUnitAttribute"/>), or by inheriting from a test
/// base class that implements this interface.
/// </para>
/// <para>
/// The lifecycle methods are called in the following order:
/// <list type="number">
/// <item><see cref="ITestRegisteredEventReceiver.OnTestRegistered"/> - when a test is registered with the framework</item>
/// <item><see cref="ITestDiscoveryEventReceiver.OnTestDiscovered"/> - when a test is discovered (allows configuration changes)</item>
/// <item><see cref="ITestStartEventReceiver.OnTestStart"/> - immediately before a test executes</item>
/// <item>Test method runs</item>
/// <item><see cref="ITestEndEventReceiver.OnTestEnd"/> - immediately after a test completes (pass or fail)</item>
/// <item><see cref="ITestSkippedEventReceiver.OnTestSkipped"/> - if a test was skipped instead of executed</item>
/// <item><see cref="ITestRetryEventReceiver.OnTestRetry"/> - before a failed test is retried</item>
/// </list>
/// </para>
/// <para>
/// For session, assembly, and class-level events, see <see cref="IFirstTestInTestSessionEventReceiver"/>,
/// <see cref="ILastTestInTestSessionEventReceiver"/>, <see cref="IFirstTestInAssemblyEventReceiver"/>,
/// <see cref="ILastTestInAssemblyEventReceiver"/>, <see cref="IFirstTestInClassEventReceiver"/>,
/// and <see cref="ILastTestInClassEventReceiver"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A plugin attribute that logs test lifecycle events
/// [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
/// public class TestLoggingPluginAttribute : TUnitAttribute, ITUnitPlugin
/// {
///     public ValueTask OnTestStart(TestContext context)
///     {
///         Console.WriteLine($"Starting: {context.TestDetails.TestName}");
///         return ValueTask.CompletedTask;
///     }
///
///     public ValueTask OnTestEnd(TestContext context)
///     {
///         Console.WriteLine($"Finished: {context.TestDetails.TestName} - {context.Result?.Status}");
///         return ValueTask.CompletedTask;
///     }
///
///     // On .NET 8.0+, other methods have default no-op implementations.
///     // On older frameworks, all methods must be explicitly implemented.
/// }
/// </code>
/// </example>
public interface ITUnitPlugin :
    ITestRegisteredEventReceiver,
    ITestDiscoveryEventReceiver,
    ITestStartEventReceiver,
    ITestEndEventReceiver,
    ITestSkippedEventReceiver,
    ITestRetryEventReceiver
{
#if NET
    /// <inheritdoc />
    ValueTask ITestRegisteredEventReceiver.OnTestRegistered(TestRegisteredContext context) => default;

    /// <inheritdoc />
    ValueTask ITestDiscoveryEventReceiver.OnTestDiscovered(DiscoveredTestContext context) => default;

    /// <inheritdoc />
    ValueTask ITestStartEventReceiver.OnTestStart(TestContext context) => default;

    /// <inheritdoc />
    ValueTask ITestEndEventReceiver.OnTestEnd(TestContext context) => default;

    /// <inheritdoc />
    ValueTask ITestSkippedEventReceiver.OnTestSkipped(TestContext context) => default;

    /// <inheritdoc />
    ValueTask ITestRetryEventReceiver.OnTestRetry(TestContext context, int retryAttempt) => default;
#endif
}
