using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6557;

/// <summary>
/// Reproduction for issue #6557: first-in-session/assembly event receivers registered mid-run
/// could miss the one-shot event.
/// https://github.com/thomhurst/TUnit/issues/6557
///
/// The first-in-X invocations are memoized per scope key and enumerate the EventReceiverRegistry
/// at the moment the first gated test reaches them. Receivers used to be registered lazily when
/// their own test entered TestCoordinator, so a receiver belonging to a later-scheduled test
/// missed the one-shot whenever an earlier test with a same-interface receiver had already
/// triggered the memoized invocation. The test project contains other
/// IFirstTestInAssemblyEventReceiver implementations, so in a full-suite run this test was
/// non-deterministic by exactly that mechanism.
///
/// EventReceiverOrchestrator.PrepareForExecution now registers every test's eligible receivers
/// (including injected property values, #6554) before any test executes, making this
/// deterministic: every test awaits the memoized first-in-session/assembly invocations before
/// its body runs, and those invocations see the complete registry.
/// </summary>
public sealed class FirstEventRecordingResource : IFirstTestInTestSessionEventReceiver, IFirstTestInAssemblyEventReceiver
{
    public static bool OnFirstTestInSessionRan { get; private set; }
    public static bool OnFirstTestInAssemblyRan { get; private set; }

    public ValueTask OnFirstTestInTestSession(TestSessionContext current, TestContext testContext)
    {
        OnFirstTestInSessionRan = true;
        return default;
    }

    public ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        OnFirstTestInAssemblyRan = true;
        return default;
    }

    public int Order => 0;
}

[EngineTest(ExpectedResult.Pass)]
public sealed class InjectedPropertyFirstEventReceiverTests
{
    [ClassDataSource<FirstEventRecordingResource>(Shared = SharedType.PerTestSession)]
    public required FirstEventRecordingResource Resource { get; init; }

    [Test]
    public async Task Injected_Property_Receives_First_In_Session_And_Assembly_Events()
    {
        // The one-shot events fired on the chronologically first test of the session/assembly
        // (possibly this one). This test awaited those memoized invocations before its body,
        // so the flags must be set by now regardless of scheduling order.
        await Assert.That(FirstEventRecordingResource.OnFirstTestInSessionRan).IsTrue();
        await Assert.That(FirstEventRecordingResource.OnFirstTestInAssemblyRan).IsTrue();
    }
}
