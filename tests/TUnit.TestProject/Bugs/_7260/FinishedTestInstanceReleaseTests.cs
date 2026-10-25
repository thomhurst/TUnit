using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7260;

/// <summary>
/// A finished test's class instance must not stay reachable through its TestContext, which the
/// session keeps until it ends, so state held in instance fields can be collected (#7260).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class FinishedTestInstanceReleaseTests
{
    private static WeakReference? _finishedInstance;

    private byte[]? _payload;

    [Test]
    public void Holds_State_In_Instance_Field()
    {
        _payload = new byte[1024 * 1024];
        _finishedInstance = new WeakReference(this);
    }

    [Test]
    [DependsOn(nameof(Holds_State_In_Instance_Field))]
    public async Task Finished_Instance_Is_Collectable()
    {
        await Assert.That(_finishedInstance).IsNotNull();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await Assert.That(_finishedInstance!.IsAlive).IsFalse();
    }
}
