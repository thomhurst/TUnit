using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    /// <summary>
    /// For internal framework use only. External code should use the Events property which returns ITestEvents.
    /// </summary>
    internal TestContextEvents InternalEvents => _testBuilderContext.Events;

    /// <summary>
    /// The events container if it has been created, otherwise <c>null</c> (no subscribers).
    /// </summary>
    internal TestContextEvents? InternalEventsIfCreated => _testBuilderContext.EventsIfCreated;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnDispose => _testBuilderContext.EventsIfCreated?.OnDispose;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestRegistered => _testBuilderContext.EventsIfCreated?.OnTestRegistered;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnInitialize => _testBuilderContext.EventsIfCreated?.OnInitialize;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestStart => _testBuilderContext.EventsIfCreated?.OnTestStart;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestEnd => _testBuilderContext.EventsIfCreated?.OnTestEnd;

    /// <inheritdoc/>
    AsyncEvent<TestContext>? ITestEvents.OnTestSkipped => _testBuilderContext.EventsIfCreated?.OnTestSkipped;

    /// <inheritdoc/>
    AsyncEvent<(TestContext TestContext, int RetryAttempt)>? ITestEvents.OnTestRetry => _testBuilderContext.EventsIfCreated?.OnTestRetry;
}
