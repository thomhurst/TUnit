using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7391;

/// <summary>
/// Review finding on PR #7391. When one injected property throws during registration, sibling
/// properties may already have resolved shared values, and the failed test still invokes their
/// ITestEndEventReceiver. Those values must be tracked by the failed test: a shared value no other
/// test holds then stays alive until that invocation, and is disposed once the test ends instead of
/// only at the session-end sweep.
/// </summary>
public sealed class DisposalRecordingResource : ITestEndEventReceiver, IAsyncDisposable
{
    public static bool OnTestEndRan { get; private set; }

    public static bool WasDisposedBeforeOnTestEnd { get; private set; }

    public static bool IsDisposed { get; private set; }

    public ValueTask OnTestEnd(TestContext context)
    {
        OnTestEndRan = true;
        WasDisposedBeforeOnTestEnd = IsDisposed;
        return default;
    }

    public int Order => 0;

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}

public sealed class ThrowingResource
{
    public ThrowingResource() => throw new InvalidOperationException("Deliberate resolution failure (#7391 review)");
}

[EngineTest(ExpectedResult.Failure)]
public sealed class PartialInjectionFailureDisposalTests
{
    // Keyed by a key no other test uses, so this test is the value's only holder.
    [ClassDataSource<DisposalRecordingResource>(Shared = SharedType.Keyed, Key = "7391-partial-injection-failure")]
    public required DisposalRecordingResource Good { get; init; }

    [ClassDataSource<ThrowingResource>(Shared = SharedType.Keyed, Key = "7391-partial-injection-failure")]
    public required ThrowingResource Bad { get; init; }

    [Test]
    public void Fails_During_Registration()
    {
        // Never runs: ThrowingResource's constructor throws while property values are resolved.
    }
}

// Marked Failure because DependsOn pulls the failing test into any filtered run of this one.
[EngineTest(ExpectedResult.Failure)]
public sealed class PartialInjectionFailureDisposalObserverTests
{
    [Test]
    [DependsOn(typeof(PartialInjectionFailureDisposalTests), nameof(PartialInjectionFailureDisposalTests.Fails_During_Registration), ProceedOnFailure = true)]
    public async Task Resolved_Value_Lives_Until_The_Failed_Test_Ends()
    {
        await Assert.That(DisposalRecordingResource.OnTestEndRan).IsTrue();
        await Assert.That(DisposalRecordingResource.WasDisposedBeforeOnTestEnd).IsFalse();
        await Assert.That(DisposalRecordingResource.IsDisposed).IsTrue();
    }
}
