using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6554;

/// <summary>
/// Partial-injection-failure edge of issue #6554 (surfaced by review on PR #6556).
///
/// Property resolution runs all of a test's injected properties in parallel (Task.WhenAll), so
/// when one property's data source throws, sibling properties may still have resolved and
/// cached their values — including values that implement event receiver interfaces. The test
/// is then marked failed at registration, and TestCoordinator invokes ITestEndEventReceiver
/// for it. The receiver-cache invalidation in ObjectLifecycleService.RegisterTestAsync must
/// therefore run even when resolution exits exceptionally (finally), otherwise the
/// successfully-created receiver stays invisible to the stale pre-injection cache and misses
/// the OnTestEnd callback.
/// </summary>
public sealed class EndRecordingResource : ITestEndEventReceiver
{
    public static bool OnTestEndRan { get; private set; }

    public ValueTask OnTestEnd(TestContext context)
    {
        OnTestEndRan = true;
        return default;
    }

    public int Order => 0;
}

public sealed class ThrowingResource
{
    public ThrowingResource() => throw new InvalidOperationException("Deliberate resolution failure (#6554 partial-injection edge)");
}

[EngineTest(ExpectedResult.Failure)]
public sealed class PartialInjectionFailureTests
{
    [ClassDataSource<EndRecordingResource>(Shared = SharedType.PerTestSession)]
    public required EndRecordingResource Good { get; init; }

    [ClassDataSource<ThrowingResource>(Shared = SharedType.PerTestSession)]
    public required ThrowingResource Bad { get; init; }

    [Test]
    public void Fails_During_Registration()
    {
        // Never runs: ThrowingResource's constructor throws while property values are being
        // resolved during registration, so the test is marked failed before execution.
    }
}

// Marked Failure (not Pass) despite passing itself: DependsOn pulls Fails_During_Registration
// into any filtered run containing this test, which would poison the EngineTest=Pass bucket's
// TRX outcome. Both classes are verified together by PartialInjectionFailure6554Tests in
// TUnit.Engine.Tests.
[EngineTest(ExpectedResult.Failure)]
public sealed class PartialInjectionFailureObserverTests
{
    [Test]
    [DependsOn(typeof(PartialInjectionFailureTests), nameof(PartialInjectionFailureTests.Fails_During_Registration), ProceedOnFailure = true)]
    public async Task End_Receiver_On_Successfully_Resolved_Property_Fired()
    {
        await Assert.That(EndRecordingResource.OnTestEndRan).IsTrue();
    }
}
