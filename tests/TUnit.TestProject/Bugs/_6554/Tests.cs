using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6554;

/// <summary>
/// Reproduction for issue #6554: "Injected properties don't get their event receivers executed."
/// https://github.com/thomhurst/TUnit/issues/6554
///
/// The per-context event receiver caches are built (and marked as built) when
/// ITestRegisteredEventReceiver instances are gathered during registration — which deliberately
/// happens BEFORE injected property values are resolved, so SkipAttribute can short-circuit
/// expensive data sources. The caches were never invalidated after property resolution, so the
/// eligible-event-object set fed to the EventReceiverRegistry at execution time was stale and
/// missing the injected property values. If the only ITestStartEventReceiver /
/// ITestEndEventReceiver in the run was an injected property (like below), the registry-level
/// fast-path gates reported "no receivers" and the events never fired.
/// </summary>
public sealed class EventRecordingResource : IAsyncInitializer, ITestStartEventReceiver, ITestEndEventReceiver
{
    public bool InitializeAsyncRan { get; private set; }
    public int OnTestStartCount { get; private set; }
    public int OnTestEndCount { get; private set; }

    public Task InitializeAsync()
    {
        InitializeAsyncRan = true;
        return Task.CompletedTask;
    }

    public ValueTask OnTestStart(TestContext context)
    {
        OnTestStartCount++;
        return default;
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        OnTestEndCount++;
        return default;
    }

    public int Order => 0;
}

[EngineTest(ExpectedResult.Pass)]
public sealed class InjectedPropertyEventReceiverTests
{
    [ClassDataSource<EventRecordingResource>(Shared = SharedType.PerTestSession)]
    public required EventRecordingResource Resource { get; init; }

    [Test]
    public async Task Injected_Property_Receives_OnTestStart()
    {
        await Assert.That(Resource.InitializeAsyncRan).IsTrue();
        await Assert.That(Resource.OnTestStartCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    [DependsOn(nameof(Injected_Property_Receives_OnTestStart))]
    public async Task Injected_Property_Receives_OnTestEnd()
    {
        // The first test has fully completed (DependsOn), so its OnTestEnd must have fired
        // on the shared instance. Our own OnTestStart must have fired too.
        await Assert.That(Resource.OnTestEndCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(Resource.OnTestStartCount).IsGreaterThanOrEqualTo(2);
    }
}
