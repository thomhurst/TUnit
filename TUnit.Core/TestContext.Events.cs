using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    /// <summary>
    /// For internal framework use only. External code should use the Events property which returns ITestEvents.
    /// </summary>
    internal TestContextEvents GetTestContextEvents() => _testBuilderContext.Events;

    AsyncEvent<TestContext>? ITestEvents.OnDispose
    {
        get => _testBuilderContext.Events.OnDispose;
        set => _testBuilderContext.Events.OnDispose = value;
    }

    AsyncEvent<TestContext>? ITestEvents.OnTestRegistered
    {
        get => _testBuilderContext.Events.OnTestRegistered;
        set => _testBuilderContext.Events.OnTestRegistered = value;
    }

    AsyncEvent<TestContext>? ITestEvents.OnInitialize
    {
        get => _testBuilderContext.Events.OnInitialize;
        set => _testBuilderContext.Events.OnInitialize = value;
    }

    AsyncEvent<TestContext>? ITestEvents.OnTestStart
    {
        get => _testBuilderContext.Events.OnTestStart;
        set => _testBuilderContext.Events.OnTestStart = value;
    }

    AsyncEvent<TestContext>? ITestEvents.OnTestEnd
    {
        get => _testBuilderContext.Events.OnTestEnd;
        set => _testBuilderContext.Events.OnTestEnd = value;
    }

    AsyncEvent<TestContext>? ITestEvents.OnTestSkipped
    {
        get => _testBuilderContext.Events.OnTestSkipped;
        set => _testBuilderContext.Events.OnTestSkipped = value;
    }

    AsyncEvent<(TestContext, int RetryAttempt)>? ITestEvents.OnTestRetry
    {
        get => _testBuilderContext.Events.OnTestRetry;
        set => _testBuilderContext.Events.OnTestRetry = value;
    }
}
