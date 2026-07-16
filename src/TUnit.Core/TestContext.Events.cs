using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    /// <summary>
    /// For internal framework use only. External code should use the Events property which returns ITestEvents.
    /// </summary>
    internal TestContextEvents InternalEvents => _testBuilderContext.Events;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnDispose => _testBuilderContext.Events.OnDispose;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestRegistered => _testBuilderContext.Events.OnTestRegistered;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnInitialize => _testBuilderContext.Events.OnInitialize;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestStart => _testBuilderContext.Events.OnTestStart;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestEnd => _testBuilderContext.Events.OnTestEnd;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestSkipped => _testBuilderContext.Events.OnTestSkipped;

    /// <inheritdoc/>
    AsyncEvent<(TestContext TestContext, int RetryAttempt)>? ITestEvents.OnTestRetry => _testBuilderContext.Events.OnTestRetry;
}
