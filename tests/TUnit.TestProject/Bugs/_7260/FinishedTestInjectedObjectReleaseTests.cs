using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7260;

/// <summary>
/// A per-test injected object is tracked by the test's context; once the test has finished and the
/// object has been untracked, the context must not keep it reachable either (#7260).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class FinishedTestInjectedObjectReleaseTests
{
    private static WeakReference? _finishedInjectedObject;

    [ClassDataSource<PerTestPayload>(Shared = SharedType.None)]
    public required PerTestPayload Payload { get; init; }

    [Test]
    public void Holds_Injected_Object()
    {
        _finishedInjectedObject = new WeakReference(Payload);
    }

    [Test]
    [DependsOn(nameof(Holds_Injected_Object))]
    public async Task Finished_Injected_Object_Is_Collectable()
    {
        await Assert.That(_finishedInjectedObject).IsNotNull();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await Assert.That(_finishedInjectedObject!.IsAlive).IsFalse();
    }

    public class PerTestPayload
    {
        public byte[] Data { get; } = new byte[1024 * 1024];
    }
}
